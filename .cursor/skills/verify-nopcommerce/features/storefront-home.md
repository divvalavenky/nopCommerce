# Storefront home

The public homepage identifies the store, exposes search and cart chrome, and lists featured sample products after a sample-data install.

## Sub-features

- `home-load` serves the homepage without redirecting to `/install`.
- `home-chrome` shows logo, search box, and cart link.
- `home-catalog` shows at least one sample product title such as `Apple MacBook Pro`.

## How to get to it (user POV)

- Open the store root URL in a browser.
- After install completes, follow the automatic redirect to the homepage.

## Driving it with control-nopcommerce

Preconditions:

- `control-nopcommerce doctor --run-id <id>` reports `installed: true`.
- Sample data was installed (default `install` command).

- **Open home.** Run `control-nopcommerce browser --run-id <id> content --url "/" --expect "header-logo"`. Exit code `0` and JSON `ok` is true.
- **Confirm catalog.** Run `control-nopcommerce browser --run-id <id> content --url "/" --expect "Apple MacBook Pro"`. The homepage HTML contains that product name.
- **Proof.** Run `control-nopcommerce browser --run-id <id> screenshot --url "/" --path .cursor/skills/verify-nopcommerce/artifacts/<id>/storefront-home/home.png`. The screenshot shows the header logo and at least one product card.

## Gotchas

- Hitting port `5000` (environment start) while that instance is still on the install wizard is not this feature.
- Without sample data, assert chrome only; do not invent product names.
- First request after install can race the app recycle — retry `doctor` and `content` for up to two minutes before failing.
