import { describe, expect, it } from "vitest";

import { parseRoute } from "./route";

describe("parseRoute", () => {
    it("opens the workspace list at the root", () => {
        expect(parseRoute("/")).toEqual({ page: "workspaces" });
    });
    it("reads a workspace name, decoded", () => {
        expect(parseRoute("/workspaces/q3%20board")).toEqual({ page: "workspace", name: "q3 board" });
        expect(parseRoute("/workspaces/q3/")).toEqual({ page: "workspace", name: "q3" });
    });
    it("opens the templates", () => {
        expect(parseRoute("/templates")).toEqual({ page: "templates" });
    });
    it("does not know other paths", () => {
        expect(parseRoute("/workspaces")).toEqual({ page: "notFound" });
        expect(parseRoute("/workspaces/a/b")).toEqual({ page: "notFound" });
        expect(parseRoute("/nope")).toEqual({ page: "notFound" });
        expect(parseRoute("/workspaces/%E0%A4%A")).toEqual({ page: "notFound" });
    });
});
