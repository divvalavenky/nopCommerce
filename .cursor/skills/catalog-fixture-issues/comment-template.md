# PR comment templates (catalog fixture issues)

## Rollup (required when critical/high > 0)

```markdown
### Catalog fixture issues (not Bugbot)

Source: `docs/fixtures/compare-same-category-products/`  
Skill: `compare-same-category-products` / `catalog-fixture-issues`  
Counts: critical=<N>, high=<N> (medium/low omitted unless requested)

| Severity | Code | Target | Suggested fix |
| --- | --- | --- | --- |
| critical | `CODE` | <target> | <fix> |
| high | `CODE` | <target> | <fix> |

Full table: `docs/fixtures/compare-same-category-products/skill-run-report.md`

These are **seeded catalog/data issues** in the fixture document, not Cursor Bugbot code-review findings.
```

Optional evidence:

```html
<img alt="Catalog fixture issues table" src="/opt/cursor/artifacts/catalog_fixture_issues.png" />
```

## Per-critical comment (optional)

```markdown
### Catalog fixture · critical · `CODE`

**Target:** <product or group>  
**Evidence:** <from report>  
**Fix:** <suggested fix>  
**File:** `docs/fixtures/compare-same-category-products/sample-catalog.md`
```
