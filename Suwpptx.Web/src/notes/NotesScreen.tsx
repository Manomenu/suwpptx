import { useEffect, useState } from "react";

import { Alert } from "@/design/alert";
import { Button } from "@/design/button";
import { Input } from "@/design/input";
import { Label } from "@/design/label";

import { createNote, fetchNotes, type Note } from "./api";
import { problemWith } from "./notes";

function messageOf(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
}

/** The Notes screen: the list, and a field to add one. The template's example feature; delete it when it is in the way. */
export function NotesScreen() {
    const [notes, setNotes] = useState<Note[]>([]);
    const [text, setText] = useState("");
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        // StrictMode runs effects twice in development: abort the first request, ignore its result.
        const controller = new AbortController();
        fetchNotes(controller.signal)
            .then(setNotes)
            .catch((e: unknown) => {
                if (!controller.signal.aborted) setError(messageOf(e));
            });
        return () => {
            controller.abort();
        };
    }, []);

    function add() {
        const problem = problemWith(text);
        if (problem !== null) {
            setError(problem);
            return;
        }
        setError(null);
        createNote(text)
            .then((note) => {
                setNotes((current) => [note, ...current]);
                setText("");
            })
            .catch((e: unknown) => {
                setError(messageOf(e));
            });
    }

    return (
        <main className="mx-auto flex max-w-xl flex-col gap-4 p-4">
            <h1 className="text-2xl font-semibold">Notes</h1>
            <div className="flex items-end gap-2">
                <div className="flex flex-1 flex-col gap-2">
                    <Label htmlFor="new-note">New note</Label>
                    <Input
                        id="new-note"
                        value={text}
                        onChange={(event) => {
                            setText(event.currentTarget.value);
                        }}
                    />
                </div>
                <Button onClick={add}>Add note</Button>
            </div>
            {error !== null && <Alert variant="destructive">{error}</Alert>}
            {notes.length === 0 ? (
                <p className="text-muted-foreground">No notes yet.</p>
            ) : (
                <ul className="list-disc pl-6">
                    {notes.map((note) => (
                        <li key={note.id}>{note.text}</li>
                    ))}
                </ul>
            )}
        </main>
    );
}
