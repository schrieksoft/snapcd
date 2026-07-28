# Vendored Scalar API Reference bundle

- `standalone.min.js` — `@scalar/api-reference` **1.63.0** (browser standalone build),
  self-hosted so the app never loads from a CDN.
- `scalar-interop.js` — Blazor interop: loads the bundle once and mounts/destroys the
  reference (used by `UI/Dashboard/Pages/Account/Interactive/ApiReference.razor`).

## Updating

```bash
curl -sL -o standalone.min.js \
  "https://cdn.jsdelivr.net/npm/@scalar/api-reference@<VERSION>/dist/browser/standalone.min.js"
```

Then update the version above and verify `/ApiReference` still renders, authorizes via
SSO, and executes a try-it call (plan: `_planning/dev/16-api-scalar/plan.md`).
