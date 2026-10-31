import { AppShell } from "@/design/appShell";
import { Link, usePathname } from "@/design/navigation";
import { TemplatesScreen } from "@/templates/TemplatesScreen";
import { WorkspaceScreen } from "@/workspaces/WorkspaceScreen";
import { WorkspacesScreen } from "@/workspaces/WorkspacesScreen";

import { parseRoute } from "./route";

const ROOT_HINT = "root folder: SUWPPTX_ROOT (default ~/suwpptx)";

/** Puts the page together from features: the route decides which screen is shown. */
export function App() {
    const route = parseRoute(usePathname());

    switch (route.page) {
        case "workspaces":
            return (
                <AppShell current="workspaces" hint={ROOT_HINT}>
                    <WorkspacesScreen />
                </AppShell>
            );
        case "workspace":
            return (
                <AppShell current="workspaces" hint={ROOT_HINT}>
                    <WorkspaceScreen key={route.name} name={route.name} />
                </AppShell>
            );
        case "templates":
            return (
                <AppShell current="templates" hint={ROOT_HINT}>
                    <TemplatesScreen />
                </AppShell>
            );
        case "notFound":
            return (
                <AppShell current={null} hint={ROOT_HINT}>
                    <h1 className="text-2xl font-semibold">Page not found</h1>
                    <Link href="/" className="mt-3 text-sm underline underline-offset-4">
                        ← Back to the workspaces
                    </Link>
                </AppShell>
            );
    }
}
