const STORAGE_KEY = 'snapcd.tree.pin';

const PATTERN = /^(stack:[^/]+|namespace:[^/]+\/[^/]+)$/;

/**
 * The pinned stack or namespace, or null. A value that is not in the shape this module writes is
 * discarded rather than handed on, so a stale or hand-edited entry cannot reach the server.
 * Storage can also be blocked outright, which only costs the pin.
 */
export function read() {
    try {
        const raw = window.localStorage.getItem(STORAGE_KEY);

        if (raw === null) return null;

        if (typeof raw !== 'string' || raw.length > 512 || !PATTERN.test(raw)) {
            window.localStorage.removeItem(STORAGE_KEY);
            return null;
        }

        return raw;
    } catch {
        return null;
    }
}

export function write(value) {
    try {
        if (value) {
            window.localStorage.setItem(STORAGE_KEY, value);
        } else {
            window.localStorage.removeItem(STORAGE_KEY);
        }
    } catch {
        // A private window or blocked site data only costs the remembered pin.
    }
}
