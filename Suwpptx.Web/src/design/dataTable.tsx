/** Rows of cells as a table; the first row is the header. */
export function DataTable({ rows, truncated }: { rows: string[][]; truncated: boolean }) {
    const [header, ...body] = rows;
    if (header === undefined) return <p className="text-sm text-muted-foreground">This file is empty.</p>;
    return (
        <div className="flex flex-col gap-2">
            <div className="max-h-[60vh] overflow-auto rounded-lg border">
                <table className="w-full border-collapse text-left text-sm">
                    <thead className="sticky top-0 bg-muted">
                        <tr>
                            {header.map((cell, column) => (
                                <th key={column} className="border-b px-3 py-2 font-medium whitespace-nowrap">
                                    {cell}
                                </th>
                            ))}
                        </tr>
                    </thead>
                    <tbody>
                        {body.map((row, index) => (
                            <tr key={index} className="odd:bg-muted/30">
                                {row.map((cell, column) => (
                                    <td key={column} className="px-3 py-1.5 whitespace-nowrap">
                                        {cell}
                                    </td>
                                ))}
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
            {truncated && <p className="text-xs text-muted-foreground">Showing the first {rows.length} rows.</p>}
        </div>
    );
}
