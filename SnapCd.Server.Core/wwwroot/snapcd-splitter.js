const STORAGE_KEY = 'snapcd.tree.width';
// The tree scrolls rather than reflowing, so the pane may be dragged almost shut.
const MIN_PERCENT = 2;
const MAX_PERCENT = 85;

function clamp(value) {
    return Math.min(MAX_PERCENT, Math.max(MIN_PERCENT, value));
}

/** A value that cannot be read is discarded, so a bad one is not re-read on every visit. */
function read() {
    try {
        const raw = window.localStorage.getItem(STORAGE_KEY);

        if (raw === null) return null;

        const stored = parseFloat(raw);

        if (isNaN(stored)) {
            window.localStorage.removeItem(STORAGE_KEY);
            return null;
        }

        return clamp(stored);
    } catch {
        return null;
    }
}

function write(value) {
    try {
        window.localStorage.setItem(STORAGE_KEY, String(value));
    } catch {
        // A private window or blocked site data only costs the remembered width.
    }
}

/**
 * Binds the handle inside `root` so dragging it resizes the two columns. The width is written
 * straight to a CSS variable, so a drag never reaches the server.
 */
export function attach(root) {
    if (!root || root.dataset.splitterBound === 'true') return;

    const handle = root.querySelector('[data-splitter-handle]');

    if (!handle) return;

    root.dataset.splitterBound = 'true';

    const stored = read();

    if (stored !== null) root.style.setProperty('--tree-width', stored + '%');

    let dragging = false;

    const apply = (clientX) => {
        const bounds = root.getBoundingClientRect();

        if (bounds.width === 0) return 0;

        const percent = clamp(((clientX - bounds.left) / bounds.width) * 100);
        root.style.setProperty('--tree-width', percent + '%');
        return percent;
    };

    const onMove = (event) => {
        if (!dragging) return;

        event.preventDefault();
        apply(event.clientX);
    };

    const onUp = (event) => {
        if (!dragging) return;

        dragging = false;
        root.classList.remove('is-splitting');
        document.body.style.userSelect = '';
        write(apply(event.clientX));
    };

    handle.addEventListener('pointerdown', (event) => {
        dragging = true;
        root.classList.add('is-splitting');
        document.body.style.userSelect = 'none';
        handle.setPointerCapture?.(event.pointerId);
        event.preventDefault();
    });

    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
    handle.addEventListener('pointercancel', onUp);

    // Double-click returns the split to the proportion the page ships with.
    handle.addEventListener('dblclick', () => {
        root.style.removeProperty('--tree-width');

        try {
            window.localStorage.removeItem(STORAGE_KEY);
        } catch {
            // Nothing to clear when storage is unavailable.
        }
    });
}
