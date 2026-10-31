// End-to-end tests: a real browser against the real server and database.
// scripts/.internal/e2e.sh prepares an empty database and runs them. They start their own server and web app on
// their own ports (checkout-env.sh), so the ones you run by hand on 6310/3310 and your local data stay untouched.
import { defineConfig, devices } from "@playwright/test";

const SERVER_PORT = Number(process.env["E2E_SERVER_PORT"] ?? 6311);
const WEB_PORT = Number(process.env["E2E_WEB_PORT"] ?? 3311);
const DATABASE = process.env["E2E_DATABASE"] ?? "suwpptx_e2e";

export default defineConfig({
    // The tests live next to the code they check (src/notes/NotesScreen.e2e.ts by NotesScreen.tsx).
    testDir: "../src",
    testMatch: "**/*.e2e.ts",
    // One database for the run, so tests that create data would see each other's in parallel.
    workers: 1,
    // On CI a failure is retried once, only to tell a steady failure from a flaky one in the report. A test that
    // passes on the retry still fails the run: flaky is a bug, in the test or in the app, never a pass.
    retries: process.env["CI"] ? 1 : 0,
    failOnFlakyTests: Boolean(process.env["CI"]),
    outputDir: "../../.artifacts/e2e/test-results",
    reporter: process.env["CI"] ? [["list"], ["html", { open: "never", outputFolder: "../../.artifacts/e2e/report" }]] : "list",
    timeout: 60_000,
    use: {
        baseURL: `http://127.0.0.1:${WEB_PORT}`,
        locale: "en-US",
        trace: "retain-on-failure",
        screenshot: "only-on-failure",
    },
    projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 1440, height: 900 } } }],
    webServer: [
        {
            command: "dotnet run --project Suwpptx.Server --no-build --no-launch-profile > .artifacts/logs/e2e-server.jsonl 2>&1",
            cwd: "../..",
            url: `http://127.0.0.1:${SERVER_PORT}/api/health`,
            env: {
                ASPNETCORE_URLS: `http://127.0.0.1:${SERVER_PORT}`,
                ConnectionStrings__Default:
                    process.env["E2E_CONNECTION_STRING"] ??
                    `Host=127.0.0.1;Port=5543;Database=${DATABASE};Username=suwpptx;Password=suwpptx`,
            },
            reuseExistingServer: false,
            timeout: 60_000,
        },
        {
            command: `pnpm exec vite --host 127.0.0.1 --port ${WEB_PORT} --strictPort`,
            cwd: "..",
            url: `http://127.0.0.1:${WEB_PORT}`,
            env: { API_PROXY_TARGET: `http://127.0.0.1:${SERVER_PORT}` },
            reuseExistingServer: false,
            timeout: 60_000,
        },
    ],
});
