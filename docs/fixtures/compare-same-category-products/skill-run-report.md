# Skill run: compare-same-category-products

Analysis of the editable fixture catalog using the `compare-same-category-products` skill (document path).

**Source:** [`sample-catalog.csv`](./sample-catalog.csv) / [`sample-catalog.md`](./sample-catalog.md)  
**Scope:** all fixture rows (published=yes) · **30 products** · **7 leaf categories**  
**Issues:** critical=4, high=8, medium=10, low=8

## Headline

Notebooks are the only tight peer set (1.33×). Jewelry (70× rental+purchase) and Cell phones (8.6× legacy+flagship) are not fair compare groups. Four critical buyability/integrity defects are seeded and detected in Accessories and QA edge cases.

## Peer-group scores

| Category | n | Min | Max | Avg | Ratio | Rank |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Jewelry | 3 | $30.00 | $2100.00 | $830.00 | 70.00× | Mixed / weak peers |
| Electronics > Cell phones | 5 | $100.00 | $859.00 | $470.40 | 8.59× | Mixed / weak peers |
| QA > Edge cases | 6 | $-5.00 | $40.00 | $17.33 | 3.33× | Issue sandbox (not shopper peers) |
| Apparel > Clothing | 4 | $15.00 | $43.50 | $24.38 | 2.90× | Usable with caveats |
| Books | 3 | $24.00 | $51.00 | $34.00 | 2.12× | Usable with caveats |
| Apparel > Accessories | 3 | $25.00 | $45.00 | $33.33 | 1.80× | Usable with caveats |
| Computers > Notebooks | 6 | $1350.00 | $1800.00 | $1510.00 | 1.33× | Tight peer set |

## Product issues

| Code | Severity | Scope | Target | Evidence | Suggested fix |
| --- | --- | --- | --- | --- | --- |
| `BUY_DISABLED` | critical | product | Published Buy Disabled | QA > Edge cases; disable_buy_button=yes | Re-enable buy or unpublish |
| `BUY_OOS_PUBLISHED` | critical | product | Reversible Check Belt | Apparel > Accessories; stock=0; published | Restock or unpublish |
| `CATALOG_DUP_SKU` | critical | product | Duplicate SKU Alpha, Duplicate SKU Beta | sku=QA-DUP-01 used 2 times | Dedupe / correct SKU |
| `PRICE_NEGATIVE` | critical | product | Negative Price Glitch | price=-5.0 | Correct price |
| `CATALOG_MIX_ERA` | high | group | Electronics > Cell phones | legacy=3 current/flagship=2 | Retire or rehome legacy |
| `CATALOG_MIX_RENTAL_PURCHASE` | high | group | Jewelry | rentals=1 purchases=2; ratio=70.0 | Split or tag commerce model |
| `MERCH_HOMEPAGE_WEAK_PEER` | high | product | Helix smartphone | Electronics > Cell phones; homepage on legacy; stronger peers exist | Point slot at stronger SKU |
| `PRICE_COST_ABOVE_PRICE` | high | product | Cost Above Price Widget | cost=35.0 price=20.0 | Review margin or price |
| `PRICE_OLD_NOT_HIGHER` | high | product | Broken Promo Mug | old_price=10.0 price=12.0 | Fix markdown or clear old price |
| `PRICE_OLD_NOT_HIGHER` | high | product | Pride and Prejudice | old_price=20.0 price=24.0 | Fix markdown or clear old price |
| `PRICE_RATIO_EXTREME` | high | group | Electronics > Cell phones | ratio=8.59 ($100.00-$859.00) | Split category or rehome outliers |
| `PRICE_RATIO_EXTREME` | high | group | Jewelry | ratio=70.00 ($30.00-$2100.00) | Split category or rehome outliers |
| `CONTENT_NO_SHORT_DESC` | medium | product | Oversized Studio Tee | Apparel > Clothing; empty short_description | Add short description |
| `CONTENT_NO_SHORT_DESC` | medium | product | Propaganda Cap | Apparel > Accessories; empty short_description | Add short description |
| `CONTENT_NO_SHORT_DESC` | medium | product | Windrun Loose Shirt | Apparel > Clothing; empty short_description | Add short description |
| `CONTENT_NO_SKU` | medium | product | Oversized Studio Tee | Apparel > Clothing; empty sku | Assign SKU |
| `SOCIAL_PREMIUM_NO_REVIEWS` | medium | product | ApexBook Pro 14 | Computers > Notebooks; premium quartile; reviews=0 | Seed or solicit reviews |
| `SOCIAL_PREMIUM_NO_REVIEWS` | medium | product | Cost Above Price Widget | QA > Edge cases; premium quartile; reviews=0 | Seed or solicit reviews |
| `SOCIAL_PREMIUM_NO_REVIEWS` | medium | product | Duplicate SKU Beta | QA > Edge cases; premium quartile; reviews=0 | Seed or solicit reviews |
| `SOCIAL_PREMIUM_NO_REVIEWS` | medium | product | Published Buy Disabled | QA > Edge cases; premium quartile; reviews=0 | Seed or solicit reviews |
| `SOCIAL_PREMIUM_NO_REVIEWS` | medium | product | Summit Ultra 15 | Computers > Notebooks; premium quartile; reviews=0 | Seed or solicit reviews |
| `SOCIAL_PREMIUM_NO_REVIEWS` | medium | product | Vintage Style Engagement Ring | Jewelry; premium quartile; reviews=0 | Seed or solicit reviews |
| `MERCH_FREE_SHIP_ONLY_ANCHOR` | low | group | Computers > Notebooks | only ApexBook Pro 14 (max price) has free shipping | Decide intentional vs extend perk |
| `SOCIAL_NO_REVIEWS` | low | product | Broken Promo Mug | QA > Edge cases; reviews=0 | Optional review push |
| `SOCIAL_NO_REVIEWS` | low | product | Custom Print Tee | Apparel > Clothing; reviews=0 | Optional review push |
| `SOCIAL_NO_REVIEWS` | low | product | Duplicate SKU Alpha | QA > Edge cases; reviews=0 | Optional review push |
| `SOCIAL_NO_REVIEWS` | low | product | Helix smartphone | Electronics > Cell phones; reviews=0 | Optional review push |
| `SOCIAL_NO_REVIEWS` | low | product | Negative Price Glitch | QA > Edge cases; reviews=0 | Optional review push |
| `SOCIAL_NO_REVIEWS` | low | product | Pine Spectre 13 | Computers > Notebooks; reviews=0 | Optional review push |
| `SOCIAL_NO_REVIEWS` | low | product | Pride and Prejudice | Books; reviews=0 | Optional review push |

