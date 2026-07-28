// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.

// Mounts Scalar on the standalone page served by ApiReferenceEndpoints. Scalar
// owns this document (it is framed by /ApiReference), so no scroll/positioning
// workarounds are needed here.

let bundlePromise = null;

function loadBundle() {
    bundlePromise ??= new Promise((resolve, reject) => {
        if (window.Scalar) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.src = '_content/SnapCd.Server.Core/scalar/standalone.min.js';
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Failed to load the Scalar bundle'));
        document.head.appendChild(script);
    });
    return bundlePromise;
}

export async function init(elementId, configuration) {
    await loadBundle();
    // The reference follows the app theme (mud-dark on <html>, stamped
    // server-side from the theme cookie); Scalar's own toggle is hidden.
    configuration.darkMode = document.documentElement.classList.contains('mud-dark');
    configuration.hideDarkModeToggle = true;
    window.Scalar.createApiReference('#' + elementId, configuration);
    wireAuthShortcut(elementId);
}

// Clicking an "Authentication required" badge (intro card or per-operation
// indicator) triggers the Authorize button. Capture phase: the badge's own
// handler stops propagation. Scalar may need to render the auth card first,
// so retry briefly until the button exists.
function wireAuthShortcut(elementId) {
    const root = document.getElementById(elementId);
    if (!root) return;
    root.addEventListener('click', e => {
        if (!e.target.closest('.authenticationRequired, [data-testid="auth-indicator"]')) return;
        const clickAuthorize = attempt => {
            const button = [...root.querySelectorAll('button')]
                .find(b => b.textContent.trim().toLowerCase() === 'authorize');
            if (button) button.click();
            else if (attempt < 20) setTimeout(() => clickAuthorize(attempt + 1), 100);
        };
        clickAuthorize(0);
    }, true);
}
