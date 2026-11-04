# Fixture: same-category product comparison

Editable catalog used to exercise the `compare-same-category-products` skill on **document** input (no live DB required).

## Files

| File | Role |
|------|------|
| [sample-catalog.md](./sample-catalog.md) | Human-editable Markdown tables + seeded-issue checklist (prefer for PR review) |
| [sample-catalog.csv](./sample-catalog.csv) | Same rows for spreadsheet / script import — keep in sync with the Markdown |

Both files are plain text so they stay editable through PR review and Bot comments.

## How to run the skill against this fixture

1. Attach or invoke `/compare-same-category-products`.
2. Point the agent at `docs/fixtures/compare-same-category-products/sample-catalog.md` (or the CSV).
3. Expect peer-group scoring **and** product-issue detection against the seeded checklist in the Markdown.

## Editing rules for PRs

- Keep the shared column set stable across category tables.
- When adding or fixing a seeded defect, update the **Seeded issues (expected)** table in the Markdown.
- Mirror row changes in both `.md` and `.csv`.
- Leave intentional blanks empty — do not fill with `N/A` unless removing the related expected issue.
