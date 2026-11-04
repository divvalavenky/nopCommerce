# Cursor Automation setup (catalog fixture issues)

Bugbot will stay clean on docs-only PRs. Use a **separate** automation that reads the fixture / skill-run report.

## Create the automation

1. Open [Cursor Automations](https://cursor.com/automations) (or your team automations dashboard).
2. Create automation, e.g. name: **Catalog fixture issues**.
3. **Trigger:** Pull request opened/updated (same repo: `divvalavenky/nopCommerce`), optionally only when paths include `docs/fixtures/compare-same-category-products/**`.
4. **Prompt:** paste the block below.
5. Enable the automation.

## Prompt (paste verbatim)

```text
You are the Catalog fixture issues automation for nopCommerce.

Do NOT wait for Cursor Bugbot. Do NOT use the bugbot-evidence skill gate.

1. Read the project skill `.cursor/skills/catalog-fixture-issues/SKILL.md` and follow it.
2. If this PR/branch touches `docs/fixtures/compare-same-category-products/**` OR the PR body/title mentions same-category / skill-run / sample-catalog, continue; otherwise stop with a one-line note.
3. Load `docs/fixtures/compare-same-category-products/skill-run-report.md` if present; otherwise run `/compare-same-category-products` on `sample-catalog.csv`.
4. If there are zero critical and zero high catalog issues, stop (no PR comments).
5. Otherwise post ONE rollup PR comment using `.cursor/skills/catalog-fixture-issues/comment-template.md` via ManagePullRequest post_comment. Include critical+high only.
6. Optionally attach a screenshot of the issues table under /opt/cursor/artifacts/ and embed it in the comment.
7. Only fix the fixture if the triggering comment or automation input explicitly says "fix". Finding/listing is the default.

Remember: these are seeded catalog issues in the fixture document, not Bugbot code findings.
```

## How it differs from Bugbot automations

| Automation | Gate | Source of “issues” |
|------------|------|--------------------|
| Bugbot identified issues | Bugbot review findings ≥ 1 | Code diff |
| Catalog fixture issues | critical/high rows in skill-run-report / comparison | `sample-catalog` document |
| bugbot-evidence skill | Bugbot findings ≥ 1 | Screenshots of Bugbot bugs |

## Manual test

On an open PR that contains the fixture report (e.g. PR with `skill-run-report.md`):

- Comment: `Find catalog fixture issues` or run an agent with `/catalog-fixture-issues`.
- Expect a PR comment titled **Catalog fixture issues (not Bugbot)** listing critical/high rows.
