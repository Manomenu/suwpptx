/** "10 min ago", "3 days ago": how long ago `date` was, as of `now`. */
export function relativeTime(date: Date | string, now: Date = new Date()): string {
    const seconds = Math.round((now.getTime() - new Date(date).getTime()) / 1000);
    if (seconds < 45) return "just now";
    const minutes = Math.round(seconds / 60);
    if (minutes < 60) return `${minutes} min ago`;
    const hours = Math.round(minutes / 60);
    if (hours < 24) return `${hours} ${hours === 1 ? "hour" : "hours"} ago`;
    const days = Math.round(hours / 24);
    if (days < 30) return `${days} ${days === 1 ? "day" : "days"} ago`;
    const months = Math.round(days / 30);
    if (months < 12) return `${months} ${months === 1 ? "month" : "months"} ago`;
    const years = Math.round(days / 365);
    return `${years} ${years === 1 ? "year" : "years"} ago`;
}

/** "48 KB", "3.1 MB": a file size for people. */
export function formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    const units = ["KB", "MB", "GB", "TB"];
    let value = bytes / 1024;
    let unit = 0;
    while (value >= 1024 && unit < units.length - 1) {
        value /= 1024;
        unit++;
    }
    return `${value < 10 ? value.toFixed(1).replace(/\.0$/, "") : Math.round(value)} ${units[unit]}`;
}

/** "1 file", "14 files". */
export function plural(count: number, one: string, many = `${one}s`): string {
    return `${count} ${count === 1 ? one : many}`;
}
