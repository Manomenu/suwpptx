import { useRef, useState } from "react";

import { toNumber } from "@/api/paths";
import { slideUrls, useDeckPreview, useDeckPreviews } from "@/api/previews";
import { useLive } from "@/api/useLive";
import { useUploadQueue } from "@/api/useUploadQueue";
import { Alert } from "@/design/alert";
import { Button } from "@/design/button";
import { Card } from "@/design/card";
import { ConfirmDialog } from "@/design/confirmDialog";
import { Dialog, DialogClose, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/design/dialog";
import { download } from "@/design/download";
import { FileDrop } from "@/design/fileDrop";
import { formatSize, plural } from "@/design/format";
import { SlideViewer, ThumbnailRow } from "@/design/slideViewer";
import { UploadStatus } from "@/design/uploadStatus";

import { deckPath, deleteTemplate, listTemplates, templateUrl, uploadPath, type FolderEntry } from "./api";

function TemplateRow({ entry, preview, onOpen }: { entry: FolderEntry; preview: ReturnType<typeof useDeckPreviews>; onOpen: () => void }) {
    const deck = deckPath(entry.path);
    const state = preview?.[deck];
    const size = formatSize(toNumber(entry.size));

    return (
        <Card className="transition hover:border-ring">
            <button
                type="button"
                onClick={onOpen}
                aria-label={`Open ${entry.path}`}
                className="flex w-full flex-col gap-2 rounded-xl p-4 text-left outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50"
            >
                <span className="flex flex-wrap items-baseline gap-x-3">
                    <span className="font-medium break-all">{entry.path}</span>
                    <span className="text-sm text-muted-foreground">
                        {state?.state === "Ready" ? `${plural(state.slides, "slide")} · ${size}` : size}
                    </span>
                </span>
                {state?.state === "Ready" && <ThumbnailRow slides={slideUrls(deck, state.slides)} />}
                {state?.state === "Rendering" && <span className="text-sm text-muted-foreground">Drawing the slides…</span>}
                {state?.state === "Failed" && (
                    <span className="w-fit rounded-md border border-destructive/40 px-2 py-0.5 text-xs text-destructive">
                        can't read this deck
                    </span>
                )}
            </button>
        </Card>
    );
}

function TemplatePanel({ entry, onClose }: { entry: FolderEntry; onClose: () => void }) {
    const deck = deckPath(entry.path);
    const preview = useDeckPreview(deck);
    const [index, setIndex] = useState(0);
    const [confirming, setConfirming] = useState(false);

    return (
        <>
            <Dialog
                open
                onOpenChange={(open) => {
                    if (!open) onClose();
                }}
            >
                <DialogContent className="max-h-[92vh] max-w-4xl overflow-y-auto *:shrink-0">
                    <DialogHeader>
                        <DialogTitle>{entry.path}</DialogTitle>
                        <DialogDescription>
                            {preview?.state === "Ready" ? `${plural(preview.slides, "slide")} · ` : ""}
                            {formatSize(toNumber(entry.size))}
                        </DialogDescription>
                    </DialogHeader>
                    <div className="flex flex-wrap gap-2">
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => {
                                download(templateUrl(entry.path, true));
                            }}
                        >
                            Download
                        </Button>
                        <Button
                            variant="outline"
                            size="sm"
                            className="text-destructive"
                            onClick={() => {
                                setConfirming(true);
                            }}
                        >
                            Delete
                        </Button>
                        <DialogClose asChild>
                            <Button variant="ghost" size="sm" className="ml-auto">
                                Close
                            </Button>
                        </DialogClose>
                    </div>
                    {(preview === undefined || preview.state === "Rendering") && (
                        <p className="text-sm text-muted-foreground">Drawing the slides…</p>
                    )}
                    {preview?.state === "Failed" && (
                        <Alert variant="destructive">This deck can't be shown: {preview.problem ?? "the file can't be read"}</Alert>
                    )}
                    {preview?.state === "Ready" && preview.slides > 0 && (
                        <SlideViewer slides={slideUrls(deck, preview.slides)} index={index} onIndexChange={setIndex} compact />
                    )}
                </DialogContent>
            </Dialog>
            <ConfirmDialog
                open={confirming}
                title={`Delete ${entry.path}?`}
                description="This removes the deck from the template library and can't be undone."
                confirmLabel="Delete"
                destructive
                onConfirm={async () => {
                    await deleteTemplate(entry.path);
                    setConfirming(false);
                    onClose();
                }}
                onCancel={() => {
                    setConfirming(false);
                }}
            />
        </>
    );
}

/** `/templates`: the shared decks whose look gets reused, each with a row of thumbnails. */
export function TemplatesScreen() {
    const { data: templates, error, reload } = useLive(listTemplates, "templates");
    const [openName, setOpenName] = useState<string | null>(null);
    const input = useRef<HTMLInputElement>(null);
    const uploads = useUploadQueue(uploadPath, reload);
    const previews = useDeckPreviews(templates?.map((template) => deckPath(template.path)) ?? []);
    const open = templates?.find((template) => template.path === openName);

    return (
        <div className="flex flex-col gap-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <h1 className="text-2xl font-semibold tracking-tight">Templates</h1>
                <Button
                    onClick={() => {
                        input.current?.click();
                    }}
                >
                    Upload deck
                </Button>
                <input
                    ref={input}
                    type="file"
                    hidden
                    multiple
                    accept=".pptx,.potx"
                    data-testid="upload-input"
                    aria-label="Upload decks"
                    onChange={(event) => {
                        uploads.add(Array.from(event.target.files ?? []));
                        event.target.value = "";
                    }}
                />
            </div>
            <p className="text-sm text-muted-foreground">
                Decks whose look Claude reuses: theme colours, fonts and layouts. Drop a .pptx or .potx anywhere on this page.
            </p>
            <UploadStatus
                uploads={uploads.uploads}
                replacing={uploads.replacing}
                onAnswerReplace={uploads.answerReplace}
                onRetry={uploads.retry}
                onDismiss={uploads.dismiss}
            />
            {error !== null && templates === undefined && <Alert variant="destructive">Can't load the templates: {error.message}</Alert>}
            {templates?.length === 0 && (
                <Card className="p-8 text-center text-muted-foreground">No templates yet. Upload a deck to reuse its look.</Card>
            )}
            <ul className="grid gap-3" aria-label="Templates">
                {templates?.map((template) => (
                    <li key={template.path}>
                        <TemplateRow
                            entry={template}
                            preview={previews}
                            onOpen={() => {
                                setOpenName(template.path);
                            }}
                        />
                    </li>
                ))}
            </ul>
            <FileDrop onFiles={uploads.add} label="Drop to add to the templates" />
            {open !== undefined && (
                <TemplatePanel
                    key={open.path}
                    entry={open}
                    onClose={() => {
                        setOpenName(null);
                    }}
                />
            )}
        </div>
    );
}
