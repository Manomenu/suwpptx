import type { components } from "./openapi";
import { apiUrl, request } from "./client";
import { toNumber } from "./paths";
import { useLive } from "./useLive";

type DeckPreview = components["schemas"]["DeckPreview"];

/** What the viewer needs of a deck's previews, slides as a plain number. */
export interface Preview {
    state: DeckPreview["state"];
    slides: number;
    problem: string | null;
}

/** A deck is named by its path under the root folder: `workspaces/<ws>/versions/v1.pptx`, `templates/x.pptx`. */
function slideUrl(deck: string, number: number): string {
    return apiUrl(`/previews/slides/${number}?deck=${encodeURIComponent(deck)}`);
}

export function slideUrls(deck: string, count: number): string[] {
    return Array.from({ length: count }, (_, index) => slideUrl(deck, index + 1));
}

async function fetchPreview(deck: string): Promise<Preview> {
    try {
        const preview = await request<DeckPreview>(`/previews?deck=${encodeURIComponent(deck)}`);
        return { state: preview.state, slides: toNumber(preview.slides), problem: preview.problem };
    } catch (error) {
        return { state: "Failed", slides: 0, problem: error instanceof Error ? error.message : "the preview can't be read" };
    }
}

/** The previews of several decks, polled while any is still rendering and refetched on every change. */
export function useDeckPreviews(decks: string[]) {
    const { data } = useLive(
        async () => Object.fromEntries(await Promise.all(decks.map(async (deck) => [deck, await fetchPreview(deck)] as const))),
        decks.join("|"),
        (all: Record<string, Preview>) => Object.values(all).some((preview) => preview.state === "Rendering"),
    );
    return data;
}

export function useDeckPreview(deck: string): Preview | undefined {
    return useDeckPreviews([deck])?.[deck];
}
