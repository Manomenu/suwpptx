import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { expect, test, type Page } from "@playwright/test";

const ROOT = resolve(process.env["E2E_ROOT"] ?? resolve(import.meta.dirname, "../../../.artifacts/e2e/root"));
const FIXTURES = resolve(import.meta.dirname, "../../e2e/fixtures");
const unique = (prefix: string) => `${prefix}-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 5)}`;

function makeWorkspaceOnDisk(name: string) {
    const directory = join(ROOT, "workspaces", name);
    mkdirSync(join(directory, "resources"), { recursive: true });
    mkdirSync(join(directory, "versions"), { recursive: true });
    writeFileSync(join(directory, "context.md"), "# context\n");
    return directory;
}

const rail = (page: Page) => page.getByRole("complementary", { name: "Resources" });

test("AC-pptx-studio-3: an uploaded file is listed with its size and downloads with the same bytes", async ({ page }) => {
    const name = unique("upload");
    makeWorkspaceOnDisk(name);
    await page.goto(`/workspaces/${name}`);

    const original = readFileSync(join(FIXTURES, "photo.png"));
    await page.getByTestId("upload-input").setInputFiles(join(FIXTURES, "photo.png"));
    const row = rail(page).getByRole("button", { name: /^photo\.png/ });
    await expect(row).toBeVisible();
    await expect(row).toContainText(/\d+(\.\d+)? (B|KB|kB|MB)/);
    expect(readFileSync(join(ROOT, "workspaces", name, "resources", "photo.png")).equals(original)).toBe(true);

    await row.click();
    const dialog = page.getByRole("dialog");
    const [download] = await Promise.all([page.waitForEvent("download"), dialog.getByRole("button", { name: "Download" }).click()]);
    expect(download.suggestedFilename()).toBe("photo.png");
    const path = await download.path();
    expect(readFileSync(path).equals(original)).toBe(true);
});

test("AC-pptx-studio-4: a file over the server's limit (5 MB in the e2e server) is refused with a message naming the limit", async ({
    page,
}) => {
    const name = unique("limit");
    const directory = makeWorkspaceOnDisk(name);
    await page.goto(`/workspaces/${name}`);

    await page.getByTestId("upload-input").setInputFiles({
        name: "big.bin",
        mimeType: "application/octet-stream",
        buffer: Buffer.alloc(6 * 1024 * 1024, 1),
    });
    const failed = page.getByRole("list", { name: "Uploads" });
    await expect(failed).toContainText("big.bin");
    await expect(failed).toContainText("5 MB");
    await expect(failed.getByRole("button", { name: "Retry" })).toBeVisible();
    await expect(rail(page).getByRole("button", { name: /^big\.bin/ })).toHaveCount(0);
    // Not even a half-written temporary file stays behind.
    expect(existsSync(join(directory, "resources", "big.bin"))).toBe(false);
    expect(readdirSync(join(directory, "resources"))).toEqual([]);
});

const text = (content: string) => Buffer.from(content);
// ftyp box only: not playable, but the <video> element is what is under test.
const fakeMp4 = Buffer.concat([Buffer.from([0, 0, 0, 24]), Buffer.from("ftypisom"), Buffer.alloc(12)]);

