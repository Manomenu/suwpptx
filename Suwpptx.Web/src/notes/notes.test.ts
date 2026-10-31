import { describe, expect, it } from "vitest";

import { MAX_NOTE_LENGTH, problemWith } from "./notes";

describe("problemWith", () => {
    it("accepts a note of ordinary length", () => {
        expect(problemWith("buy milk")).toBeNull();
    });

    it("refuses an empty or blank note", () => {
        expect(problemWith("")).not.toBeNull();
        expect(problemWith("   ")).not.toBeNull();
    });

    it("refuses a note longer than the limit, and accepts one exactly at it", () => {
        expect(problemWith("x".repeat(MAX_NOTE_LENGTH))).toBeNull();
        expect(problemWith("x".repeat(MAX_NOTE_LENGTH + 1))).not.toBeNull();
    });
});
