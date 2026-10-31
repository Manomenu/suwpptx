import { describe, expect, it } from "vitest";

import { detectDelimiter, parseDelimited } from "./csv";

describe("parseDelimited", () => {
    it("splits rows and cells", () => {
        expect(parseDelimited("a,b\n1,2\n", ",", 100)).toEqual([
            ["a", "b"],
            ["1", "2"],
        ]);
    });
    it("keeps delimiters, quotes and line breaks inside quoted cells", () => {
        expect(parseDelimited('"a,1","say ""hi""","x\ny"\r\nnext,row,z', ",", 100)).toEqual([
            ["a,1", 'say "hi"', "x\ny"],
            ["next", "row", "z"],
        ]);
    });
    it("skips blank lines and the byte order mark", () => {
        expect(parseDelimited("﻿a,b\n\n1,2", ",", 100)).toEqual([
            ["a", "b"],
            ["1", "2"],
        ]);
    });
    it("keeps empty cells", () => {
        expect(parseDelimited("a,,c\n,,", ",", 100)).toEqual([
            ["a", "", "c"],
            ["", "", ""],
        ]);
    });
    it("stops at the row limit", () => {
        expect(parseDelimited("1\n2\n3\n4", ",", 2)).toEqual([["1"], ["2"]]);
    });
    it("reads tabs", () => {
        expect(parseDelimited("a\tb", "\t", 10)).toEqual([["a", "b"]]);
    });
});

describe("detectDelimiter", () => {
    it("prefers semicolons when the header has more of them", () => {
        expect(detectDelimiter("a;b;c\n1,5;2;3")).toBe(";");
        expect(detectDelimiter("a,b,c\n1;2;3")).toBe(",");
        expect(detectDelimiter("")).toBe(",");
    });
});
