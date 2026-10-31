import { useEffect } from "react";

import { Button } from "@/design/button";
import { cn } from "@/design/classNames";

function typingInside(target: EventTarget | null): boolean {
    return target instanceof HTMLElement && (target.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName));
}

/** One large slide, "Slide n / N", previous and next (also the arrow keys), and a strip of thumbnails. Takes image URLs only. */
export function SlideViewer({
    slides,
    index,
    onIndexChange,
    keyboard = true,
    compact = false,
    className,
}: {
    slides: string[];
    index: number;
    onIndexChange: (index: number) => void;
    keyboard?: boolean;
    /** Smaller slide, for a viewer inside a dialog. */
    compact?: boolean;
    className?: string;
}) {
    const last = slides.length - 1;
    const current = Math.min(Math.max(index, 0), last);

    useEffect(() => {
        if (!keyboard) return;
        const onKey = (event: KeyboardEvent) => {
            if (typingInside(event.target) || event.metaKey || event.ctrlKey || event.altKey) return;
            if (event.key === "ArrowRight" && current < last) onIndexChange(current + 1);
            if (event.key === "ArrowLeft" && current > 0) onIndexChange(current - 1);
        };
        window.addEventListener("keydown", onKey);
        return () => {
            window.removeEventListener("keydown", onKey);
        };
    }, [keyboard, current, last, onIndexChange]);

    const url = slides[current];
    if (url === undefined) return null;

    return (
        <div className={cn("flex flex-col gap-3", className)}>
            <div className="flex items-center justify-center rounded-xl border bg-muted/50 p-2 sm:p-4">
                <img
                    src={url}
                    alt={`Slide ${current + 1} of ${slides.length}`}
                    className={cn("w-auto max-w-full rounded-md bg-white shadow-md", compact ? "max-h-[46vh]" : "max-h-[62vh]")}
                    data-testid="slide-image"
                />
            </div>
            <div className="flex items-center justify-center gap-3">
                <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                        onIndexChange(current - 1);
                    }}
                    disabled={current === 0}
                >
                    ← Previous
                </Button>
                <p className="min-w-24 text-center text-sm text-muted-foreground" data-testid="slide-position">
                    Slide {current + 1} / {slides.length}
                </p>
                <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                        onIndexChange(current + 1);
                    }}
                    disabled={current === last}
                >
                    Next →
                </Button>
            </div>
            <ul aria-label="Slides" className="flex gap-2 overflow-x-auto pb-2">
                {slides.map((thumbnail, position) => (
                    <li key={thumbnail} className="shrink-0">
                        <button
                            type="button"
                            aria-label={`Go to slide ${position + 1}`}
                            aria-current={position === current ? "true" : undefined}
                            onClick={() => {
                                onIndexChange(position);
                            }}
                            className={cn(
                                "relative block overflow-hidden rounded-md border-2 bg-white transition",
                                position === current ? "border-primary" : "border-transparent opacity-70 hover:opacity-100",
                            )}
                        >
                            <img src={thumbnail} alt="" loading="lazy" className="h-16 w-auto sm:h-20" />
                            <span className="absolute bottom-0.5 left-0.5 rounded bg-black/60 px-1 text-[10px] text-white">
                                {position + 1}
                            </span>
                        </button>
                    </li>
                ))}
            </ul>
        </div>
    );
}

/** A row of the first few slides as small pictures. */
export function ThumbnailRow({ slides, max = 6, className }: { slides: string[]; max?: number; className?: string }) {
    return (
        <ul className={cn("flex gap-1.5 overflow-hidden", className)} data-testid="thumbnails">
            {slides.slice(0, max).map((url) => (
                <li key={url} className="shrink-0 overflow-hidden rounded border bg-white">
                    <img src={url} alt="" loading="lazy" className="h-12 w-auto" />
                </li>
            ))}
        </ul>
    );
}
