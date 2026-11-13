const STORAGE_KEY = 'snapcd.tree.pin';

/** The pinned stack or namespace, or null. Storage can be blocked, which only costs the pin. */
export function read() {
    try {
        return window.localStorage.getItem(STORAGE_KEY);
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