## Deep dives

### Computers › Notebooks — tight peer set

Six laptops in a $1,350–$1,800 band (1.33×). ApexBook Pro 14 is the only homepage + free-ship SKU; Trail Carbon 14 is the value pick among 5★. Premium unreviewed: ApexBook + Summit Ultra.

| Product | Price | Rating | Reviews | Homepage | Free ship | Insight |
| --- | ---: | --- | ---: | --- | --- | --- |
| ApexBook Pro 14 | $1800 | — | 0 | yes | yes | premium anchor; SOCIAL_PREMIUM_NO_REVIEWS |
| Summit Ultra 15 | $1590 | — | 0 | no | no | SOCIAL_PREMIUM_NO_REVIEWS |
| Cascade 15 Laptop | $1500 | 5.0 | 1 | no | no | — |
| Harbor Sleek 15 | $1460 | 4.0 | 1 | no | no | — |
| Trail Carbon 14 | $1360 | 5.0 | 1 | no | no | best value among 5★ |
| Pine Spectre 13 | $1350 | — | 0 | no | no | — |

### Electronics › Cell phones — mixed eras

8.59× spread from current flagships vs legacy demo SKUs. Homepage promotes Helix smartphone (legacy) instead of Nova/Aura — `MERCH_HOMEPAGE_WEAK_PEER` + `CATALOG_MIX_ERA`.

| Product | Price | Notes cohort | Homepage |
| --- | ---: | --- | --- |
| Nova Phone X 256GB | $859 | Current | no |
| Aura Phone 16 128GB | $799 | Current | no |
| Lumina 1020 | $349 | Legacy | no |
| Helix smartphone | $245 | Legacy | yes |
| Helix Mini Blue | $100 | Legacy | no |

### Jewelry — incompatible commerce models

70× ratio from rental necklace ($30) vs engagement ring ($2,100). Emit `CATALOG_MIX_RENTAL_PURCHASE` before any “best value” claim.

| Product | Price | Model |
| --- | ---: | --- |
| Vintage Style Engagement Ring | $2100 | Purchase |
| Flower Girl Bracelet | $360 | Purchase |
| Elegant Gemstone Necklace (rental) | $30 | Rental |

### QA › Edge cases — issue concentration

Sandbox leaf holding critical pricing/buyability defects (negative price, buy disabled, dup SKU, cost above price). Not a shopper peer set.

## Cross-category findings

- **Merchandising:** Homepage slots on ApexBook Pro 14 (sensible premium) and Helix smartphone (weak/legacy). Free shipping concentrated on premium/promo anchors (Notebooks, Jewelry bracelet, Books).
- **Promos:** Books mostly valid markdowns; Pride and Prejudice + Broken Promo Mug have `old_price <= price`.
- **Content:** Missing SKU on Oversized Studio Tee; missing short descriptions on several Apparel SKUs.
- **Cost:** Only QA-04 has `product_cost`; margin analysis blocked for the rest (not `DATA_COST_ALL_BLANK` because one cost exists).
- **Tightest sets after Notebooks:** Apparel Accessories (1.8×) and Books (2.13×) if promos are treated carefully.

## Recommended actions

| Priority | Action | Why |
| --- | --- | --- |
| High | Fix negative price / re-enable buy / dedupe `QA-DUP-01` / restock or unpublish Reversible Check Belt | Clears all `critical` codes |
| High | Split rental jewelry; retire or rehome legacy phones; move homepage off Helix | Clears `CATALOG_MIX_*`, `PRICE_RATIO_EXTREME`, `MERCH_HOMEPAGE_WEAK_PEER` |
| High | Correct broken markdowns (Pride and Prejudice, Broken Promo Mug) and cost>price widget | Clears `PRICE_OLD_NOT_HIGHER`, `PRICE_COST_ABOVE_PRICE` |
| Medium | Seed reviews on ApexBook / Summit Ultra / engagement ring; add SKU + short descriptions | Clears social/content mediums |
| Low | Decide free-ship policy on Notebooks; optional reviews on non-premium blanks | Clears low hygiene codes |

## Method

Document path of `compare-same-category-products`: leaf groups from `category_path`, field model from skill `fields.md`, issue codes from `issues.md`. Blank cells left blank (not imputed).

