import { useState } from "react";

import { ApiError } from "@/api/client";
import { toNumber } from "@/api/paths";
import { useDeckPreviews } from "@/api/previews";
import { useLive } from "@/api/useLive";
import { useUploadQueue } from "@/api/useUploadQueue";
import { Alert } from "@/design/alert";
import { Button } from "@/design/button";
import { CopyButton } from "@/design/copyButton";
import { download } from "@/design/download";
import { FileDrop } from "@/design/fileDrop";
import { relativeTime } from "@/design/format";
import { Link } from "@/design/navigation";
import { UploadStatus } from "@/design/uploadStatus";

import { deckPath, fileUrl, getWorkspace, uploadPath } from "./api";
import { DeckViewer } from "./DeckViewer";
import { ResourcePanel } from "./ResourcePanel";
import { ResourceRail } from "./ResourceRail";
import { arrivedAfter, newestReady, pickVersion, type VersionPreview } from "./versions";

function Gone({ deleted }: { deleted: boolean }) {
    return (
        <div className="flex flex-col items-start gap-3 py-8" data-testid="workspace-gone">
            <h1 className="text-2xl font-semibold">{deleted ? "This workspace was deleted" : "This workspace doesn't exist"}</h1>
            <Link href="/" className="text-sm underline underline-offset-4">
                ← Back to the workspaces
            </Link>
        </div>
    );
}

/** `/workspaces/<name>`: header, the deck viewer in the middle, the resource rail on the left. */
export function WorkspaceScreen({ name }: { name: string }) {
    const { data: workspace, error, reload } = useLive(() => getWorkspace(name), `workspace:${name}`);
    const [chosen, setChosen] = useState<number | null>(null);
    const [openPath, setOpenPath] = useState<string | null>(null);
    const [railOpen, setRailOpen] = useState(false);
    const [seenReady, setSeenReady] = useState<number | null>(null);
    const [arrived, setArrived] = useState<{ number: number; back: number } | null>(null);
    const uploads = useUploadQueue((fileName, replace) => uploadPath(name, fileName, replace), reload);

    const versions = workspace?.versions.map((version) => ({ ...version, number: toNumber(version.number) })) ?? [];
    const decks = versions.map((version) => deckPath(name, version.path));
    const previews = useDeckPreviews(decks);
    const byNumber: Record<number, VersionPreview & { slides: number }> = {};
    versions.forEach((version, index) => {
        const preview = previews?.[decks[index] ?? ""];
        if (preview) byNumber[version.number] = preview;
    });
    const numbers = versions.map((version) => version.number);
    const view = pickVersion(numbers, byNumber, chosen);

    // A newer version that became ready while the screen is open: shown at once, with a way back.
    const ready = previews === undefined ? seenReady : newestReady(numbers, byNumber);
    if (ready !== seenReady) {
        setSeenReady(ready);
        const back = arrivedAfter(seenReady, ready);
        if (back !== null && ready !== null) setArrived({ number: ready, back });
    }

    if (error instanceof ApiError && error.status === 404) return <Gone deleted={workspace !== undefined} />;
    if (workspace === undefined) {
        return error === null ? (
            <p className="text-sm text-muted-foreground">Loading…</p>
        ) : (
            <Alert variant="destructive">Can't load this workspace: {error.message}</Alert>
        );
    }

    const shownIndex = versions.findIndex((version) => version.number === view.shown);
    const shownVersion = shownIndex >= 0 ? versions[shownIndex] : undefined;
    const shownPreview = shownVersion ? byNumber[shownVersion.number] : undefined;
    const selectedNumber = chosen !== null && numbers.includes(chosen) ? chosen : (numbers[0] ?? null);
    const openEntry = workspace.files.find((file) => file.path === openPath);

    return (
        <div className="flex flex-col gap-5">
            <header className="flex flex-wrap items-center gap-3">
                <h1 className="min-w-0 text-2xl font-semibold tracking-tight">
                    <Link href="/" className="break-all hover:underline">
                        ← {workspace.name}
                    </Link>
                </h1>
                <CopyButton text={workspace.folder} label="Copy path" />
                <div className="ml-auto flex flex-wrap items-center gap-3">
                    {versions.length > 0 && (
                        <select
                            aria-label="Version"
                            value={selectedNumber ?? ""}
                            onChange={(event) => {
                                const next = Number(event.target.value);
                                setChosen(next === numbers[0] ? null : next);
                                setArrived(null);
                            }}
                            className="h-9 rounded-md border border-input bg-card px-3 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
                        >
                            {versions.map((version) => (
                                <option key={version.number} value={version.number}>
                                    v{version.number} · {relativeTime(version.changedAt)}
                                </option>
                            ))}
                        </select>
                    )}
                    <Button
                        disabled={shownVersion === undefined}
                        onClick={() => {
                            if (shownVersion) download(fileUrl(name, shownVersion.path, true));
                        }}
                    >
                        Download .pptx
                    </Button>
                </div>
            </header>

            <Button
                variant="outline"
                size="sm"
                className="self-start lg:hidden"
                aria-expanded={railOpen}
                onClick={() => {
                    setRailOpen(!railOpen);
                }}
            >
                Resources ({workspace.files.length})
            </Button>

            <UploadStatus
                uploads={uploads.uploads}
                replacing={uploads.replacing}
                onAnswerReplace={uploads.answerReplace}
                onRetry={uploads.retry}
                onDismiss={uploads.dismiss}
            />

            <div className="grid items-start gap-6 lg:grid-cols-[17rem_minmax(0,1fr)]">
                <div className={railOpen ? "flex flex-col gap-3" : "hidden flex-col gap-3 lg:flex"}>
                    <ResourceRail files={workspace.files} onOpen={setOpenPath} onFiles={uploads.add} />
                </div>
                <DeckViewer
                    key={name}
                    workspace={name}
                    versionCount={versions.length}
                    shown={
                        shownVersion && shownPreview
                            ? { number: shownVersion.number, deck: deckPath(name, shownVersion.path), slides: shownPreview.slides }
                            : null
                    }
                    pending={view.pending}
                    arrived={chosen === null ? arrived : null}
                    onBack={() => {
                        if (arrived) setChosen(arrived.back);
                        setArrived(null);
                    }}
                    keyboard={openEntry === undefined}
                />
            </div>

            <FileDrop onFiles={uploads.add} label="Drop to add to resources" />
            {openEntry !== undefined && (
                <ResourcePanel
                    key={openEntry.path}
                    workspace={name}
                    entry={openEntry}
                    onClose={() => {
                        setOpenPath(null);
                    }}
                />
            )}
        </div>
    );
}
