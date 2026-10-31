/**
 * What every feature's HTTP calls share: the API's base path, `request`, and the error a response turns into.
 * The calls themselves live with their feature (`<feature>/api.ts`); their types come from the generated `openapi.d.ts`.
 */

const BASE = import.meta.env.VITE_API_BASE ?? "/api";

/** A failed response: the message is the server's ProblemDetails `detail` (what the caller can do about it), `status` tells the kinds apart. */
export class ApiError extends Error {
    readonly status: number;

    constructor(message: string, status: number) {
        super(message);
        this.status = status;
    }
}

/** The URL of an API path, for `src` and `href` attributes and for `fetch`. */
export function apiUrl(path: string): string {
    return `${BASE}${path}`;
}

/** The error a failed response carries: the server's ProblemDetails `detail` when it sent one. */
export async function toError(response: Response): Promise<ApiError> {
    const body = (await response.json().catch(() => null)) as { detail?: unknown } | null;
    const detail = typeof body?.detail === "string" ? body.detail : null;
    return new ApiError(detail ?? `${response.status} ${response.statusText}`, response.status);
}

/** A JSON call to the API: the parsed body, or the error the response carries. */
export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const response = await fetch(apiUrl(path), {
        ...init,
        headers: init.body === undefined ? {} : { "content-type": "application/json" },
    });
    if (!response.ok) throw await toError(response);
    return (response.status === 204 ? undefined : await response.json()) as T;
}
