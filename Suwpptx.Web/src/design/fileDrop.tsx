import { useEffect, useRef, useState } from "react";

function carriesFiles(event: DragEvent) {
    return event.dataTransfer?.types.includes("Files") ?? false;
}

/** Files dropped anywhere on the page go to `onFiles`; while they are dragged over it, a frame says where they land. */
export function FileDrop({ onFiles, label }: { onFiles: (files: File[]) => void; label: string }) {
    const [dragging, setDragging] = useState(false);
    const depth = useRef(0);
    const handler = useRef(onFiles);

    useEffect(() => {
        handler.current = onFiles;
    });

    useEffect(() => {
        const enter = (event: DragEvent) => {
            if (!carriesFiles(event)) return;
            event.preventDefault();
            depth.current++;
            setDragging(true);
        };
        const over = (event: DragEvent) => {
            if (carriesFiles(event)) event.preventDefault();
        };
        const leave = (event: DragEvent) => {
            if (!carriesFiles(event)) return;
            depth.current = Math.max(0, depth.current - 1);
            if (depth.current === 0) setDragging(false);
        };
        const drop = (event: DragEvent) => {
            if (!carriesFiles(event)) return;
            event.preventDefault();
            depth.current = 0;
            setDragging(false);
            const files = Array.from(event.dataTransfer?.files ?? []);
            if (files.length > 0) handler.current(files);
        };
        window.addEventListener("dragenter", enter);
        window.addEventListener("dragover", over);
        window.addEventListener("dragleave", leave);
        window.addEventListener("drop", drop);
        return () => {
            window.removeEventListener("dragenter", enter);
            window.removeEventListener("dragover", over);
            window.removeEventListener("dragleave", leave);
            window.removeEventListener("drop", drop);
        };
    }, []);

    if (!dragging) return null;
    return (
        <div className="pointer-events-none fixed inset-3 z-50 flex items-center justify-center rounded-2xl border-2 border-dashed border-ring bg-background/80 backdrop-blur-sm">
            <p className="text-lg font-medium">{label}</p>
        </div>
    );
}
