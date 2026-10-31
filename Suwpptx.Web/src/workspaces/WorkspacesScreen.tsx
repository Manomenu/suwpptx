import { useState, type SyntheticEvent } from "react";

import { useLive } from "@/api/useLive";
import { toNumber } from "@/api/paths";
import { Alert } from "@/design/alert";
import { Button } from "@/design/button";
import { Card } from "@/design/card";
import { ConfirmDialog } from "@/design/confirmDialog";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/design/dialog";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/design/dropdown-menu";
import { plural, relativeTime } from "@/design/format";
import { Input } from "@/design/input";
import { Label } from "@/design/label";
import { Link, navigate } from "@/design/navigation";

import { createWorkspace, deleteWorkspace, listWorkspaces, renameWorkspace, type WorkspaceSummary } from "./api";

function summaryLine(workspace: WorkspaceSummary) {
    const files = plural(toNumber(workspace.files), "file");
    if (workspace.latestVersion === null) return `${files} · no deck yet`;
    return `${files} · v${workspace.latestVersion} · ${relativeTime(workspace.changedAt)}`;
}

function NewWorkspaceForm({ onDone }: { onDone: () => void }) {
    const [name, setName] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    const submit = async (event: SyntheticEvent) => {
        event.preventDefault();
        setBusy(true);
        setError(null);
        try {
            const created = await createWorkspace(name);
            navigate(`/workspaces/${encodeURIComponent(created.name)}`);
        } catch (failure) {
            setError(failure instanceof Error ? failure.message : String(failure));
        } finally {
            setBusy(false);
        }
    };

    return (
        <Card className="p-4">
            <form onSubmit={(event) => void submit(event)} className="flex flex-col gap-3">
                <div className="flex flex-col gap-2">
                    <Label htmlFor="new-workspace-name">Name</Label>
                    <Input
                        id="new-workspace-name"
                        value={name}
                        onChange={(event) => {
                            setName(event.target.value);
                        }}
                        autoFocus
                        autoComplete="off"
                        placeholder="q3-board-review"
                        aria-invalid={error !== null}
                    />
                </div>
                {error !== null && <Alert variant="destructive">{error}</Alert>}
                <div className="flex gap-2">
                    <Button type="submit" disabled={busy}>
                        Create
                    </Button>
                    <Button type="button" variant="ghost" onClick={onDone}>
                        Cancel
                    </Button>
                </div>
            </form>
        </Card>
    );
}

function RenameDialog({ workspace, onClose }: { workspace: WorkspaceSummary; onClose: () => void }) {
    const [name, setName] = useState(workspace.name);
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    const submit = async (event: SyntheticEvent) => {
        event.preventDefault();
        setBusy(true);
        setError(null);
        try {
            await renameWorkspace(workspace.name, name);
            onClose();
        } catch (failure) {
            setError(failure instanceof Error ? failure.message : String(failure));
        } finally {
            setBusy(false);
        }
    };

    return (
        <Dialog
            open
            onOpenChange={(open) => {
                if (!open) onClose();
            }}
        >
            <DialogContent aria-describedby={undefined} aria-labelledby={undefined} aria-label="Edit workspace">
                <DialogHeader>
                    <DialogTitle>Rename {workspace.name}</DialogTitle>
                </DialogHeader>
                <form onSubmit={(event) => void submit(event)} className="flex flex-col gap-4">
                    <div className="flex flex-col gap-2">
                        <Label htmlFor="rename-workspace-name">Name</Label>
                        <Input
                            id="rename-workspace-name"
                            value={name}
                            onChange={(event) => {
                                setName(event.target.value);
                            }}
                            autoComplete="off"
                            aria-invalid={error !== null}
                        />
                    </div>
                    {error !== null && <Alert variant="destructive">{error}</Alert>}
                    <DialogFooter>
                        <Button type="button" variant="outline" onClick={onClose}>
                            Cancel
                        </Button>
                        <Button type="submit" disabled={busy}>
                            Rename
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    );
}

/** `/`: every workspace as a card; new, rename and delete. */
export function WorkspacesScreen() {
    const { data: workspaces, error, reload } = useLive(listWorkspaces, "workspaces");
    const [creating, setCreating] = useState(false);
    const [renaming, setRenaming] = useState<WorkspaceSummary | null>(null);
    const [deleting, setDeleting] = useState<WorkspaceSummary | null>(null);

    return (
        <div className="flex flex-col gap-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <h1 className="text-2xl font-semibold tracking-tight">Workspaces</h1>
                {!creating && (
                    <Button
                        onClick={() => {
                            setCreating(true);
                        }}
                    >
                        New workspace
                    </Button>
                )}
            </div>
            {creating && (
                <NewWorkspaceForm
                    onDone={() => {
                        setCreating(false);
                    }}
                />
            )}
            {error !== null && workspaces === undefined && <Alert variant="destructive">Can't load the workspaces: {error.message}</Alert>}
            {workspaces?.length === 0 && (
                <Card className="p-8 text-center text-muted-foreground">
                    No workspaces yet. Create one with <strong className="text-foreground">New workspace</strong>, or make a folder in
                    workspaces/.
                </Card>
            )}
            <ul className="grid gap-3" aria-label="Workspaces">
                {workspaces?.map((workspace) => (
                    <li key={workspace.name}>
                        <Card className="group relative flex items-center gap-3 p-1 transition hover:border-ring">
                            <Link
                                href={`/workspaces/${encodeURIComponent(workspace.name)}`}
                                className="flex min-w-0 flex-1 flex-col gap-0.5 rounded-lg px-4 py-3 outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50"
                            >
                                <span className="truncate font-medium">{workspace.name}</span>
                                <span className="text-sm text-muted-foreground">{summaryLine(workspace)}</span>
                            </Link>
                            <DropdownMenu>
                                <DropdownMenuTrigger asChild>
                                    <Button
                                        variant="ghost"
                                        size="icon"
                                        aria-label={`Actions for ${workspace.name}`}
                                        className="mr-2 text-lg"
                                    >
                                        ⋯
                                    </Button>
                                </DropdownMenuTrigger>
                                <DropdownMenuContent>
                                    <DropdownMenuItem
                                        onSelect={() => {
                                            setRenaming(workspace);
                                        }}
                                    >
                                        Rename
                                    </DropdownMenuItem>
                                    <DropdownMenuItem
                                        destructive
                                        onSelect={() => {
                                            setDeleting(workspace);
                                        }}
                                    >
                                        Delete
                                    </DropdownMenuItem>
                                </DropdownMenuContent>
                            </DropdownMenu>
                        </Card>
                    </li>
                ))}
            </ul>
            {renaming !== null && (
                <RenameDialog
                    key={renaming.name}
                    workspace={renaming}
                    onClose={() => {
                        setRenaming(null);
                        reload();
                    }}
                />
            )}
            <ConfirmDialog
                open={deleting !== null}
                title={`Delete ${deleting?.name ?? ""}?`}
                description={`Delete ${deleting?.name ?? ""} with ${plural(toNumber(deleting?.files ?? 0), "file")} and ${plural(
                    toNumber(deleting?.versions ?? 0),
                    "version",
                )}? This removes the folder and can't be undone.`}
                confirmLabel="Delete"
                destructive
                onConfirm={async () => {
                    if (deleting === null) return;
                    await deleteWorkspace(deleting.name);
                    setDeleting(null);
                    reload();
                }}
                onCancel={() => {
                    setDeleting(null);
                }}
            />
        </div>
    );
}
