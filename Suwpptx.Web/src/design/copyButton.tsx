import { useState } from "react";

import { Button } from "@/design/button";

/** Copies `text` to the clipboard and says so next to the button for a moment. */
export function CopyButton({
    text,
    label,
    variant = "outline",
    size = "sm",
}: {
    text: string;
    label: string;
    variant?: "outline" | "secondary" | "ghost";
    size?: "sm" | "xs";
}) {
    const [copied, setCopied] = useState(false);

    const copy = async () => {
        try {
            await navigator.clipboard.writeText(text);
            setCopied(true);
            setTimeout(() => {
                setCopied(false);
            }, 2000);
        } catch {
            setCopied(false);
        }
    };

    return (
        <span className="inline-flex items-center gap-2">
            <Button variant={variant} size={size} onClick={() => void copy()}>
                {label}
            </Button>
            <span role="status" className="text-xs text-success">
                {copied ? "Copied" : ""}
            </span>
        </span>
    );
}
