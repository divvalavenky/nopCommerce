# PRD: E-Commerce Storefront MVP
**Project:** nopCommerce (open source) · **Author:** Venky Divvala · **Status:** Draft · **Date:** Nov 3, 2026

## 1. Summary
Ship the minimum e-commerce loop on nopCommerce: discover products, buy (guest or registered), pay, get confirmation, and fulfill—covering the core retail requirements every online store must satisfy on day one.

## 2. Problem
- Merchants lack a prioritized “open for business” requirements bar; treating the whole platform as MVP delays launch.
- Shoppers abandon when discovery, cart truthfulness, checkout, or payment trust break.
- Ops cannot answer “where is my order?” or ship/refund without a clear order-management path.

## 3. Goals and non-goals
**Goals (e-commerce MVP)**
- **Discover:** browse categories, search, open product detail with price, availability, and media.
- **Transact:** cart → checkout (address, ship or pickup point) → pay → order confirmation.
- **Account:** guest checkout and registered customers; order history for registered shoppers.
- **Operate:** admin catalog publish, order processing, shipment / ready-for-pickup, basic refunds.
- **Trust:** localizable UI and emails; money and times shown in store currency and time zone.

**Non-goals (v1)**
- Marketplace / multi-vendor commissions.
- Native mobile apps (existing Web API / mobile plugins are out of MVP scope).
- Personalization, A/B testing, or merchandising AI.
- Self-service RMA / returns portal (admin-initiated refunds only).
- Scheduled delivery windows or pickup **time slots** (separate PRD; point pickup only if enabled).

## 4. Users
- **Shopper:** finds products, checks out with low friction, trusts payment and order status.
- **Store admin / owner:** configures catalog, tax, shipping, payments; monitors orders.
- **Fulfillment staff:** ships or marks ready for pickup; handles cancel/refund basics.

## 5. User stories (by e-commerce domain)

### Catalog & discovery
1. As a shopper, I can browse categories and open a product page with price, stock state, and images.
2. As a shopper, I can search published products and understand empty results.

### Cart
3. As a shopper, I can add simple and attribute-based products, change quantities, and see line and cart totals.

### Checkout & fulfillment choice
4. As a shopper, I can check out as guest or logged-in, enter billing/shipping addresses, and choose a shipping method or store pickup point.

### Payments & confirmation
5. As a shopper, I can pay with an enabled method, land on a confirmation page with an order number, and receive an order-placed email.

### Customer account
6. As a registered shopper, I can view my orders under My Account.

### Admin merchandising
7. As an admin, I can create categories and products (SKU, price, inventory, images) and publish/unpublish them.

### Admin configuration
8. As an admin, I can enable shipping and/or pickup points and at least one payment method per store.

### Order operations
9. As staff, I can find orders, update status, add a shipment or mark ready for pickup, and refund a paid order.

## 6. Functional requirements

Requirements are grouped by e-commerce capability. Priority drives release sequencing (see §11).

### A. Catalog & merchandising
| ID | Requirement | Priority |
|---|---|---|
| FR1 | Public catalog: categories, product detail (price, stock visibility, images) for published products | Must |
| FR2 | Product search over published catalog; clear empty state | Must |
| FR7 | Admin CRUD for categories/products (SKU, price, inventory, media); publish/unpublish | Must |

### B. Cart & session
| ID | Requirement | Priority |
|---|---|---|
| FR3 | Cart persists for the session (and for registered customers per platform rules); line prices and totals visible | Must |
| FR12 | Discount / coupon code entry on cart or checkout | Should |
| FR13 | Wishlist for registered customers | Should |

### C. Checkout, shipping, tax
| ID | Requirement | Priority |
|---|---|---|
| FR4 | Checkout: guest + registered; billing/shipping address; shipping method **or** pickup point; payment; confirm | Must |
| FR9 | Multi-store aware currency, tax display, and theme overrides where the platform already supports them | Must |
| FR11 | Pick up in store via existing pickup provider when enabled (point selection only; no time slots) | Should |

