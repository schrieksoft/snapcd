You are writing an audit-quality summary of a completed SnapCd module job for a
human reader who didn't watch it happen — e.g., a teammate, a reviewer, or the
person who'll triage drift tomorrow.

Context:
- organizationId: {{organizationId}}
- jobId: {{jobId}}
- moduleId: {{moduleId}}

Pull the source material in this order. Don't gather all of it if a short job
makes most of it irrelevant; aim for "minimum context to write an honest
summary".

1. Read the job's logs from the MCP resource
   `snapcd://orgs/{{organizationId}}/jobs/{{jobId}}/logs`. The resource returns
   a JSON array of entries conforming to `SnapCd.Contracts.Dto.Misc.LogEntryDto` —
   that DTO is the canonical schema; the fields you'll use are `TaskName`,
   `Message`, and `Timestamp`. Partition by `TaskName` (known phases:
   `GetDefinitiveRevision`, `GetModule`, `Init`, `Validate`, `Variables`,
   `Plan`, `ApplyFromPlan`, `DestroyFromPlan`). For "what actually changed",
   look at `TaskName == "ApplyFromPlan"` (or `"DestroyFromPlan"`); the
   resource addresses and counts come from there. **There is also a SnapCd
   `Plan summary:` entry inside the `Plan` task** — a single `Message` that
   starts with `Plan summary:` followed by `- Unchanged:`, `- Create:`,
   `- Modify:`, `- Destroy:`, `- Recreate:` — prefer it for headline counts
   over parsing terraform's own table. Ignore ANSI escapes (`[...m`) and
   routine lifecycle entries (`Now X`, `Completed X`, hook markers,
   `Environment Variables loaded`).
2. The plan that led to this apply is in the same logs resource, filtered to
   `TaskName == "Plan"`. Reconcile planned vs. applied — call out any
   divergence (a resource the plan expected to update but the apply ended up
   replacing). For the output deltas specifically, prefer
   `snapcd://orgs/{{organizationId}}/jobs/{{jobId}}/status` — its
   `OutputsCreate/Modify/Destroy/Recreate/Unchanged` lists are the
   structured form. The same resource also has the resulting
   `ActualStateHeadline` (Applied / Destroyed / Unknown) and `JobType`
   (Apply / Destroy) — useful for the outcome line. Do not invent other URIs.
3. Fetch the approval record(s) from
   `snapcd://orgs/{{organizationId}}/jobs/{{jobId}}/approvals` — returns a
   JSON array of `{PrincipalId, PrincipalType, DecisionDateTime, Declined}`.
   Note each approver, decision, and time. If the array is empty the job
   ran without recorded decisions (auto-applied because the threshold was 0,
   or no approvals were required for the module's scope) — say so explicitly.
   The principal-id is the only identity returned; do not invent a name.
4. If you need the module name, source coordinates, or the state-management
   engine for the summary header, fetch
   `snapcd://orgs/{{organizationId}}/modules/{{moduleId}}/source` — it returns
   `Name`, `SourceType`, `SourceUrl`, `SourceRevision`, `SourceSubdirectory`,
   `Engine`. If you mention CLI commands anywhere (rare in a summary, but
   possible for follow-ups), match them to `Engine` — `tofu` / `terraform` /
   `pulumi`; never guess.
5. Flag anything anomalous: warnings in the logs, resources replaced rather
   than updated, longer-than-expected duration, or approvals from someone
   unexpected for this module's scope.

Output format (strict markdown — this becomes the human-readable record).
Lead with a self-contained summary the reader can scan in one breath; put
the structured detail underneath. **Do not write any wrapping title or
heading** (no `# Job summary — module X`, no module-name banner) — the
rendering UI already identifies the mission and the module. Start
directly with the **Summary** paragraph.

**Summary** — two or three sentences (max). State the outcome
(succeeded / succeeded-with-warnings / no-op), the headline of what changed
(e.g. "3 resources created, 1 replaced" — concrete counts, not just "changes
applied"), and whether anything is worth a closer look. Someone reading just
this paragraph should know what happened without scrolling.

Then the details, in this order:

- **Outcome** — one line: succeeded / succeeded-with-warnings / no-op.
- **What changed** — a tight bulleted list of resources added / changed /
  destroyed, addressed by `aws_*.name` / `module.x.y`. Group by action.
  If there are more than ~10 resources, summarize counts per type and
  expose only the noteworthy ones.
- **Approval** — who approved, when, with what reason; or "auto-applied
  (no approval required)".
- **Anomalies** — bullet list, or "none" if nothing stood out.

Keep it concrete. Do not invent details that aren't in the logs/plan/approvals.
Do not propose changes — this is a record of what happened, not a review.
