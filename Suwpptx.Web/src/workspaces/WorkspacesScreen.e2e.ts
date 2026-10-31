import { copyFileSync, existsSync, mkdirSync, rmSync, writeFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { expect, test } from "@playwright/test";

// The folder the server works on (e2e.sh exports it); tests write into it to play "Claude Code changed the folder".
const ROOT = resolve(process.env["E2E_ROOT"] ?? resolve(import.meta.dirname, "../../../.artifacts/e2e/root"));
const FIXTURES = resolve(import.meta.dirname, "../../e2e/fixtures");
const workspaceDir = (name: string) => join(ROOT, "workspaces", name);
const unique = (prefix: string) => `${prefix}-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 5)}`;

function makeWorkspaceOnDisk(name: string) {
    const directory = workspaceDir(name);
    mkdirSync(join(directory, "resources"), { recursive: true });
    mkdirSync(join(directory, "versions"), { recursive: true });
    writeFileSync(join(directory, "context.md"), "# context\n");
}

test("AC-pptx-studio-1: a new workspace opens, is listed and has a context.md; a duplicate name is refused", async ({ page }) => {
    const name = unique("create");
    await page.goto("/");
    await page.getByRole("button", { name: "New workspace" }).click();
    await page.getByRole("textbox", { name: "Name" }).fill(name);
    await page.getByRole("button", { name: "Create", exact: true }).click();

    await expect(page).toHaveURL(new RegExp(`/workspaces/${name}$`));
    await expect(page.getByRole("link", { name: `← ${name}` })).toBeVisible();
    expect(existsSync(join(workspaceDir(name), "context.md"))).toBe(true);

    await page.getByRole("link", { name: "Workspaces" }).click();
    await expect(page.getByRole("link", { name: new RegExp(`^${name}`) })).toBeVisible();

    await page.getByRole("button", { name: "New workspace" }).click();
    await page.getByRole("textbox", { name: "Name" }).fill(name);
    await page.getByRole("button", { name: "Create", exact: true }).click();
    await expect(page.getByRole("alert")).toContainText("pick another name");
    await expect(page).toHaveURL(/\/$/);
});

test("AC-pptx-studio-12: deleting a workspace removes it from the list and its folder from disk", async ({ page }) => {
    const name = unique("remove");
    await page.goto("/");
    await page.getByRole("button", { name: "New workspace" }).click();
    await page.getByRole("textbox", { name: "Name" }).fill(name);
    await page.getByRole("button", { name: "Create", exact: true }).click();
    await expect(page).toHaveURL(new RegExp(`/workspaces/${name}$`));

    // context.md + note.txt = 2 files, one deck version.
    writeFileSync(join(workspaceDir(name), "resources", "note.txt"), "hello");
    copyFileSync(join(FIXTURES, "three-slides.pptx"), join(workspaceDir(name), "versions", "v1.pptx"));

    await page.getByRole("link", { name: "Workspaces" }).click();
    const card = page.getByRole("link", { name: new RegExp(`^${name}`) });
    await expect(card).toContainText("2 files");
    await page.getByRole("button", { name: `Actions for ${name}` }).click();
    await page.getByRole("menuitem", { name: "Delete" }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toContainText(`Delete ${name} with 2 files and 1 version? This removes the folder and can't be undone.`);
    await dialog.getByRole("button", { name: "Delete" }).click();

    await expect(card).toHaveCount(0);
    await expect(dialog).toBeHidden();
    expect(existsSync(workspaceDir(name))).toBe(false);
});

test("AC-pptx-studio-13: a workspace added or removed on disk appears and disappears in the open list within 5 seconds", async ({
    page,
}) => {
    const name = unique("outside");
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Workspaces" })).toBeVisible();
    const card = page.getByRole("link", { name: new RegExp(`^${name}`) });
    await expect(card).toHaveCount(0);

    makeWorkspaceOnDisk(name);
    await expect(card).toBeVisible({ timeout: 5_000 });

    rmSync(workspaceDir(name), { recursive: true, force: true });
    await expect(card).toHaveCount(0, { timeout: 5_000 });
});

test("AC-pptx-studio-13: a file copied into resources/ on disk appears in the open workspace's rail within 5 seconds", async ({ page }) => {
    const name = unique("rail");
    makeWorkspaceOnDisk(name);
    await page.goto(`/workspaces/${name}`);
    const rail = page.getByRole("complementary", { name: "Resources" });
    await expect(rail.getByRole("button", { name: /^context\.md/ })).toBeVisible();
    await expect(rail.getByRole("button", { name: /^photo\.png/ })).toHaveCount(0);

    copyFileSync(join(FIXTURES, "photo.png"), join(workspaceDir(name), "resources", "photo.png"));
    await expect(rail.getByRole("button", { name: /^photo\.png/ })).toBeVisible({ timeout: 5_000 });
});
