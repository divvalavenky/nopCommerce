---
name: verify-nopcommerce
description: Drive the nopCommerce web storefront and admin UI with an isolated PostgreSQL-backed instance. Use when proving a user-visible change works end to end (install, browse, search, cart, admin login), not for unit tests alone.
---

# Verify nopCommerce

Project-local verification for the **nopCommerce web UI** (public storefront + `/Admin`). Secondary surfaces exist (Web API plugin, mobile app plugin, unit tests under `src/Tests`); this skill does not drive those.

Agents read this cold mid-task. Follow Launch → Doctor → Drive → Evidence → Cleanup. Feature recipes live in `features/`.

## Launch

Every verification run owns its own process, port, app directory, and PostgreSQL database. Never drive the environment `start` script instance on port `5000` or any other foreign listener.

```bash
RUN_ID="r$(date +%s)"
CONTROL=".cursor/skills/verify-nopcommerce/scripts/control-nopcommerce"
chmod +x "$CONTROL"

# First launch builds NopCommerce.sln (so Plugins copy into Nop.Web) then publishes
# to /tmp/nopcommerce-verify/publish and copies a per-run app tree under
# /tmp/nopcommerce-verify/runs/<id>/app
"$CONTROL" launch --run-id "$RUN_ID" --port 5055

# Fresh DB install with sample catalog (Build your own computer, Apple MacBook Pro, …)
"$CONTROL" install --run-id "$RUN_ID"
```

Ready means:

1. `launch` prints JSON with `url` (default `http://127.0.0.1:5055`) and `pid`.
2. `/install` served the wizard before `install`.
3. After `install`, `App_Data/appsettings.json` has a non-empty `ConnectionStrings.ConnectionString` under the run’s app dir, the homepage shows storefront chrome (`.header-logo`, search box), and sample products are visible when sample data was requested (default). Install triggers an app-domain restart; `control-nopcommerce` relaunches the same run’s process automatically.

Environment knobs (optional): `NOP_VERIFY_HOME`, `NOP_VERIFY_PORT`, `NOP_VERIFY_PG_USER` (default `nopcommerce`), `NOP_VERIFY_PG_PASSWORD` (default `nopcommerce`), `NOP_VERIFY_ADMIN_EMAIL` (default `admin@yourStore.com`), `NOP_VERIFY_ADMIN_PASSWORD` (default `Verify_Pass_123`), `NOP_VERIFY_CHROME`.

PostgreSQL must accept connections (this Cloud Agent `start` script creates role/db extensions). `launch` creates `nopcommerce_v_<run_id>` and extensions `citext` + `pgcrypto`.

Isolation rules:

- Refuse to bind a port that already has a listener.
- Refuse to reuse an existing `--run-id` without `cleanup`.
- Concurrent runs are supported only with distinct `--run-id` and `--port` values. Do not point two runs at the same app directory.

Teardown is `cleanup` (below). Leave proof under `.cursor/skills/verify-nopcommerce/artifacts/<run-id>/`.

## Doctor

Read-only. Run first when anything looks off:

```bash
"$CONTROL" doctor --run-id "$RUN_ID"
```

Pass criteria: state file owned by `verify-nopcommerce`, PID alive and executing `Nop.Web.dll`, URL reachable, `installed` flag matches a non-empty `ConnectionStrings.ConnectionString` in `App_Data/appsettings.json`, connection string references this run’s database name.

Exit code `0` = healthy; `2` = unhealthy (JSON lists `issues`).

## Drive

Harness: Playwright Chromium via `control-nopcommerce browser`, using stable CSS/ARIA from DefaultClean + admin AdminLTE — not coordinates.

```bash
# Storefront search
"$CONTROL" browser --run-id "$RUN_ID" search --query "MacBook" --expect "Apple MacBook Pro"

# Open a product details page
"$CONTROL" browser --run-id "$RUN_ID" open-product --name "Apple MacBook Pro"

# Add a simple product to cart and open /cart
"$CONTROL" browser --run-id "$RUN_ID" add-to-cart-simple --name "First Prize Pies"

# Admin login (returnUrl=/admin)
"$CONTROL" browser --run-id "$RUN_ID" admin-login

# Generic navigation / assertions
"$CONTROL" browser --run-id "$RUN_ID" content --url "/" --expect "header-logo"
"$CONTROL" browser --run-id "$RUN_ID" screenshot --url "/cart" --path .cursor/skills/verify-nopcommerce/artifacts/$RUN_ID/cart-manual.png
```

Stable handles:

| Surface | Handle |
|---|---|
| Search input | `input.search-box-text` (ARIA label from `Search.SearchBox.Text.Label`) |
| Search submit | `button.search-box-button` |
| Product title on card | `h2.product-title a` / role `link` by product name |
| Add to cart (details) | `button.add-to-cart-button` |
| Header cart | `a.ico-cart` → `/cart` |
| Customer login | `#Email`, `#Password`, `button.login-button` at `/login` |
| Admin entry | `/Admin` or link `.administration` |
| Install wizard | `#AdminEmail`, `#AdminPassword`, `#DataProvider` value `3` (PostgreSQL), `#ServerName`, `#DatabaseName`, `#Username`, `#Password`, `button.btn-install` |

Read `features/README.md` then the matching feature file before claiming a path is verified.

## Evidence

Proof root: `.cursor/skills/verify-nopcommerce/artifacts/<run-id>/` (gitignored; survives `cleanup`). A checked-in sample from the first proof run lives under `examples/proof1/` (homepage + MacBook search screenshots).

Standards:

- Exercise the real user path (install wizard, search box, product page, cart, login form). Do not call internal services or test-only endpoints to fake the outcome.
- Capture the action and the resulting state (screenshot + HTML of the result page), not only a final dashboard.
- Verify side effects where relevant: cart HTML contains the product name; `dataSettings.json` exists after install; admin URL lands under `/admin`.
- Mocks are not used here; external payment/shipping providers are out of scope for these recipes.

## Cleanup

```bash
"$CONTROL" cleanup --run-id "$RUN_ID"
```

Kills only the PID recorded in the run state (SIGTERM then SIGKILL). Drops `nopcommerce_v_<run_id>`. Removes `/tmp/nopcommerce-verify/runs/<run-id>/`. **Does not** delete `artifacts/<run-id>/`.

Never `pkill -f Nop.Web` or kill by process name — that can hit the environment start instance.

## Helpers

Executable: `.cursor/skills/verify-nopcommerce/scripts/control-nopcommerce`

```text
control-nopcommerce launch  --run-id <id> [--port 5055]
control-nopcommerce doctor  --run-id <id>
control-nopcommerce install --run-id <id>
control-nopcommerce browser --run-id <id> <action> [flags]
control-nopcommerce cleanup --run-id <id>
```

Requires: .NET SDK matching `global.json`, PostgreSQL, Python 3 with `playwright`, Chrome at `/usr/local/bin/google-chrome` (or `NOP_VERIFY_CHROME`).
