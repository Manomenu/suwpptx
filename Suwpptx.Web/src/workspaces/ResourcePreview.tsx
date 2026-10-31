import hljs from "highlight.js/lib/common";
import Markdown from "react-markdown";
import { readSheet } from "read-excel-file/browser";
import remarkGfm from "remark-gfm";

import { toError } from "@/api/client";
import { slideUrls, useDeckPreview } from "@/api/previews";
import { useLive } from "@/api/useLive";
import { Alert } from "@/design/alert";
import { DataTable } from "@/design/dataTable";

import { detectDelimiter, parseDelimited } from "./csv";
import type { ResourceKind } from "./resourceKind";

const TABLE_ROWS = 200;

async function fetchBlob(url: string): Promise<Blob> {
    const response = await fetch(url);
    if (!response.ok) throw await toError(response);
    return response.blob();
}

async function fetchText(url: string): Promise<string> {
    return (await fetchBlob(url)).text();
}

function cellText(value: unknown): string {
    if (value instanceof Date) return value.toISOString().slice(0, 10);
    if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") return String(value);
    return "";
}

async function fetchTable(url: string, kind: "csv" | "xlsx", fileName: string): Promise<string[][]> {
    if (kind === "xlsx") {
        const rows = await readSheet(await fetchBlob(url));
        return rows.slice(0, TABLE_ROWS + 1).map((row) => row.map(cellText));
    }
    const text = await fetchText(url);
    const delimiter = fileName.toLowerCase().endsWith(".tsv") ? "\t" : detectDelimiter(text);
    return parseDelimited(text, delimiter, TABLE_ROWS + 1);
}

function Failure({ message }: { message: string }) {
    return <Alert variant="destructive">Can't show this file: {message}</Alert>;
}

function Waiting() {
    return <p className="text-sm text-muted-foreground">Loading…</p>;
}

function TextBody({ url, render }: { url: string; render: (text: string) => React.ReactNode }) {
    const { data, error } = useLive(() => fetchText(url), url);
    if (error !== null && data === undefined) return <Failure message={error.message} />;
    if (data === undefined) return <Waiting />;
    return <>{render(data)}</>;
}

function highlight(code: string, language: string | null): string | null {
    if (language === null || !hljs.getLanguage(language)) return null;
    return hljs.highlight(code, { language, ignoreIllegals: true }).value;
}

function TableBody({ url, kind, fileName }: { url: string; kind: "csv" | "xlsx"; fileName: string }) {
    const { data, error } = useLive(() => fetchTable(url, kind, fileName), url);
    if (error !== null && data === undefined) return <Failure message={error.message} />;
    if (data === undefined) return <Waiting />;
    return <DataTable rows={data.slice(0, TABLE_ROWS + 1)} truncated={data.length > TABLE_ROWS} />;
}

function DeckBody({ deck }: { deck: string }) {
    const preview = useDeckPreview(deck);
    if (preview === undefined || preview.state === "Rendering") return <p className="text-sm text-muted-foreground">Drawing the slides…</p>;
    if (preview.state === "Failed")
        return <Alert variant="destructive">This deck can't be shown: {preview.problem ?? "the file can't be read"}</Alert>;
    return (
        <ul className="grid grid-cols-2 gap-3" aria-label="Slides" data-testid="deck-thumbnails">
            {slideUrls(deck, preview.slides).map((url, index) => (
                <li key={url} className="overflow-hidden rounded-lg border bg-white">
                    <img src={url} alt={`Slide ${index + 1}`} loading="lazy" className="w-full" />
                </li>
            ))}
        </ul>
    );
}

/** The body of the resource panel: the file shown the way its kind calls for. */
export function ResourcePreview({
    kind,
    language,
    fileName,
    url,
    deck,
    previewable,
}: {
    kind: ResourceKind;
    language: string | null;
    fileName: string;
    url: string;
    deck: string;
    previewable: boolean;
}) {
    if (!previewable) return null;
    switch (kind) {
        case "markdown":
            return (
                <TextBody
                    url={url}
                    render={(text) => (
                        <div className="markdown-body" data-testid="markdown-preview">
                            <Markdown remarkPlugins={[remarkGfm]}>{text}</Markdown>
                        </div>
                    )}
                />
            );
        case "code":
            return (
                <TextBody
                    url={url}
                    render={(text) => {
                        const html = highlight(text, language);
                        return (
                            <pre className="hljs overflow-auto rounded-lg border bg-muted/40 p-3 text-xs" data-testid="code-preview">
                                {html === null ? <code>{text}</code> : <code dangerouslySetInnerHTML={{ __html: html }} />}
                            </pre>
                        );
                    }}
                />
            );
        case "text":
            return (
                <TextBody
                    url={url}
                    render={(text) => (
                        <pre className="overflow-auto rounded-lg border bg-muted/40 p-3 text-xs whitespace-pre-wrap">{text}</pre>
                    )}
                />
            );
        case "image":
            return (
                <img
                    src={url}
                    alt={fileName}
                    className="max-h-[65vh] max-w-full self-center rounded-lg border bg-muted/40 object-contain"
                />
            );
        case "video":
            return <video src={url} controls className="max-h-[65vh] w-full rounded-lg bg-black" />;
        case "audio":
            return <audio src={url} controls className="w-full" />;
        case "pdf":
            return <iframe src={url} title={fileName} className="h-[70vh] w-full rounded-lg border bg-white" />;
        case "csv":
        case "xlsx":
            return <TableBody url={url} kind={kind} fileName={fileName} />;
        case "deck":
            return <DeckBody deck={deck} />;
        case "other":
            return null;
    }
}
