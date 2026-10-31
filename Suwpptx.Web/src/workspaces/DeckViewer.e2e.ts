import { copyFileSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { expect, test } from "@playwright/test";

const ROOT = resolve(process.env["E2E_ROOT"] ?? resolve(import.meta.dirname, "../../../.artifacts/e2e/root"));
const FIXTURES = resolve(import.meta.dirname, "../../e2e/fixtures");
const DECK = join(FIXTURES, "three-slides.pptx");
const unique = (prefix: string) => `${prefix}-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 5)}`;

function makeWorkspaceOnDisk(name: string) {
    const directory = join(ROOT, "workspaces", name);
    mkdirSync(join(directory, "resources"), { recursive: true });
    mkdirSync(join(directory, "versions"), { recursive: true });
    writeFileSync(join(directory, "context.md"), "# context\n");
    return directory;
}

test("AC-pptx-studio-11: the newest version opens first, versions switch, Download returns the .pptx and a new version arrives live", async ({
    page,
}) => {
    test.setTimeout(120_000);
    const name = unique("versions");
    const directory = makeWorkspaceOnDisk(name);
    copyFileSync(DECK, join(directory, "versions", "v1.pptx"));
    copyFileSync(DECK, join(directory, "versions", "v2.pptx"));
    const source = readFileSync(DECK);

    await page.goto(`/workspaces/${name}`);
    const position = page.getByTestId("slide-position");
    const slide = page.getByTestId("slide-image");
    await expect(position).toHaveText("Slide 1 / 3", { timeout: 45_000 });
    await expect.poll(() => slide.evaluate((element: HTMLImageElement) => element.naturalWidth)).toBeGreaterThan(0);
    await expect(page.getByRole("combobox", { name: "Version" })).toHaveValue("2");
    // While v2 is still being drawn the viewer shows v1 meanwhile: wait until v2 itself is what is shown.
    await expect(page.getByTestId("viewer-rendering")).toHaveCount(0);

    await page.getByRole("button", { name: "Next" }).click();
    await expect(position).toHaveText("Slide 2 / 3");

    await page.getByRole("combobox", { name: "Version" }).selectOption("1");
    await expect(position).toHaveText("Slide 1 / 3");
    const [download] = await Promise.all([page.waitForEvent("download"), page.getByRole("button", { name: "Download .pptx" }).click()]);
    expect(download.suggestedFilename()).toBe("v1.pptx");
    const bytes = readFileSync(await download.path());
    expect(bytes.subarray(0, 2).toString("latin1")).toBe("PK");
    expect(bytes.equals(source)).toBe(true);

    // Back on the newest, then Claude Code writes v3 while the page is open.
    await page.getByRole("combobox", { name: "Version" }).selectOption("2");
    copyFileSync(DECK, join(directory, "versions", "v3.pptx"));
    await expect(page.getByTestId("viewer-arrived")).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId("viewer-arrived")).toContainText("v3 ready");
    await expect(page.getByRole("button", { name: "Back to v2" })).toBeVisible();
    await expect(page.getByRole("combobox", { name: "Version" })).toHaveValue("3");
    await expect(position).toHaveText("Slide 1 / 3");
});

test("AC-pptx-studio-15: a version that is not a .pptx is reported as failed and the previous version stays shown", async ({ page }) => {
    test.setTimeout(120_000);
    const name = unique("broken");
    const directory = makeWorkspaceOnDisk(name);
    copyFileSync(DECK, join(directory, "versions", "v1.pptx"));

    await page.goto(`/workspaces/${name}`);
    await expect(page.getByTestId("slide-position")).toHaveText("Slide 1 / 3", { timeout: 45_000 });
    const slideSrc = await page.getByTestId("slide-image").getAttribute("src");

    writeFileSync(join(directory, "versions", "v2.pptx"), "this is only text, not a deck\n");
    const failed = page.getByTestId("viewer-failed");
    await expect(failed).toBeVisible({ timeout: 30_000 });
    await expect(failed).toContainText("not a PowerPoint file");
    await expect(failed).toContainText("v1 is still the one shown");
    await expect(page.getByTestId("slide-image")).toBeVisible();
    await expect(page.getByTestId("slide-image")).toHaveAttribute("src", slideSrc ?? "");
    await expect(page.getByTestId("slide-position")).toHaveText("Slide 1 / 3");
});
