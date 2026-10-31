import { describe, expect, it } from "vitest";

import { baseName, encodePath, toNumber } from "./paths";

describe("encodePath", () => {
    it("encodes every segment and keeps the slashes", () => {
        expect(encodePath("resources/my photo#1.png")).toBe("resources/my%20photo%231.png");
    });
});

describe("baseName", () => {
    it("takes the last segment", () => {
        expect(baseName("resources/sub/a.md")).toBe("a.md");
        expect(baseName("a.md")).toBe("a.md");
    });
});

describe("toNumber", () => {
    it("reads both shapes of the generated integer type", () => {
        expect(toNumber("12")).toBe(12);
        expect(toNumber(7)).toBe(7);
    });
});
