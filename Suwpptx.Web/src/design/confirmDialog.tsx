import { useState } from "react";

import { Alert } from "@/design/alert";
import { Button } from "@/design/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/design/dialog";

/**
 * A question with two answers. `onConfirm` may fail: its message is shown in the dialog and the dialog stays open.
 */
export function ConfirmDialog({
    open,
    title,
    description,
    confirmLabel,
    destructive = false,
    onConfirm,
    onCancel,
}: {
    open: boolean;
    title: string;
    description: string;
    confirmLabel: string;
    destructive?: boolean;
    onConfirm: () => Promise<void> | void;
    onCancel: () => void;
}) {
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const confirm = async () => {
        setBusy(true);
        setError(null);
        try {
            await onConfirm();
        } catch (failure) {
            setError(failure instanceof Error ? failure.message : String(failure));
        } finally {
            setBusy(false);
        }
    };

    return (
        <Dialog
            open={open}
            onOpenChange={(next) => {
                if (!next) {
                    setError(null);
                    onCancel();
                }
            }}
        >
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{title}</DialogTitle>
                    <DialogDescription>{description}</DialogDescription>
                </DialogHeader>
                {error !== null && <Alert variant="destructive">{error}</Alert>}
                <DialogFooter>
                    <Button variant="outline" onClick={onCancel} disabled={busy}>
                        Cancel
                    </Button>
                    <Button variant={destructive ? "destructive" : "default"} onClick={() => void confirm()} disabled={busy}>
                        {confirmLabel}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
