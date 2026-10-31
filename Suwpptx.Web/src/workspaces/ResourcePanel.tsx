import { useState } from "react";

import { baseName, toNumber } from "@/api/paths";
import { Button } from "@/design/button";
import { ConfirmDialog } from "@/design/confirmDialog";
import { Dialog, DialogClose, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/design/dialog";
import { download } from "@/design/download";
import { formatSize } from "@/design/format";

import { deckPath, deleteFile, fileUrl, type FolderEntry } from "./api";
import { ResourcePreview } from "./ResourcePreview";
import { classify, previewable } from "./resourceKind";

/** The side panel of one resource: a preview by kind, Download and Delete. */
export function ResourcePanel({ workspace, entry, onClose }: { workspace: string; entry: FolderEntry; onClose: () => void }) {
    const [confirming, setConfirming] = useState(false);
    const { kind, language, icon } = classify(entry.path);
    const size = toNumber(entry.size);
    const name = baseName(entry.path);
    const canPreview = previewable(kind, size);

    return (
        <>
            <Dialog
                open
                onOpenChange={(open) => {
                    if (!open) onClose();
                }}
            >
                <DialogContent side aria-describedby="resource-description">
                    <DialogHeader>
                        <DialogTitle>{name}</DialogTitle>
                        <DialogDescription id="resource-description">
                            {entry.path} · {formatSize(size)}
                        </DialogDescription>
                    </DialogHeader>
                    <div className="flex flex-wrap gap-2">
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => {
                                download(fileUrl(workspace, entry.path, true));
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
                    <ResourcePreview
                        kind={kind}
                        language={language}
                        fileName={name}
                        url={fileUrl(workspace, entry.path)}
                        deck={deckPath(workspace, entry.path)}
                        previewable={canPreview}
                    />
                    {!canPreview && (
                        <div
                            className="flex flex-col items-center gap-2 rounded-xl border bg-muted/40 px-6 py-10 text-center"
                            data-testid="file-card"
                        >
                            <span className="text-4xl" aria-hidden>
                                {icon}
                            </span>
                            <p className="font-medium break-all">{name}</p>
                            <p className="text-sm text-muted-foreground">
                                {formatSize(size)} · {kind === "other" ? "no preview for this type" : "too large to preview here"}
                            </p>
                        </div>
                    )}
                </DialogContent>
            </Dialog>
            <ConfirmDialog
                open={confirming}
                title={`Delete ${name}?`}
                description="This removes the file from the workspace folder and can't be undone."
                confirmLabel="Delete"
                destructive
                onConfirm={async () => {
                    await deleteFile(workspace, entry.path);
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
