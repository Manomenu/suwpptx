/**
 * What every feature's HTTP calls share: the API's base path, `request`, and the error a response turns into.
 * The calls themselves live with their feature (`<feature>/api.ts`); their types come from the generated `openapi.d.ts`.
 */

const BASE = import.meta.env.VITE_API_BASE ?? "/api";

/** The error a failed response carries: the server's ProblemDetails `detail` when it sent one. */
export async function toError(response: Response): Promise<Error> {
    const body = (await response.json().catch(() => null)) as { detail?: unknown } | null;
    const detail = typeof body?.detail === "string" ? body.detail : null;
    return new Error(detail ?? `${response.status} ${response.statusText}`);
}

/** A JSON call to the API: the parsed body, or the error the response carries. */
export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const response = await fetch(`${BASE}${path}`, {
        ...init,
        headers: init.body === undefined ? {} : { "content-type": "application/json" },
    });
    if (!response.ok) throw await toError(response);
    return (response.status === 204 ? undefined : await response.json()) as T;
}
