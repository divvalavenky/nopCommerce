# Add to cart

A shopper opens a simple product, adds it to the shopping cart, and sees the line item on `/cart`.

## Sub-features

- `cart-open-product` opens product details from the catalog.
- `cart-add` clicks Add to cart on the details page.
- `cart-view` shows the product name on `/cart`.

## How to get to it (user POV)

- From the homepage or search results, open a product such as `First Prize Pies`.
- Choose Add to cart on the product details page.
- Open Shopping cart from the header (`a.ico-cart`) or go to `/cart`.

## Driving it with control-nopcommerce

Preconditions:

- Installed sample-data storefront; doctor healthy.
- Use a simple product without required attributes: `First Prize Pies` (book). Do not use `Build your own computer` or gift cards for this baseline recipe.

- **Add and view.** Run `control-nopcommerce browser --run-id <id> add-to-cart-simple --name "First Prize Pies"`. Exit code `0`; `artifacts/<id>/cart/cart.png` and `cart.html` contain `First Prize Pies`.
- **Proof.** The cart page HTML is the second view; a bar notification alone is not enough.

## Gotchas

- Configurable products require attribute selection; the harness click will not create a valid cart line.
- Quantity defaults to 1; do not assert a specific unit price in this recipe (tax/currency settings vary by country chosen at install).
- Guest carts are cookie-scoped to the Playwright context for that command only.
