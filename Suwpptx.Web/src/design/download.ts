/** Starts the browser's download of a URL (for files the server sends as an attachment). */
export function download(url: string) {
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = "";
    document.body.append(anchor);
    anchor.click();
    anchor.remove();
}
