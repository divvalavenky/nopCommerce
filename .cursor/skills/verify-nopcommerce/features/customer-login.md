# Customer login

A registered customer signs in on the public `/login` form using email and password.

## Sub-features

- `login-form` shows Email and Password fields.
- `login-success` accepts the admin customer created at install and leaves `/login`.

## How to get to it (user POV)

- Choose Log in from the header links.
- Open `/login` directly.
- Admin login recipes also use this form with `returnUrl=/admin`.

## Driving it with control-nopcommerce

Preconditions:

- Installed storefront; doctor healthy.
- Credentials from install defaults unless overridden: `admin@yourStore.com` / `Verify_Pass_123`.

- **Open form.** Run `control-nopcommerce browser --run-id <id> content --url "/login" --expect "login-button"`. Exit code `0`.
- **Sign in via admin recipe.** Run `control-nopcommerce browser --run-id <id> admin-login` (posts the public login form). Exit code `0` and the resulting URL or HTML shows an authenticated admin destination.
- **Proof.** Screenshot under `artifacts/<id>/admin-login/admin.png` plus HTML; also acceptable proof is leaving `/login` without a validation error and seeing account-specific header links (Log out).

## Gotchas

- The install admin is both a storefront customer and an administrator. Do not create a second user for this baseline.
- Wrong password stays on `/login` with a validation message — capture HTML before retrying.
- Multifactor plugins are disabled in default verification; if MFA is enabled in the app under test, stop and record the unmet precondition.
