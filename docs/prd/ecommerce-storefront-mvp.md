# PRD: E-Commerce Storefront MVP
**Project:** nopCommerce (open source) · **Author:** Venky Divvala · **Status:** Draft · **Date:** Nov 3, 2026

## 1. Summary
Define the minimum storefront a merchant needs to sell online on nopCommerce: browse and search a catalog, add to cart, check out (ship or pick up), pay, and manage the order—plus the admin tools to run that loop day one.

## 2. Problem
- Merchants need a clear “open for business” bar; shipping a full platform dump without priorities delays launch.
- Shoppers abandon when search, cart, or checkout feel incomplete or untrustworthy.
- Ops staff need a single place to fulfill, refund, and answer “where is my order?” without custom tooling.

## 3. Goals and non-goals
**Goals**
- Shoppers can discover products, purchase as guest or registered customer, and receive order confirmation.
- Merchants can configure catalog, pricing, shipping/pickup, and at least one payment method per store.
- Admins can process orders (status, shipments / ready-for-pickup, cancellations, basic refunds).
- Storefront and transactional email copy are localizable; times shown in the store time zone.

**Non-goals (v1)**
- Marketplace / multi-vendor commission workflows.
- Native mobile apps (Web API / mobile plugins may exist; out of MVP scope).
- Advanced personalization, A/B testing, or on-site merchandising AI.
- Full RMA/self-service returns portal (admin-initiated refunds only in v1).
- Scheduled delivery windows or pickup time slots (separate PRDs).

## 4. Users
- **Shopper:** finds products quickly, checks out with minimal friction, tracks the order.
- **Store admin / owner:** configures catalog, tax, shipping, payments; monitors orders and revenue.
- **Fulfillment staff:** marks shipments or pickup-ready; handles cancellations.

## 5. User stories
1. As a shopper, I can search and filter the catalog and open a product detail page with price, stock, and images.
2. As a shopper, I can add simple and attribute-based products to the cart and update quantities.
3. As a shopper, I can check out as a guest or while logged in, enter addresses, and choose shipping or pick up in store.
4. As a shopper, I can pay with an enabled payment method and land on a confirmation page with an order number.
5. As a shopper, I receive an order-placed email and can view the order under My Account (registered) or via order details link policy the store uses.
6. As an admin, I can create categories and products (including SKU, price, inventory) and publish them to the storefront.
7. As an admin, I can enable shipping methods and/or pickup points and at least one payment provider.
8. As staff, I can filter orders, change status, add a shipment or mark ready for pickup, and issue a refund for a paid order.

## 6. Functional requirements
| ID | Requirement | Priority |
|---|---|---|
| FR1 | Catalog: categories, product details, stock visibility, and product images on the public store | Must |
| FR2 | Search returns relevant published products; empty state is clear | Must |
| FR3 | Shopping cart persists for the session (and for registered customers per platform rules); line price and totals visible | Must |
| FR4 | Checkout supports guest and registered flows: billing/shipping address, shipping method or pickup point, payment, confirm | Must |
| FR5 | At least one online payment method and one offline/manual method can be enabled per store | Must |
| FR6 | Order is created atomically with payment result; confirmation page and `%Order.…%` email tokens for order-placed | Must |
| FR7 | Admin catalog CRUD for categories/products; publish/unpublish | Must |
| FR8 | Admin order list + detail: status, payment status, shipments / ready-for-pickup | Must |
| FR9 | Multi-store aware settings for currency, tax display, and theme where the platform already supports overrides | Must |
| FR10 | All new or merchant-facing MVP strings use localization resources | Must |
| FR11 | Pick up in store via existing pickup provider when enabled (point selection only; no time slots) | Should |
| FR12 | Discount codes / coupon entry on cart or checkout | Should |
| FR13 | Wishlist for registered customers | Should |
| FR14 | Basic PDF invoice / order summary from admin or customer account | Should |
| FR15 | Abandoned-cart reminder emails via existing reminder pipeline | Could |
| FR16 | Product reviews moderated in admin | Could |
| FR17 | Webhooks or plugin events for order-paid integrations | Could |

## 7. Technical notes
- Prefer **configuration and theme/store settings** over new plugins for MVP; add or extend plugins only when a Must cannot ship in core/admin already.
- Payments and shipping stay behind existing provider interfaces (`IPaymentMethod`, shipping rate computation, `IPickupPointProvider`).
- Orders, cart, and checkout remain in `Nop.Services` / `Nop.Web`; do not fork checkout into a one-off plugin for MVP.
- Persist money and UTC timestamps per platform conventions; display in store currency and time zone.
- Use FluentMigrator only if MVP introduces new tables; otherwise no schema change.
- Caching: rely on existing catalog/cache invalidation; do not add long-lived cart caches that can oversell without inventory checks.
- Feature flags: keep experimental Should/Could items behind settings defaulting off until Alpha Musts are green.

## 8. UX outline
- **Home → category or search → product detail → add to cart → cart → checkout → confirm.**
- Checkout steps stay aligned with current nopCommerce one-page or stepped checkout setting; show order totals, tax, and shipping before pay.
- Out-of-stock: disable add-to-cart or show clear messaging per inventory settings.
- Admin: Catalog and Sales menus cover day-one ops; payment/shipping configuration under Configuration.
- Confirmation: “Order #… · Thank you” with email expectation in one short line.

## 9. Success metrics
- Merchant can place a test order end-to-end within one hour of sample-data install (happy path).
- ≥ 95% of test checkouts with valid payment sandbox credentials reach `OrderStatus` paid/processing without manual DB fixes.
- Zero checkout crashes on empty cart, invalid address, or payment decline (handled errors only).
- Support tickets for “can’t find order” and “can’t pay” trend down after GA vs soft-launch baseline.

## 10. Risks and open questions
- Which payment provider is mandatory in CI/demo (PayPal, Stripe, Manual)? (Proposed: Manual + one sandbox online method.)
- Guest checkout on vs off by default per store? (Proposed: admin setting; sample store on.)
- Tax inclusive vs exclusive display defaults by region? (Proposed: leave to store tax settings; document in launch checklist.)
- Theme: DefaultClean only for MVP, or allow installed themes? (Proposed: DefaultClean for proof; themes allowed if they don’t break checkout selectors.)
- Inventory oversell under concurrency: accept platform inventory rules for v1; no new reservation system in this PRD.

## 11. Release plan
1. **Alpha:** FR1–FR6 on a clean install with sample catalog; Manual payment + one shipping method; verify-nopcommerce happy path (search → cart → checkout → confirm).
2. **Beta:** FR7–FR10 hardened; admin order fulfillment; localization pass; guest + registered checkout; optional FR11 pickup point.
3. **GA:** FR12–FR14 as time allows; docs for merchant launch checklist; FR15–FR17 only with explicit ask.

## Appendix — Skill handoff
When implementing with `implement-nopcommerce-prd`:

- Treat **Must** as Alpha/Beta only; skip Should/Could unless requested.
- Non-goals block pickup slots, RMA portal, and marketplace work from sneaking into this MVP.
- Prove with `verify-nopcommerce` (install → search → add to cart → admin login / order visibility).
