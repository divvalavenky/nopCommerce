# Admin area

Store administrators reach the `/Admin` dashboard after authenticating with an admin-capable account.

## Sub-features

- `admin-gate` redirects anonymous `/Admin` visitors to login.
- `admin-dashboard` shows the administration shell after a successful login.

## How to get to it (user POV)

- Open `/Admin` while signed out (expect login redirect).
- Choose Administration from the public header after signing in.
- Sign in at `/login?returnUrl=%2Fadmin`.

## Driving it with control-nopcommerce

Preconditions:

- Installed storefront; doctor healthy.
- Admin credentials from install.

- **Anonymous gate.** Run `control-nopcommerce browser --run-id <id> content --url "/Admin" --expect "login"`. The response is the login flow (URL or body), not an empty dashboard.
- **Authenticated dashboard.** Run `control-nopcommerce browser --run-id <id> admin-login`. Exit code `0`; artifacts under `artifacts/<id>/admin-login/` show the admin UI (dashboard/content header or sidebar).
- **Proof.** Keep `admin.png` / `admin.html`. Assert `/admin` appears in the final URL or the HTML contains admin chrome such as a dashboard content header.

## Gotchas

- `/Admin` before install redirects to the install wizard — that is not this feature.
- AdminLTE markup differs from the public theme; do not reuse storefront selectors.
- A 200 on `/Admin` after login is insufficient if the body is still the public login form — check for admin chrome.
