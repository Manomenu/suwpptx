/**
 * One EventSource on /api/changes for the whole page. The server says "something in the folder changed" (event
 * "change", data = revision); subscribers refetch what their screen shows. Bursts are merged by a short debounce.
 */
import { apiUrl } from "./client";

const DEBOUNCE_MS = 200;

const listeners = new Set<() => void>();
let source: EventSource | null = null;
let timer: ReturnType<typeof setTimeout> | undefined;

function notifySoon() {
    clearTimeout(timer);
    timer = setTimeout(() => {
        listeners.forEach((listener) => {
            listener();
        });
    }, DEBOUNCE_MS);
}

function open() {
    let failed = false;
    const next = new EventSource(apiUrl("/changes"));
    next.addEventListener("change", notifySoon);
    next.onerror = () => {
        failed = true;
    };
    // After a reconnect changes may have been missed: refetch once.
    next.onopen = () => {
        if (failed) notifySoon();
        failed = false;
    };
    source = next;
}

/** Calls `listener` after every change in the folder; returns the function that stops it. */
export function subscribeChanges(listener: () => void): () => void {
    listeners.add(listener);
    if (!source) open();
    return () => {
        listeners.delete(listener);
        if (listeners.size === 0) {
            source?.close();
            source = null;
            clearTimeout(timer);
        }
    };
}
