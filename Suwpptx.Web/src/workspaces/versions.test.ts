import { describe, expect, it } from "vitest";

import { arrivedAfter, newestReady, pickVersion, type VersionPreview } from "./versions";

const ready: VersionPreview = { state: "Ready", problem: null };
const rendering: VersionPreview = { state: "Rendering", problem: null };
const failed: VersionPreview = { state: "Failed", problem: "the file is password-protected" };

describe("pickVersion", () => {
    it("shows nothing for a workspace without versions", () => {
        expect(pickVersion([], {}, null)).toEqual({ shown: null, pending: null });
    });
    it("shows the newest when it is ready, even with gaps in the numbers", () => {
        expect(pickVersion([5, 2, 1], { 1: ready, 2: ready, 5: ready }, null)).toEqual({ shown: 5, pending: null });
    });
    it("keeps the previous ready version while the newest is rendering", () => {
        expect(pickVersion([4, 3], { 3: ready, 4: rendering }, null)).toEqual({
            shown: 3,
            pending: { number: 4, state: "Rendering", problem: null },
        });
    });
    it("keeps the previous ready version when the newest failed, and says why", () => {
        expect(pickVersion([4, 3], { 3: ready, 4: failed }, null)).toEqual({
            shown: 3,
            pending: { number: 4, state: "Failed", problem: "the file is password-protected" },
        });
    });
    it("skips over failed versions to the newest ready one", () => {
        expect(pickVersion([4, 3, 2], { 2: ready, 3: failed, 4: failed }, null).shown).toBe(2);
    });
    it("shows nothing when the only version is not ready yet", () => {
        expect(pickVersion([1], {}, null)).toEqual({ shown: null, pending: { number: 1, state: "Rendering", problem: null } });
    });
    it("shows the version the owner picked", () => {
        expect(pickVersion([3, 2, 1], { 1: ready, 2: ready, 3: ready }, 2)).toEqual({ shown: 2, pending: null });
    });
    it("ignores a pick that no longer exists", () => {
        expect(pickVersion([3, 2], { 2: ready, 3: ready }, 7).shown).toBe(3);
    });
});

describe("newestReady", () => {
    it("is the highest ready number", () => {
        expect(newestReady([3, 2, 1], { 1: ready, 2: ready, 3: rendering })).toBe(2);
        expect(newestReady([1], {})).toBeNull();
    });
});

describe("arrivedAfter", () => {
    it("names the version we were on when a newer one became ready", () => {
        expect(arrivedAfter(3, 4)).toBe(3);
    });
    it("is null at first sight, when nothing changed, or when it went down", () => {
        expect(arrivedAfter(null, 1)).toBeNull();
        expect(arrivedAfter(3, 3)).toBeNull();
        expect(arrivedAfter(3, 2)).toBeNull();
        expect(arrivedAfter(3, null)).toBeNull();
    });
});
