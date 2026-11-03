# nopCommerce verification map

Maintained source for verifying user-facing nopCommerce behavior. Read this index before driving the app, then use the matching feature file as the recipe.

## Baseline preconditions

- Launch with `.cursor/skills/verify-nopcommerce/scripts/control-nopcommerce launch --run-id <id> --port <free-port>`.
- Run `install` so sample data is present (default admin `admin@yourStore.com` / `Verify_Pass_123`).
- Run `control-nopcommerce doctor --run-id <id>` and require `ok: true`, matching URL, and `installed: true`.
- Never drive an instance that was not started by this verification run (including the Cloud Agent start script on port `5000`).
- Prefer distinct `--run-id` + `--port` for concurrent runs; never share an app directory.

## Driving conventions

- Start every recipe from the baseline installed storefront unless its preconditions say otherwise.
- Prefer the CSS/ARIA handles listed in the skill and feature files over coordinates or tab order.
- Treat every command as literal. Keep quoted product names and flags unchanged.
- Run browser actions through `control-nopcommerce browser`.
- Restore cart/customer state when a mutation would poison a later recipe; do not remove proof artifacts during cleanup.

## Proof and skip reporting

- Capture the user action and the resulting state (HTML + screenshot under `artifacts/<run-id>/…`).
- Mutation proof includes a second user-facing view (for example cart page after add-to-cart).
- Record the feature ID and entry point used with every artifact.
- Report an unreachable path with the attempted command and the unmet precondition.
- Do not report a skipped entry point as verified through a different path.

## Feature entry contract

Each feature file starts with an H1 title and one paragraph describing the user-visible behavior. It then uses exactly four H2 sections in this order:

1. `Sub-features`
2. `How to get to it (user POV)`
3. `Driving it with control-nopcommerce`
4. `Gotchas`

## Features

- [Storefront home](./storefront-home.md) — homepage loads with branding and sample catalog.
- [Product search](./product-search.md) — header search finds sample products.
- [Add to cart](./add-to-cart.md) — add a simple product and see it on `/cart`.
- [Customer login](./customer-login.md) — sign in as the installed admin customer on the public login form.
- [Admin area](./admin-area.md) — reach the administration dashboard after login.
