/** The screens of the app and the URLs they live at. Plain functions, so the rules can be tested without a browser. */
export type Route = { page: "workspaces" } | { page: "workspace"; name: string } | { page: "templates" } | { page: "notFound" };

export function parseRoute(pathname: string): Route {
    const segments = pathname.split("/").filter((segment) => segment !== "");
    const [first, second, ...rest] = segments;
    if (first === undefined) return { page: "workspaces" };
    if (first === "templates" && second === undefined) return { page: "templates" };
    if (first === "workspaces" && second !== undefined && rest.length === 0) {
        try {
            return { page: "workspace", name: decodeURIComponent(second) };
        } catch {
            return { page: "notFound" };
        }
    }
    return { page: "notFound" };
}
