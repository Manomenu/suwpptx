import type { ReactNode } from "react";

import { cn } from "@/design/classNames";
import { Link } from "@/design/navigation";

const NAV = [
    { href: "/", label: "Workspaces", key: "workspaces" },
    { href: "/templates", label: "Templates", key: "templates" },
] as const;

/** The top bar every screen shares, and the page under it. */
export function AppShell({ current, hint, children }: { current: "workspaces" | "templates" | null; hint: string; children: ReactNode }) {
    return (
        <div className="flex min-h-screen flex-col">
            <header className="border-b bg-card">
                <div className="mx-auto flex max-w-7xl flex-wrap items-center gap-x-8 gap-y-2 px-6 py-3">
                    <Link href="/" className="text-base font-semibold tracking-tight">
                        Suwpptx Studio
                    </Link>
                    <nav aria-label="Main" className="flex gap-1">
                        {NAV.map((item) => (
                            <Link
                                key={item.key}
                                href={item.href}
                                aria-current={current === item.key ? "page" : undefined}
                                className={cn(
                                    "rounded-md px-3 py-1.5 text-sm text-muted-foreground hover:bg-accent hover:text-accent-foreground",
                                    current === item.key && "bg-accent font-medium text-accent-foreground",
                                )}
                            >
                                {item.label}
                            </Link>
                        ))}
                    </nav>
                    <p className="ml-auto font-mono text-xs text-muted-foreground" data-testid="root-hint">
                        {hint}
                    </p>
                </div>
            </header>
            <main className="mx-auto flex w-full max-w-7xl flex-1 flex-col px-6 py-6">{children}</main>
        </div>
    );
}
