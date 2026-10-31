import { useRef } from "react";

import { baseName, toNumber } from "@/api/paths";
import { Button } from "@/design/button";
import { cn } from "@/design/classNames";
import { formatSize } from "@/design/format";

import type { FolderEntry } from "./api";
import { classify } from "./resourceKind";

/** "resources/a/b.png" is shown as "a/b.png"; context.md stays as it is. */
function displayName(path: string) {
    return path.startsWith("resources/") ? path.slice("resources/".length) : path;
}

/** The narrow column of files: Upload, and one row per file (icon by kind, name, size). */
export function ResourceRail({
    files,
    onOpen,
    onFiles,
    className,
}: {
    files: FolderEntry[];
    onOpen: (path: string) => void;
    onFiles: (files: File[]) => void;
    className?: string;
}) {
    const input = useRef<HTMLInputElement>(null);

    return (
        <aside aria-label="Resources" className={cn("flex flex-col gap-3", className)}>
            <div className="flex items-center justify-between gap-2">
                <h2 className="text-sm font-semibold">Resources</h2>
                <Button
                    size="sm"
                    onClick={() => {
                        input.current?.click();
                    }}
                >
                    Upload
                </Button>
                <input
                    ref={input}
                    type="file"
                    multiple
                    hidden
                    data-testid="upload-input"
                    aria-label="Upload files"
                    onChange={(event) => {
                        onFiles(Array.from(event.target.files ?? []));
                        event.target.value = "";
                    }}
                />
            </div>
            <p className="text-xs text-muted-foreground">or drop files anywhere</p>
            <ul className="flex flex-col gap-0.5" aria-label="Resource files">
                {files.map((file) => {
                    const { icon } = classify(file.path);
                    return (
                        <li key={file.path}>
                            <button
                                type="button"
                                onClick={() => {
                                    onOpen(file.path);
                                }}
                                title={displayName(file.path)}
                                className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm outline-none hover:bg-accent focus-visible:ring-[3px] focus-visible:ring-ring/50"
                            >
                                <span aria-hidden>{icon}</span>
                                <span className="min-w-0 flex-1 truncate">{displayName(file.path) || baseName(file.path)}</span>
                                <span className="shrink-0 text-xs text-muted-foreground">{formatSize(toNumber(file.size))}</span>
                            </button>
                        </li>
                    );
                })}
            </ul>
        </aside>
    );
}
