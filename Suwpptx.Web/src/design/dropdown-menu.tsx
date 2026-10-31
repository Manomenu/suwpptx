import * as React from "react";
import { DropdownMenu as DropdownMenuPrimitive } from "radix-ui";

import { cn } from "@/design/classNames";

const DropdownMenu = DropdownMenuPrimitive.Root;
const DropdownMenuTrigger = DropdownMenuPrimitive.Trigger;

function DropdownMenuContent({ className, ...props }: React.ComponentProps<typeof DropdownMenuPrimitive.Content>) {
    return (
        <DropdownMenuPrimitive.Portal>
            <DropdownMenuPrimitive.Content
                align="end"
                sideOffset={4}
                className={cn("z-50 min-w-36 rounded-lg border bg-card p-1 text-card-foreground shadow-md", className)}
                {...props}
            />
        </DropdownMenuPrimitive.Portal>
    );
}

function DropdownMenuItem({
    className,
    destructive = false,
    ...props
}: React.ComponentProps<typeof DropdownMenuPrimitive.Item> & { destructive?: boolean }) {
    return (
        <DropdownMenuPrimitive.Item
            className={cn(
                "flex cursor-default items-center rounded-md px-2.5 py-1.5 text-sm outline-none select-none data-[highlighted]:bg-accent",
                destructive && "text-destructive",
                className,
            )}
            {...props}
        />
    );
}

export { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger };
