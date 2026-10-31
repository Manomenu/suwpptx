import { join, resolve } from "node:path";
import { expect, test } from "@playwright/test";

const FIXTURES = resolve(import.meta.dirname, "../../e2e/fixtures");

test("AC-pptx-studio-6: a .potx uploaded to the template library shows slide thumbnails", async ({ page }) => {
    await page.goto("/templates");
    await expect(page.getByRole("heading", { name: "Templates" })).toBeVisible();

    await page.getByTestId("upload-input").setInputFiles(join(FIXTURES, "three-slides.potx"));
    const template = page.getByRole("button", { name: "Open three-slides.potx" });
    await expect(template).toBeVisible();

    const thumbnails = template.getByTestId("thumbnails").locator("img");
    await expect(thumbnails).toHaveCount(3, { timeout: 45_000 });
    for (let index = 0; index < 3; index++) {
        await expect.poll(() => thumbnails.nth(index).evaluate((element: HTMLImageElement) => element.naturalWidth)).toBeGreaterThan(0);
    }
});
