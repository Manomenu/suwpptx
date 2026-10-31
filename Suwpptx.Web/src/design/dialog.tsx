import * as React from "react";
import { Dialog as DialogPrimitive } from "radix-ui";

import { cn } from "@/design/classNames";

const Dialog = DialogPrimitive.Root;
const DialogClose = DialogPrimitive.Close;

/** `side` makes it a panel that slides in from the right edge instead of a centred dialog. */
function DialogContent({
    className,
    children,
    side = false,
    ...props
}: React.ComponentProps<typeof DialogPrimitive.Content> & { side?: boolean }) {
    return (
        <DialogPrimitive.Portal>
            <DialogPrimitive.Overlay className="data-[state=open]:animate-in data-[state=open]:fade-in-0 fixed inset-0 z-40 bg-black/50" />
            <DialogPrimitive.Content
                data-slot="dialog-content"
                className={cn(
                    "fixed z-50 flex flex-col gap-4 border bg-card p-6 text-card-foreground shadow-lg outline-none",
                    side
                        ? "inset-y-0 right-0 w-full max-w-2xl overflow-y-auto border-y-0 border-r-0"
                        : "top-1/2 left-1/2 w-[calc(100%-2rem)] max-w-md -translate-x-1/2 -translate-y-1/2 rounded-xl",
                    className,
                )}
                {...props}
            >
                {children}
            </DialogPrimitive.Content>
        </DialogPrimitive.Portal>
    );
}

function DialogHeader({ className, ...props }: React.ComponentProps<"div">) {
    return <div className={cn("flex flex-col gap-1.5", className)} {...props} />;
}

function DialogFooter({ className, ...props }: React.ComponentProps<"div">) {
    return <div className={cn("flex flex-wrap justify-end gap-2", className)} {...props} />;
}

function DialogTitle({ className, ...props }: React.ComponentProps<typeof DialogPrimitive.Title>) {
    return <DialogPrimitive.Title className={cn("text-lg leading-tight font-semibold break-words", className)} {...props} />;
}

function DialogDescription({ className, ...props }: React.ComponentProps<typeof DialogPrimitive.Description>) {
    return <DialogPrimitive.Description className={cn("text-sm text-muted-foreground", className)} {...props} />;
}

export { Dialog, DialogClose, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle };
