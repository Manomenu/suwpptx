import { useEffect, useRef, useState } from "react";

import { ApiError } from "./client";
import { uploadFile } from "./upload";

interface QueuedUpload {
    id: number;
    name: string;
    progress: number;
    status: "uploading" | "error";
    error: string | null;
}
type Entry = QueuedUpload & { file: File };

/**
 * Uploads files one after another with progress. A name that exists asks first ("Replace x?") and, on yes, sends
 * the file again with replace=true. A failure (too large, bad type, connection) stays in the list with its message and a retry.
 * `pathFor` builds the PUT path of a file; `onUploaded` runs after each stored file.
 */
export function useUploadQueue(pathFor: (fileName: string, replace: boolean) => string, onUploaded: () => void) {
    const [entries, setEntries] = useState<Entry[]>([]);
    const [replacing, setReplacing] = useState<{ fileName: string; answer: (replace: boolean) => void } | null>(null);
    const chain = useRef<Promise<void>>(Promise.resolve());
    const nextId = useRef(1);
    const latest = useRef({ pathFor, onUploaded });
    useEffect(() => {
        latest.current = { pathFor, onUploaded };
    });

    const patch = (id: number, change: Partial<Entry>) => {
        setEntries((all) => all.map((entry) => (entry.id === id ? { ...entry, ...change } : entry)));
    };
    const remove = (id: number) => {
        setEntries((all) => all.filter((entry) => entry.id !== id));
    };
    const ask = (fileName: string) =>
        new Promise<boolean>((resolve) => {
            setReplacing({ fileName, answer: resolve });
        });

    const process = async (entry: Entry) => {
        patch(entry.id, { status: "uploading", progress: 0, error: null });
        const send = (replace: boolean) =>
            uploadFile(latest.current.pathFor(entry.file.name, replace), entry.file, (progress) => {
                patch(entry.id, { progress });
            });
        try {
            try {
                await send(false);
            } catch (error) {
                if (!(error instanceof ApiError) || error.status !== 409) throw error;
                if (!(await ask(entry.file.name))) {
                    remove(entry.id);
                    return;
                }
                await send(true);
            }
            remove(entry.id);
            latest.current.onUploaded();
        } catch (error) {
            patch(entry.id, { status: "error", error: error instanceof Error ? error.message : String(error) });
        }
    };

    const enqueue = (entry: Entry) => {
        chain.current = chain.current.then(() => process(entry));
    };

    return {
        uploads: entries as QueuedUpload[],
        replacing: replacing === null ? null : replacing.fileName,
        add: (files: File[]) => {
            for (const file of files) {
                const entry: Entry = { id: nextId.current++, name: file.name, progress: 0, status: "uploading", error: null, file };
                setEntries((all) => [...all, entry]);
                enqueue(entry);
            }
        },
        answerReplace: (replace: boolean) => {
            replacing?.answer(replace);
            setReplacing(null);
        },
        retry: (id: number) => {
            const entry = entries.find((candidate) => candidate.id === id);
            if (entry) enqueue(entry);
        },
        dismiss: remove,
    };
}
