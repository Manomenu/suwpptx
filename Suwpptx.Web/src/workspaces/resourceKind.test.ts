import { describe, expect, it } from "vitest";

import { classify, previewable } from "./resourceKind";

describe("classify", () => {
    it("AC-pptx-studio-5: markdown is rendered", () => {
        expect(classify("context.md").kind).toBe("markdown");
        expect(classify("resources/Notes.MARKDOWN").kind).toBe("markdown");
    });

    it("AC-pptx-studio-5: code carries its highlight.js language", () => {
        expect(classify("resources/app.ts")).toMatchObject({ kind: "code", language: "typescript" });
        expect(classify("a/b/build.py")).toMatchObject({ kind: "code", language: "python" });
        expect(classify("page.html")).toMatchObject({ kind: "code", language: "xml" });
    });

    it("AC-pptx-studio-5: plain text", () => {
        expect(classify("notes.txt").kind).toBe("text");
        expect(classify("server.log").kind).toBe("text");
    });

    it("AC-pptx-studio-5: images, video, audio and PDF are played by the browser", () => {
        expect(classify("photo.PNG").kind).toBe("image");
        expect(classify("hero.jpg").kind).toBe("image");
        expect(classify("demo.mp4").kind).toBe("video");
        expect(classify("voice.mp3").kind).toBe("audio");
        expect(classify("notes.pdf").kind).toBe("pdf");
    });

    it("AC-pptx-studio-5: csv and xlsx show as a table", () => {
        expect(classify("data.csv").kind).toBe("csv");
        expect(classify("data.tsv").kind).toBe("csv");
        expect(classify("sales.xlsx").kind).toBe("xlsx");
    });

    it("AC-pptx-studio-5: pptx and potx show slide thumbnails", () => {
        expect(classify("old.pptx").kind).toBe("deck");
        expect(classify("look.potx").kind).toBe("deck");
    });

    it("AC-pptx-studio-5: any other type, or none, is a file card", () => {
        expect(classify("model.blend").kind).toBe("other");
        expect(classify("firmware.bin").kind).toBe("other");
        expect(classify("Makefile").kind).toBe("other");
        expect(classify("archive.tar.gz").kind).toBe("other");
    });

    it("AC-pptx-studio-5: a dot inside a folder name is not an extension", () => {
        expect(classify("v1.0/readme").kind).toBe("other");
    });
});

describe("previewable", () => {
    it("AC-pptx-studio-5: a huge text file is offered as a download instead", () => {
        expect(previewable("text", 1000)).toBe(true);
        expect(previewable("text", 50 * 1024 * 1024)).toBe(false);
        expect(previewable("xlsx", 50 * 1024 * 1024)).toBe(false);
        expect(previewable("video", 5_000_000_000)).toBe(true);
        expect(previewable("other", 1)).toBe(false);
    });
});
