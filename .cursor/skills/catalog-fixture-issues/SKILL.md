---
name: catalog-fixture-issues
description: Find catalog/product issues from the same-category fixture or skill-run report and post them as PR comments (independent of Bugbot). Use when a PR touches docs/fixtures/compare-same-category-products, when the user asks to find fixture catalog issues, or when an automation should surface product issues from sample-catalog / skill-run-report without waiting for Bugbot code findings.
---

# Catalog fixture issues (not Bugbot)

Surfaces **product/catalog issues** from the editable fixture used by `compare-same-category-products`. This path does **not** require Bugbot findings.

## When this applies

| Signal | Action |
|--------|--------|
| PR/diff touches `docs/fixtures/compare-same-category-products/**` | Run this skill |
| User/automation says find/fix **fixture** or **catalog** issues | Run this skill |
| Bugbot is clean on a docs-only PR | Still run this skill if the fixture/report is in scope |

Do **not** confuse with `bugbot-evidence` (that skill gates on Bugbot code findings only).

## Hard gate

1. Locate source data (prefer in order):
   - `docs/fixtures/compare-same-category-products/skill-run-report.md` if present and up to date with the catalog
   - else run `/compare-same-category-products` on `sample-catalog.csv` or `sample-catalog.md`
2. Parse the **Product issues** table (or regenerated issue list).
3. **If zero critical/high issues → STOP.** Post nothing (or a single “no critical/high catalog issues” chat note). Do not spam medium/low-only unless the user asked for all severities.
4. **If ≥1 critical or high issue →** post PR comments (and optional evidence).

## Workflow

```
Task Progress:
- [ ] 1. Confirm fixture paths in the PR/branch
- [ ] 2. Load skill-run-report or re-run compare-same-category-products
- [ ] 3. Gate on critical/high count
- [ ] 4. Post rollup PR comment (+ optional per-critical comments)
- [ ] 5. Optional: screenshot tables / report sections as evidence
- [ ] 6. If user asked to fix: edit sample-catalog.md + .csv, refresh report, push
```

### 1–2. Load issues

- Paths (repo-relative):
  - `docs/fixtures/compare-same-category-products/sample-catalog.md`
  - `docs/fixtures/compare-same-category-products/sample-catalog.csv`
  - `docs/fixtures/compare-same-category-products/skill-run-report.md`
- Prefer regenerating via `compare-same-category-products` when the catalog changed without a matching report update.
- Keep issue codes from that skill’s `issues.md` model (`BUY_OOS_PUBLISHED`, `PRICE_NEGATIVE`, etc.).

### 3. Gate

Default publish set = **critical + high** only. Include medium/low only when the user or automation prompt says “all severities”.

### 4. PR comments

Use `ManagePullRequest` → `post_comment` on the PR.

- Always post **one rollup comment** (see [comment-template.md](comment-template.md)).
- Optionally add one extra comment per **critical** finding (max 5).
- Prefix with `Catalog fixture issues` so routers can tell these from Bugbot threads.
- Link paths to files in the PR (`sample-catalog.md`, `skill-run-report.md`).

### 5. Evidence (optional)

When the automation or user asks for screenshots/videos:

- Capture the **Product issues** section / table (or the seeded-issues checklist) as PNG under `/opt/cursor/artifacts/`.
- Embed with `<img src="/opt/cursor/artifacts/...">` in the PR comment.
- Do **not** require a live storefront for document-only fixtures.

### 6. Fix mode (only if asked)

If the user/automation says **fix** fixture issues:

- Edit `sample-catalog.md` and mirror in `sample-catalog.csv`.
- Update the **Seeded issues (expected)** checklist when removing intentional defects.
- Re-run comparison and refresh `skill-run-report.md`.
- Commit/push on the PR branch. Do not clear intentional sandbox rows in `QA > Edge cases` unless explicitly requested.

## Anti-patterns

- Waiting for Bugbot before reporting fixture catalog issues
- Treating skill-report “product issues” as Bugbot code defects
- Posting medium/low noise by default
- Fixing seeded QA defects without an explicit fix request

## Automation

Paste-ready Cursor Automation prompt: [AUTOMATION.md](AUTOMATION.md)

## Related skills

- `compare-same-category-products` — produces the analysis / issue codes
- `bugbot-evidence` — Bugbot-only visual evidence (separate gate)
