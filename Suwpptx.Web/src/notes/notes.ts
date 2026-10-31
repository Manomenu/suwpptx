// Rules live in plain modules with a test beside them; components only wire them to React.
export const MAX_NOTE_LENGTH = 500;

/** What is wrong with the text of a new note, in the user's words; null when it can be sent. */
export function problemWith(text: string): string | null {
    if (text.trim().length === 0) return "Write something first.";
    if (text.length > MAX_NOTE_LENGTH) return `A note has at most ${MAX_NOTE_LENGTH} characters.`;
    return null;
}