### D. Payments & order creation
| ID | Requirement | Priority |
|---|---|---|
| FR5 | Per store: at least one online payment method and one offline/manual method can be enabled | Must |
| FR6 | Order created consistently with payment result; confirmation page; order-placed email via `%Order.…%` tokens | Must |

### E. Customer account & documents
| ID | Requirement | Priority |
|---|---|---|
| FR10 | All merchant-facing MVP strings use localization resources | Must |
| FR14 | Basic PDF invoice / order summary (admin and/or customer account) | Should |

### F. Order management & fulfillment
| ID | Requirement | Priority |
|---|---|---|
| FR8 | Admin order list + detail: order status, payment status, shipments / ready-for-pickup | Must |

### G. Engagement & integrations (post-MVP stretch)
| ID | Requirement | Priority |
|---|---|---|
| FR15 | Abandoned-cart reminder emails via existing reminder pipeline | Could |
| FR16 | Product reviews with admin moderation | Could |
| FR17 | Webhooks or plugin events for order-paid integrations | Could |

## 7. Technical notes
- Prefer **configuration and existing core/admin** over new plugins; extend plugins only when a Must cannot ship otherwise.
- Keep payments/shipping behind existing interfaces (`IPaymentMethod`, shipping rate computation, `IPickupPointProvider`).
- Cart, checkout, and orders stay in `Nop.Services` / `Nop.Web`—do not fork checkout into a one-off MVP plugin.
- Money and timestamps: platform conventions (store currency; UTC storage, store time zone display).
- FluentMigrator only if new tables are required; MVP should avoid schema churn.
- Inventory: use platform stock checks at add-to-cart and place-order; no new hold/reservation system in this PRD (contrast: pickup-slot PRD).
- Gate experimental Should/Could behind settings defaulting **off** until Alpha Musts pass.

## 8. UX outline
- Happy path: **Home → category or search → product detail → add to cart → cart → checkout → confirmation.**
- Checkout follows the store’s one-page vs stepped setting; show merchandise, shipping, tax, and order total before pay.
- Out of stock: block or warn per inventory settings—never silent oversell in the UI.
- Admin day-one: **Catalog** + **Sales**; payments/shipping under **Configuration**.
- Confirmation copy: order number + short “confirmation email on the way” line.

## 9. Success metrics
- Merchant completes a test purchase end-to-end within one hour of sample-data install.
- ≥ 95% of sandbox-payment test checkouts reach paid/processing without manual DB repair.
- Empty cart, invalid address, and payment decline show handled errors (no unhandled checkout failures).
- “Can’t pay” / “can’t find order” support volume declines after GA vs soft-launch baseline.

## 10. Risks and open questions
| Question | Proposed default |
|---|---|
| Mandatory payment method for CI/demo? | Manual + one sandbox online provider |
| Guest checkout default? | Admin setting; sample store **on** |
| Tax inclusive vs exclusive display? | Store tax settings; document in launch checklist |
| Theme for MVP proof? | DefaultClean for automated proof; other themes OK if checkout still works |
| Concurrent oversell? | Accept platform inventory rules; no new reservation layer here |

## 11. Release plan
1. **Alpha (shopper buy-path Must):** FR1–FR6 — sample catalog, Manual payment, one shipping method; prove with `verify-nopcommerce` (search → cart → checkout → confirm).
2. **Beta (operate the store):** FR7–FR10 — admin catalog + order fulfillment, localization, guest + registered; optional FR11 pickup point.
3. **GA (retail polish):** FR12–FR14 as capacity allows; merchant launch checklist docs; FR15–FR17 only on explicit ask.

## Appendix — Skill handoff (`implement-nopcommerce-prd`)
- **Parse:** sections 1–11 match the skill’s required fields; FR domains A–G are e-commerce requirement groupings, not extra scope.
- **Sequence:** implement **Must** only for Alpha/Beta; defer Should/Could unless requested.
- **Non-goals:** block marketplace, RMA portal, mobile apps, and pickup **time slots** from this MVP.
- **Prove:** `verify-nopcommerce` — install → search → add to cart → checkout path → admin order visibility.
