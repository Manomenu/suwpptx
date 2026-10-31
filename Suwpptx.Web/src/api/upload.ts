import { ApiError, apiUrl } from "./client";
import type { components } from "./openapi";

export type FolderEntry = components["schemas"]["FolderEntry"];

/**
 * Sends a file as the raw body of a PUT, with progress (fetch cannot report upload progress, XMLHttpRequest can).
 * A failed response rejects with an ApiError carrying the server's detail and status (409: the file exists).
 */
export function uploadFile(path: string, file: File, onProgress: (fraction: number) => void): Promise<FolderEntry> {
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.open("PUT", apiUrl(path));
        xhr.upload.onprogress = (event) => {
            if (event.lengthComputable) onProgress(event.loaded / event.total);
        };
        xhr.onload = () => {
            if (xhr.status >= 200 && xhr.status < 300) {
                resolve(JSON.parse(xhr.responseText) as FolderEntry);
                return;
            }
            let detail: unknown;
            try {
                detail = (JSON.parse(xhr.responseText) as { detail?: unknown }).detail;
            } catch {
                detail = undefined;
            }
            reject(new ApiError(typeof detail === "string" ? detail : `${xhr.status} ${xhr.statusText}`, xhr.status));
        };
        xhr.onerror = () => {
            reject(new ApiError("The upload was interrupted. Nothing was stored; try again.", 0));
        };
        xhr.send(file);
    });
}
