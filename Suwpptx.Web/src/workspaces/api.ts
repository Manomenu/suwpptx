import { apiUrl, request } from "@/api/client";
import type { components } from "@/api/openapi";
import { encodePath } from "@/api/paths";

export type WorkspaceSummary = components["schemas"]["WorkspaceSummary"];
export type Workspace = components["schemas"]["Workspace"];
export type FolderEntry = components["schemas"]["FolderEntry"];

const workspacePath = (name: string) => `/workspaces/${encodeURIComponent(name)}`;

export function listWorkspaces() {
    return request<WorkspaceSummary[]>("/workspaces");
}

export function createWorkspace(name: string) {
    return request<WorkspaceSummary>("/workspaces", { method: "POST", body: JSON.stringify({ name }) });
}

export function getWorkspace(name: string) {
    return request<Workspace>(workspacePath(name));
}

export function renameWorkspace(name: string, newName: string) {
    return request<WorkspaceSummary>(workspacePath(name), { method: "PATCH", body: JSON.stringify({ name: newName }) });
}

export function deleteWorkspace(name: string) {
    return request<undefined>(workspacePath(name), { method: "DELETE" });
}

export function deleteFile(name: string, path: string) {
    return request<undefined>(`${workspacePath(name)}/files/${encodePath(path)}`, { method: "DELETE" });
}

/** Where a file of the workspace can be read (inline) or, with `download`, saved. */
export function fileUrl(name: string, path: string, download = false) {
    return apiUrl(`${workspacePath(name)}/files/${encodePath(path)}${download ? "?download=true" : ""}`);
}

/** The PUT path that stores a browser upload as `resources/<fileName>`. */
export function uploadPath(name: string, fileName: string, replace: boolean) {
    return `${workspacePath(name)}/resources/${encodeURIComponent(fileName)}?replace=${replace}`;
}

/** A deck of the workspace as the previews API names it. */
export function deckPath(name: string, relativePath: string) {
    return `workspaces/${name}/${relativePath}`;
}
