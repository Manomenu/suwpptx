/** The delimiter of a CSV text: `;` when the first line has more of them than commas (Excel in many locales), else `,`. */
export function detectDelimiter(text: string): "," | ";" {
    const firstLine = text.split(/\r?\n/, 1)[0] ?? "";
    const semicolons = firstLine.split(";").length;
    const commas = firstLine.split(",").length;
    return semicolons > commas ? ";" : ",";
}

/** Parses delimited text into rows of cells: quoted cells may hold delimiters, line breaks and `""`. Stops after `maxRows`. */
export function parseDelimited(text: string, delimiter: string, maxRows: number): string[][] {
    const rows: string[][] = [];
    let row: string[] = [];
    let cell = "";
    let quoted = false;
    const source = text.startsWith("﻿") ? text.slice(1) : text;

    const endRow = () => {
        row.push(cell);
        cell = "";
        if (row.length > 1 || row[0] !== "") rows.push(row);
        row = [];
    };

    for (let i = 0; i < source.length && rows.length < maxRows; i++) {
        const char = source[i] ?? "";
        if (quoted) {
            if (char !== '"') cell += char;
            else if (source[i + 1] === '"') {
                cell += '"';
                i++;
            } else quoted = false;
        } else if (char === '"' && cell === "") quoted = true;
        else if (char === delimiter) {
            row.push(cell);
            cell = "";
        } else if (char === "\n" || char === "\r") {
            if (char === "\r" && source[i + 1] === "\n") i++;
            endRow();
        } else cell += char;
    }
    if (rows.length < maxRows && (cell !== "" || row.length > 0)) endRow();
    return rows;
}
