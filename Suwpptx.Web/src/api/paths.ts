/** Encodes each segment of a file path, keeping the `/` between them. */
export function encodePath(path: string): string {
    return path.split("/").map(encodeURIComponent).join("/");
}

/** The last segment of a path: the file's name. */
export function baseName(path: string): string {
    return path.slice(path.lastIndexOf("/") + 1);
}

/** Server numbers come as `number | string` in the generated types (int formats); the JSON always carries numbers. */
export function toNumber(value: number | string): number {
    return Number(value);
}
