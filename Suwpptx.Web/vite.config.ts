import path from "node:path";

import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
    plugins: [react(), tailwindcss()],
    resolve: { alias: { "@": path.resolve(import.meta.dirname, "./src") } },
    server: {
        port: 3310,
        strictPort: true,
        // Same contract as nginx in the image: the browser only ever talks to this origin, and /api goes to the server.
        proxy: {
            "/api": {
                // The end-to-end tests run their own server next to yours (e2e.sh).
                target: process.env["API_PROXY_TARGET"] ?? "http://127.0.0.1:6310",
                changeOrigin: true,
            },
        },
    },
});
