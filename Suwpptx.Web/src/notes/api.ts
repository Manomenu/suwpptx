// This feature's HTTP calls, typed from the generated OpenAPI types — never a hand-written copy of a server model.
import { request } from "../api/client";
import type { components } from "../api/openapi";

export type Note = components["schemas"]["Note"];

export function fetchNotes(signal: AbortSignal): Promise<Note[]> {
    return request<Note[]>("/notes", { signal });
}

export function createNote(text: string): Promise<Note> {
    const body: components["schemas"]["NewNote"] = { text };
    return request<Note>("/notes", { method: "POST", body: JSON.stringify(body) });
}
