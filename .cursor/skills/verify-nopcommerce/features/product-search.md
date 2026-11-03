# Product search

Header search lets a shopper find catalog products by keyword and open a matching result.

## Sub-features

- `search-submit` runs a query from the header search box.
- `search-match` shows a known sample product in the results.
- `search-empty` shows a no-results state for a nonsense query.

## How to get to it (user POV)

- Type into the header search box and choose the Search button.
- Land on the product search results page (`/search` route).

## Driving it with control-nopcommerce

Preconditions:

- Installed sample-data storefront; doctor healthy.
- Catalog contains `Apple MacBook Pro`.

- **Match.** Run `control-nopcommerce browser --run-id <id> search --query "MacBook" --expect "Apple MacBook Pro"`. Exit code `0`; artifacts under `artifacts/<id>/search/` include `results.png` and `results.html` showing that title.
- **Miss.** Run `control-nopcommerce browser --run-id <id> search --query "zzznopmatch999" --expect "No products were found"`. If the localized empty string differs, capture HTML and assert the results list has no product title links before failing the recipe — do not weaken the expect to pass.
- **Proof.** Keep the match artifacts; they are the proof for `search-match`.

## Gotchas

- Autocomplete suggestions are not the results page. Proof is the page after submitting `button.search-box-button`.
- Queries are case-insensitive; still use the literal product title in `--expect`.
- Configurable products can appear in search but may not add to cart without attributes — use the add-to-cart feature’s product for cart proofs.
