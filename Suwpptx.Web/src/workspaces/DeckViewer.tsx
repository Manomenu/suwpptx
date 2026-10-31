import { useState } from "react";

import { slideUrls } from "@/api/previews";
import { Alert } from "@/design/alert";
import { Button } from "@/design/button";
import { Card } from "@/design/card";
import { CopyButton } from "@/design/copyButton";
import { SlideViewer } from "@/design/slideViewer";

import type { VersionView } from "./versions";

export interface ShownDeck {
    number: number;
    deck: string;
    slides: number;
}

function EmptyState({ workspace }: { workspace: string }) {
    const command = `/suwpptx:make ${workspace}`;
    return (
        <Card className="flex flex-col items-center gap-4 px-6 py-16 text-center" data-testid="viewer-empty">
            <p className="text-lg font-medium">No deck yet.</p>
            <p className="text-sm text-muted-foreground">Add files, then ask Claude Code:</p>
            <div className="flex flex-wrap items-center justify-center gap-3">
                <code className="rounded-md bg-muted px-3 py-1.5 font-mono text-sm">{command}</code>
                <CopyButton text={command} label="Copy" />
            </div>
            <p className="max-w-md text-xs text-muted-foreground">
                Plugin not installed? Start Claude Code from the repository with{" "}
                <code className="rounded bg-muted px-1 font-mono">claude --plugin-dir ./plugin</code>.
            </p>
        </Card>
    );
}

/** The big area of the workspace screen: the shown version's slides plus a note about any newer version that isn't showable yet. */
export function DeckViewer({
    workspace,
    versionCount,
    shown,
    pending,
    arrived,
    onBack,
    keyboard,
}: {
    workspace: string;
    versionCount: number;
    shown: ShownDeck | null;
    pending: VersionView["pending"];
    arrived: { number: number; back: number } | null;
    onBack: () => void;
    keyboard: boolean;
}) {
    const [position, setPosition] = useState({ version: 0, index: 0 });
    if (versionCount === 0) return <EmptyState workspace={workspace} />;

    const index = shown !== null && position.version === shown.number ? position.index : 0;

    return (
        <div className="flex min-w-0 flex-col gap-3">
            {pending?.state === "Rendering" && (
                <Alert role="status" data-testid="viewer-rendering">
                    <span>
                        v{pending.number} found, drawing slides…{shown !== null && ` Showing v${shown.number} meanwhile.`}
                    </span>
                </Alert>
            )}
            {pending?.state === "Failed" && (
                <Alert role="status" variant="destructive" data-testid="viewer-failed">
                    <span>
                        v{pending.number} can't be shown: {pending.problem ?? "the file can't be read"}
                        {shown !== null && ` — v${shown.number} is still the one shown.`}
                    </span>
                </Alert>
            )}
            {arrived !== null && pending === null && (
                <Alert role="status" data-testid="viewer-arrived">
                    <span className="flex flex-wrap items-center gap-3">
                        v{arrived.number} ready, shown now
                        <Button variant="outline" size="xs" onClick={onBack}>
                            Back to v{arrived.back}
                        </Button>
                    </span>
                </Alert>
            )}
            {shown !== null && (
                <SlideViewer
                    slides={slideUrls(shown.deck, shown.slides)}
                    index={index}
                    onIndexChange={(next) => {
                        setPosition({ version: shown.number, index: next });
                    }}
                    keyboard={keyboard}
                />
            )}
        </div>
    );
}
