import { useEffect, useRef, useState } from "react";

import { subscribeChanges } from "./changes";
import { ApiError } from "./client";

const POLL_MS = 1500;

interface Loaded<T> {
    key: string;
    data: T | undefined;
    error: ApiError | Error | null;
}

/**
 * Loads what a screen shows and keeps it current: again after every change event of the folder, and every
 * 1.5 s while `shouldPoll(data)` says something is still being prepared (slides being drawn).
 * `key` names what is loaded: a new key starts over and never shows the old key's data.
 */
export function useLive<T>(load: () => Promise<T>, key: string, shouldPoll?: (data: T) => boolean) {
    const loadRef = useRef(load);
    const pollRef = useRef(shouldPoll);
    const runRef = useRef<() => void>(() => undefined);
    const [state, setState] = useState<Loaded<T>>({ key, data: undefined, error: null });

    useEffect(() => {
        loadRef.current = load;
        pollRef.current = shouldPoll;
    });

    useEffect(() => {
        let alive = true;
        let timer: ReturnType<typeof setTimeout> | undefined;

        const run = async () => {
            clearTimeout(timer);
            try {
                const data = await loadRef.current();
                if (!alive) return;
                setState({ key, data, error: null });
                if (pollRef.current?.(data)) timer = setTimeout(() => void run(), POLL_MS);
            } catch (error) {
                if (!alive) return;
                const failure = error instanceof Error ? error : new Error(String(error));
                setState((old) => ({ key, data: old.key === key ? old.data : undefined, error: failure }));
            }
        };

        runRef.current = () => void run();
        void run();
        const stop = subscribeChanges(() => void run());
        return () => {
            alive = false;
            clearTimeout(timer);
            stop();
        };
    }, [key]);

    const current = state.key === key ? state : { key, data: undefined, error: null };
    return {
        data: current.data,
        error: current.error,
        /** Loads again now, e.g. after the screen itself changed something. */
        reload: () => {
            runRef.current();
        },
    };
}
