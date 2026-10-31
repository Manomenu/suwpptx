/** How the resource panel shows a file, decided by its extension alone. */
export type ResourceKind = "markdown" | "code" | "text" | "image" | "video" | "audio" | "pdf" | "csv" | "xlsx" | "deck" | "other";

export interface Classified {
    kind: ResourceKind;
    /** highlight.js language name, for `code` only. */
    language: string | null;
    /** What the icon in the list shows. */
    icon: string;
}

const CODE: Record<string, string> = {
    js: "javascript",
    mjs: "javascript",
    cjs: "javascript",
    jsx: "javascript",
    ts: "typescript",
    tsx: "typescript",
    json: "json",
    py: "python",
    cs: "csharp",
    java: "java",
    go: "go",
    rs: "rust",
    rb: "ruby",
    php: "php",
    sh: "bash",
    bash: "bash",
    sql: "sql",
    html: "xml",
    htm: "xml",
    xml: "xml",
    css: "css",
    scss: "scss",
    yml: "yaml",
    yaml: "yaml",
    toml: "ini",
    ini: "ini",
    c: "c",
    h: "c",
    cpp: "cpp",
    cc: "cpp",
    hpp: "cpp",
    kt: "kotlin",
    swift: "swift",
    lua: "lua",
    ps1: "powershell",
};

const BY_EXTENSION: Record<string, ResourceKind> = {
    md: "markdown",
    markdown: "markdown",
    txt: "text",
    text: "text",
    log: "text",
    png: "image",
    jpg: "image",
    jpeg: "image",
    gif: "image",
    webp: "image",
    svg: "image",
    bmp: "image",
    avif: "image",
    mp4: "video",
    webm: "video",
    mov: "video",
    m4v: "video",
    ogv: "video",
    mp3: "audio",
    wav: "audio",
    ogg: "audio",
    m4a: "audio",
    flac: "audio",
    aac: "audio",
    pdf: "pdf",
    csv: "csv",
    tsv: "csv",
    xlsx: "xlsx",
    pptx: "deck",
    potx: "deck",
};

const ICONS: Record<ResourceKind, string> = {
    markdown: "📝",
    code: "💻",
    text: "📄",
    image: "🖼",
    video: "🎬",
    audio: "🎵",
    pdf: "📕",
    csv: "📊",
    xlsx: "📊",
    deck: "📽",
    other: "📎",
};

export function classify(fileName: string): Classified {
    const name = fileName.slice(fileName.lastIndexOf("/") + 1).toLowerCase();
    const extension = name.includes(".") ? name.slice(name.lastIndexOf(".") + 1) : "";
    const language = CODE[extension];
    const kind: ResourceKind = language === undefined ? (BY_EXTENSION[extension] ?? "other") : "code";
    return { kind, language: language ?? null, icon: ICONS[kind] };
}

/** Kinds that are read as text in the browser; past this size the panel offers the download instead of loading it. */
const TEXT_PREVIEW_LIMIT_BYTES = 2 * 1024 * 1024;
const SPREADSHEET_PREVIEW_LIMIT_BYTES = 25 * 1024 * 1024;

export function previewable(kind: ResourceKind, sizeBytes: number): boolean {
    if (kind === "markdown" || kind === "code" || kind === "text" || kind === "csv") return sizeBytes <= TEXT_PREVIEW_LIMIT_BYTES;
    if (kind === "xlsx") return sizeBytes <= SPREADSHEET_PREVIEW_LIMIT_BYTES;
    return kind !== "other";
}
