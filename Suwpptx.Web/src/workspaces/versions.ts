type PreviewState = "Rendering" | "Ready" | "Failed";
export interface VersionPreview {
    state: PreviewState;
    problem: string | null;
}

export interface VersionView {
    /** The version whose slides are on screen: the wanted one when it is ready, else the newest ready one below it. */
    shown: number | null;
    /** The wanted version when it is not ready: its slides are being drawn, or it can't be read. */
    pending: { number: number; state: "Rendering" | "Failed"; problem: string | null } | null;
}

/**
 * Which version the viewer shows. `chosen` is what the owner picked, or null to follow the newest.
 * A version that is rendering or failed never replaces the one on screen: the viewer keeps the previous ready version.
 * A version whose previews are not known yet counts as rendering.
 */
export function pickVersion(numbers: number[], previews: Record<number, VersionPreview | undefined>, chosen: number | null): VersionView {
    if (numbers.length === 0) return { shown: null, pending: null };
    const wanted = chosen !== null && numbers.includes(chosen) ? chosen : Math.max(...numbers);
    const preview = previews[wanted];
    if (preview?.state === "Ready") return { shown: wanted, pending: null };

    const fallback = numbers.filter((number) => number < wanted && previews[number]?.state === "Ready");
    return {
        shown: fallback.length > 0 ? Math.max(...fallback) : null,
        pending: { number: wanted, state: preview?.state === "Failed" ? "Failed" : "Rendering", problem: preview?.problem ?? null },
    };
}

/** The highest ready version, the one a follower of the newest ends up on. */
export function newestReady(numbers: number[], previews: Record<number, VersionPreview | undefined>): number | null {
    const ready = numbers.filter((number) => previews[number]?.state === "Ready");
    return ready.length > 0 ? Math.max(...ready) : null;
}

/** The version the viewer was on before a newer one became ready, or null when nothing new arrived. */
export function arrivedAfter(previous: number | null, current: number | null): number | null {
    return previous !== null && current !== null && current > previous ? previous : null;
}
