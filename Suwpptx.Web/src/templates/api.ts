import { apiUrl, request } from "@/api/client";
import type { components } from "@/api/openapi";
import { encodePath } from "@/api/paths";

export type FolderEntry = components["schemas"]["FolderEntry"];

export function listTemplates() {
    return request<FolderEntry[]>("/templates");
}

export function deleteTemplate(fileName: string) {
    return request<undefined>(`/templates/${encodePath(fileName)}`, { method: "DELETE" });
}

export function templateUrl(fileName: string, download = false) {
    return apiUrl(`/templates/${encodePath(fileName)}${download ? "?download=true" : ""}`);
}

export function uploadPath(fileName: string, replace: boolean) {
    return `/templates/${encodeURIComponent(fileName)}?replace=${replace}`;
}

/** The deck as the previews API names it. */
export function deckPath(fileName: string) {
    return `templates/${fileName}`;
}
