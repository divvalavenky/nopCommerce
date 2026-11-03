# PRD: Market-Based Pricing Plugin
**Project:** nopCommerce (open source) · **Author:** Venky Divvala · **Status:** Draft · **Date:** Nov 3, 2026

## 1. Summary
Let merchants define geographic **markets** (sets of countries), set per-product prices per market, and have the storefront/cart use that price when the shopper’s address country maps to a market—without replacing core price lists, tier prices, or discounts.

## 2. Problem
- Catalog price is global; merchants selling into multiple countries need country/market list prices.
- Existing **price lists** target customers/roles, not geography.
- Third-party geo-pricing plugins are inconsistent with core calculation order.

## 3. Goals and non-goals
**Goals**
- Admins define markets, assign countries, and set product prices per market.
- Resolved market price becomes the base list price before price lists / tiers / discounts.
- Feature is gated by a plugin setting (off until configured).

**Non-goals (v1)**
- Competitor scrape / demand-based dynamic pricing.
- Automatic FX conversion of market prices (prices are in primary store currency).
- State/province or zip-level markets.
- Replacing or forking core price lists.

## 4. Users
- **Shopper:** sees the price for their market when an address country is known.
- **Store admin:** configures markets and product market prices.
- **Developer:** installs `Misc.MarketBasedPricing` without core API breaks.

## 5. User stories
1. As an admin, I can enable market-based pricing and choose shipping-vs-billing country preference.
2. As an admin, I can create a market, assign one or more countries, and set it active.
3. As an admin, I can set a product’s price for a market.
4. As a shopper with a matching country, I see and check out at the market price (then discounts/tiers as usual).
5. As a shopper with no matching market/price, I see the normal catalog price.

## 6. Functional requirements
| ID | Requirement | Priority |
|---|---|---|
| FR1 | Plugin setting: Enabled (default false), PreferShippingAddress (default true) | Must |
| FR2 | Market entity: name, active, display order, optional store scope (0 = all) | Must |
| FR3 | Map countries to a market (one active market win per country; lowest display order) | Must |
| FR4 | Market product price: product + market + price | Must |
| FR5 | Resolve country: shipping address if preferred & present, else billing; no match → catalog price | Must |
| FR6 | Apply market price via price calculation as overridden base price when Enabled | Must |
| FR7 | Admin UI: configure settings, manage markets/countries, manage market product prices | Must |
| FR8 | Localization resources for all plugin strings | Must |
| FR9 | Short-TTL / cache-safe pricing (reuse core product price cache keys via override) | Must |
| FR10 | Admin filter “orders by market” | Should |
| FR11 | CSV import of market prices | Should |
| FR12 | `MarketPriceAppliedEvent` for integrations | Could |

## 7. Technical notes
- New Misc plugin: `Nop.Plugin.Misc.MarketBasedPricing` (`Misc.MarketBasedPricing`).
- Entities + FluentMigrator under the plugin; no core schema change.
- Decorate `IPriceCalculationService` in plugin `INopStartup` (wrap concrete `PriceCalculationService`); pass market price as `overriddenProductPrice` so price lists → tiers → discounts stay intact.
- Do not break `IPriceCalculationService` consumers; decoration is additive.
- Times N/A; money stored as `decimal` in primary store currency.
- Multi-store: optional `StoreId` on market (0 = all stores).

## 8. UX outline
- Plugin Configure: enable toggle + address preference → Markets grid → Market edit (countries + product prices).
- Storefront: no extra badge in v1; price simply reflects market when applicable.
- Missing market price: silent fallback to catalog price.

## 9. Success metrics
- Admin can configure one market + one product price and see it on PDP/cart for a customer with that country.
- Unmapped countries unchanged vs baseline catalog price.
- No regression in price-list / tier / discount unit tests when plugin disabled.

## 10. Risks and open questions
| Question | Proposed default |
|---|---|
| Country source when guest has no address? | No market; catalog price |
| Country in two markets? | First active market by DisplayOrder, then Id |
| Market vs price list precedence? | Market sets base override; price lists still apply after |
| Plugin enabled by default? | **false** (Alpha gate) |

## 11. Release plan
1. **Alpha:** FR1–FR9 — settings, entities, admin CRUD, price decoration behind Enabled.
2. **Beta:** harden multi-store; docs; sample market.
3. **GA:** FR10–FR11 as capacity allows; FR12 on ask.

## Appendix — Skill handoff
- Implement **Must** only for Alpha; skip Should/Could.
- Prove: unit tests for country→market resolution + price apply when enabled/disabled; build plugin; optional `verify-nopcommerce` after install/enable.
