import { describe, expect, it } from "vitest";

import { formatSize, plural, relativeTime } from "./format";

const now = new Date("2026-10-31T12:00:00Z");
const ago = (seconds: number) => new Date(now.getTime() - seconds * 1000);

describe("relativeTime", () => {
    it("says just now under a minute", () => {
        expect(relativeTime(ago(10), now)).toBe("just now");
    });
    it("counts minutes, hours and days", () => {
        expect(relativeTime(ago(600), now)).toBe("10 min ago");
        expect(relativeTime(ago(3600), now)).toBe("1 hour ago");
        expect(relativeTime(ago(3 * 3600), now)).toBe("3 hours ago");
        expect(relativeTime(ago(3 * 86400), now)).toBe("3 days ago");
    });
    it("counts months and years", () => {
        expect(relativeTime(ago(60 * 86400), now)).toBe("2 months ago");
        expect(relativeTime(ago(800 * 86400), now)).toBe("2 years ago");
    });
    it("takes ISO strings", () => {
        expect(relativeTime("2026-10-31T11:50:00Z", now)).toBe("10 min ago");
    });
});

describe("formatSize", () => {
    it("scales the unit", () => {
        expect(formatSize(512)).toBe("512 B");
        expect(formatSize(48 * 1024)).toBe("48 KB");
        expect(formatSize(3.1 * 1024 * 1024)).toBe("3.1 MB");
        expect(formatSize(1024)).toBe("1 KB");
        expect(formatSize(212 * 1024 * 1024)).toBe("212 MB");
    });
});

describe("plural", () => {
    it("picks the form by the count", () => {
        expect(plural(1, "file")).toBe("1 file");
        expect(plural(14, "file")).toBe("14 files");
    });
});