const kinds: { file: string; content: () => Buffer; check: (page: Page) => Promise<void> }[] = [
    {
        file: "readme.md",
        content: () => text("# Big Heading\n\nSome *text*.\n"),
        check: async (page) => {
            await expect(page.getByTestId("markdown-preview").getByRole("heading", { name: "Big Heading" })).toBeVisible();
        },
    },
    {
        file: "code.ts",
        content: () => text("export const answer: number = 42;\n"),
        check: async (page) => {
            await expect(page.getByTestId("code-preview")).toContainText("export const answer");
        },
    },
    {
        file: "plain.txt",
        content: () => text("just plain words\n"),
        check: async (page) => {
            await expect(page.getByRole("dialog").locator("pre")).toContainText("just plain words");
            await expect(page.getByTestId("code-preview")).toHaveCount(0);
        },
    },
    {
        file: "photo.png",
        content: () => readFileSync(join(FIXTURES, "photo.png")),
        check: async (page) => {
            const image = page.getByRole("dialog").getByRole("img", { name: "photo.png" });
            await expect(image).toBeVisible();
            await expect.poll(() => image.evaluate((element: HTMLImageElement) => element.naturalWidth)).toBeGreaterThan(0);
        },
    },
    {
        file: "clip.mp4",
        content: () => fakeMp4,
        check: async (page) => {
            await expect(page.getByRole("dialog").locator("video")).toBeVisible();
        },
    },
    {
        file: "notes.pdf",
        content: () => readFileSync(join(FIXTURES, "notes.pdf")),
        check: async (page) => {
            await expect(page.getByRole("dialog").locator("iframe")).toBeVisible();
        },
    },
    {
        file: "sales.csv",
        content: () => text("Region,Revenue\nNorth,10\nSouth,20\n"),
        check: async (page) => {
            const table = page.getByRole("dialog").getByRole("table");
            await expect(table).toContainText("Region");
            await expect(table).toContainText("North");
        },
    },
    {
        file: "sales.xlsx",
        content: () => readFileSync(join(FIXTURES, "sales.xlsx")),
        check: async (page) => {
            const table = page.getByRole("dialog").getByRole("table");
            await expect(table).toContainText("Region");
            await expect(table).toContainText("North");
        },
    },
    {
        file: "deck.pptx",
        content: () => readFileSync(join(FIXTURES, "three-slides.pptx")),
        check: async (page) => {
            const thumbnails = page.getByTestId("deck-thumbnails").getByRole("img");
            await expect(thumbnails).toHaveCount(3, { timeout: 45_000 });
            for (let index = 0; index < 3; index++) {
                await expect
                    .poll(() => thumbnails.nth(index).evaluate((element: HTMLImageElement) => element.naturalWidth))
                    .toBeGreaterThan(0);
            }
        },
    },
    {
        file: "model.blend",
        content: () => text("BLENDER-v300"),
        check: async (page) => {
            await expect(page.getByTestId("file-card")).toContainText("no preview for this type");
        },
    },
];

test("AC-pptx-studio-5: the resource panel previews each kind of file the matching way", async ({ page }) => {
    test.setTimeout(120_000);
    const name = unique("preview");
    makeWorkspaceOnDisk(name);
    await page.goto(`/workspaces/${name}`);

    for (const { file, content } of kinds.slice(0, 5)) {
        await page.getByTestId("upload-input").setInputFiles({ name: file, mimeType: "application/octet-stream", buffer: content() });
    }
    // Upload the rest straight on disk to cover files the server picks up on its own too.
    for (const { file, content } of kinds.slice(5)) {
        writeFileSync(join(ROOT, "workspaces", name, "resources", file), content());
    }

    for (const { file, check } of kinds) {
        await rail(page)
            .getByRole("button", { name: new RegExp(`^${file.replace(".", "\\.")}`) })
            .click();
        const dialog = page.getByRole("dialog");
        await expect(dialog.getByRole("heading", { name: file })).toBeVisible();
        await check(page);
        await dialog.getByRole("button", { name: "Close" }).click();
        await expect(dialog).toBeHidden();
    }
});

test("AC-pptx-studio-6: a .pptx in resources shows its slide thumbnails", async ({ page }) => {
    const name = unique("deckthumbs");
    const directory = makeWorkspaceOnDisk(name);
    copyFileSync(join(FIXTURES, "three-slides.pptx"), join(directory, "resources", "deck.pptx"));
    await page.goto(`/workspaces/${name}`);
    await rail(page)
        .getByRole("button", { name: /^deck\.pptx/ })
        .click();

    const thumbnails = page.getByTestId("deck-thumbnails").getByRole("img");
    await expect(thumbnails).toHaveCount(3, { timeout: 45_000 });
    for (let index = 0; index < 3; index++) {
        await expect.poll(() => thumbnails.nth(index).evaluate((element: HTMLImageElement) => element.naturalWidth)).toBeGreaterThan(0);
    }
});
