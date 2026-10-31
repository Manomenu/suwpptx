import { Alert } from "@/design/alert";
import { Button } from "@/design/button";
import { ConfirmDialog } from "@/design/confirmDialog";

export interface UploadItem {
    id: number;
    name: string;
    progress: number;
    status: "uploading" | "error";
    error: string | null;
}

/** The files on their way up (name, progress bar) or failed (message, Retry), and the "Replace x?" question. */
export function UploadStatus({
    uploads,
    replacing,
    onAnswerReplace,
    onRetry,
    onDismiss,
}: {
    uploads: UploadItem[];
    replacing: string | null;
    onAnswerReplace: (replace: boolean) => void;
    onRetry: (id: number) => void;
    onDismiss: (id: number) => void;
}) {
    return (
        <>
            {uploads.length > 0 && (
                <ul className="flex flex-col gap-2" aria-label="Uploads">
                    {uploads.map((upload) =>
                        upload.status === "error" ? (
                            <li key={upload.id}>
                                <Alert variant="destructive" className="flex flex-col gap-2">
                                    <p className="font-medium break-all">{upload.name}</p>
                                    <p>{upload.error}</p>
                                    <div className="flex gap-2">
                                        <Button
                                            size="xs"
                                            variant="outline"
                                            onClick={() => {
                                                onRetry(upload.id);
                                            }}
                                        >
                                            Retry
                                        </Button>
                                        <Button
                                            size="xs"
                                            variant="ghost"
                                            onClick={() => {
                                                onDismiss(upload.id);
                                            }}
                                        >
                                            Dismiss
                                        </Button>
                                    </div>
                                </Alert>
                            </li>
                        ) : (
                            <li key={upload.id} className="flex flex-col gap-1 rounded-lg border bg-card px-3 py-2 text-sm">
                                <span className="truncate">{upload.name}</span>
                                <progress
                                    className="h-1.5 w-full accent-primary"
                                    value={upload.progress}
                                    max={1}
                                    aria-label={`Uploading ${upload.name}`}
                                />
                            </li>
                        ),
                    )}
                </ul>
            )}
            <ConfirmDialog
                open={replacing !== null}
                title={`Replace ${replacing ?? ""}?`}
                description={`A file named ${replacing ?? ""} is already there. Replacing overwrites it with the one you dropped.`}
                confirmLabel="Replace"
                onConfirm={() => {
                    onAnswerReplace(true);
                }}
                onCancel={() => {
                    onAnswerReplace(false);
                }}
            />
        </>
    );
}
