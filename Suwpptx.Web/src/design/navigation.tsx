import * as React from "react";

/** Navigation without a router library: the History API, plus a hook that re-renders on every location change. */
const EVENT = "suwpptx:navigate";

export function navigate(to: string) {
    window.history.pushState(null, "", to);
    window.dispatchEvent(new Event(EVENT));
}

function subscribe(onChange: () => void) {
    window.addEventListener(EVENT, onChange);
    window.addEventListener("popstate", onChange);
    return () => {
        window.removeEventListener(EVENT, onChange);
        window.removeEventListener("popstate", onChange);
    };
}

export function usePathname(): string {
    return React.useSyncExternalStore(subscribe, () => window.location.pathname);
}

/** A real link (open in a new tab works); a plain click navigates without reloading the page. */
export function Link({ href, onClick, ...props }: React.ComponentProps<"a"> & { href: string }) {
    return (
        <a
            href={href}
            onClick={(event) => {
                onClick?.(event);
                if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey)
                    return;
                event.preventDefault();
                navigate(href);
            }}
            {...props}
        />
    );
}
