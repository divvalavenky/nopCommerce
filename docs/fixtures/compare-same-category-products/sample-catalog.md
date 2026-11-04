# Sample categorized product catalog (skill fixture)

Editable fixture for testing the `compare-same-category-products` skill against **document** data.

- Edit rows/columns in this file (or the companion CSV) in a PR; keep Markdown tables aligned.
- Currency: USD unless a row says otherwise.
- `published` = yes means the SKU should be treated as live on the storefront.
- Blank cells mean **missing data** — do not invent values when running the skill.
- Section [Seeded issues (expected)](#seeded-issues-expected) lists intentional defects for issue-detection checks. Update that list when you add/remove seeded problems.

Companion spreadsheet-friendly file: [sample-catalog.csv](./sample-catalog.csv)

---

## Computers > Notebooks

Tight peer set (healthy comparison baseline).

| product_id | product_name | sku | price | old_price | product_cost | stock_quantity | reviews | avg_rating | short_description | is_free_shipping | show_on_homepage | is_featured | is_download | is_rental | call_for_price | disable_buy_button | published | notes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| NB-01 | ApexBook Pro 14 | APX-NB-14 | 1800 | | | 10000 | 0 | | Thin premium laptop with all-day battery | yes | yes | no | no | no | no | no | yes | Premium anchor; homepage + free ship |
| NB-02 | Summit Ultra 15 | SMT-NB-15 | 1590 | | | 10000 | 0 | | Lightweight ultrabook for travel | no | no | no | no | no | no | no | yes | |
| NB-03 | Cascade 15 Laptop | CSC-NB-15 | 1500 | | | 10000 | 1 | 5.0 | Everyday 15-inch laptop | no | no | no | no | no | no | no | yes | |
| NB-04 | Harbor Sleek 15 | HRB-NB-15 | 1460 | | | 10000 | 1 | 4.0 | Slim aluminum notebook | no | no | no | no | no | no | no | yes | |
| NB-05 | Trail Carbon 14 | TRL-NB-14 | 1360 | | | 10000 | 1 | 5.0 | Carbon-fiber business laptop | no | no | no | no | no | no | no | yes | Value pick among rated |
| NB-06 | Pine Spectre 13 | PIN-NB-13 | 1350 | | | 10000 | 0 | | Compact 13-inch notebook | no | no | no | no | no | no | no | yes | Cheapest; unreviewed |

---

## Electronics > Cell phones

Mixed eras (weak peers — flagship + legacy demo).

| product_id | product_name | sku | price | old_price | product_cost | stock_quantity | reviews | avg_rating | short_description | is_free_shipping | show_on_homepage | is_featured | is_download | is_rental | call_for_price | disable_buy_button | published | notes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PH-01 | Nova Phone X 256GB | NOV-X-256 | 859 | | | 10000 | 1 | 4.0 | Current flagship, 256GB | no | no | no | no | no | no | no | yes | Current cohort |
| PH-02 | Aura Phone 16 128GB | AUR-16-128 | 799 | | | 10000 | 1 | 4.0 | Current flagship, 128GB | no | no | no | no | no | no | no | yes | Current cohort |
| PH-03 | Lumina 1020 | LUM-1020 | 349 | | | 10000 | 1 | 5.0 | Legacy camera phone (demo) | no | no | no | no | no | no | no | yes | Legacy demo |
| PH-04 | Helix smartphone | HLX-M8 | 245 | | | 10000 | 0 | | Older midrange smartphone | no | yes | no | no | no | no | no | yes | Legacy; wrongly on homepage |
| PH-05 | Helix Mini Blue | HLX-MINI-BL | 100 | | | 10000 | 1 | 5.0 | Compact legacy phone | no | no | no | no | no | no | no | yes | Legacy demo |

---

## Apparel > Clothing

| product_id | product_name | sku | price | old_price | product_cost | stock_quantity | reviews | avg_rating | short_description | is_free_shipping | show_on_homepage | is_featured | is_download | is_rental | call_for_price | disable_buy_button | published | notes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| CL-01 | River Straight Jeans | RVR-JN-511 | 43.50 | 55 | | 10000 | 1 | 5.0 | Classic straight-fit denim | no | no | no | no | no | no | no | yes | Valid markdown |
| CL-02 | Oversized Studio Tee | | 24 | | | 10000 | 1 | 4.0 | | no | no | no | no | no | no | no | yes | Missing SKU + short desc |
| CL-03 | Custom Print Tee | CST-TEE-01 | 15 | | | 10000 | 0 | | Build-your-own graphic tee | no | no | no | no | no | no | no | yes | |
| CL-04 | Windrun Loose Shirt | WND-SH-01 | 15 | | | 10000 | 1 | 5.0 | | no | no | no | no | no | no | no | yes | Missing short desc |

---

## Apparel > Accessories

| product_id | product_name | sku | price | old_price | product_cost | stock_quantity | reviews | avg_rating | short_description | is_free_shipping | show_on_homepage | is_featured | is_download | is_rental | call_for_price | disable_buy_button | published | notes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| AC-01 | Reversible Check Belt | RH-CHK-BL | 45 | | | 0 | 1 | 4.0 | Reversible leather belt | no | no | no | no | no | no | no | yes | OOS while published |
| AC-02 | Propaganda Cap | OB-HAT-PR | 30 | | | 10000 | 1 | 5.0 | | no | no | no | no | no | no | no | yes | Missing short desc |
| AC-03 | Aviator Sunglasses | RB-AV-01 | 25 | | | 10000 | 1 | 4.0 | Classic metal aviators | no | no | no | no | no | no | no | yes | |

---

## Jewelry

Incompatible commerce models (purchase + rental).

| product_id | product_name | sku | price | old_price | product_cost | stock_quantity | reviews | avg_rating | short_description | is_free_shipping | show_on_homepage | is_featured | is_download | is_rental | call_for_price | disable_buy_button | published | notes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| JW-01 | Vintage Style Engagement Ring | VS-ENG-01 | 2100 | | | 10000 | 0 | | Classic solitaire engagement ring | no | no | no | no | no | no | no | yes | Purchase; unreviewed premium |
| JW-02 | Flower Girl Bracelet | FG-BR-01 | 360 | | | 10000 | 1 | 5.0 | Delicate event bracelet | yes | no | no | no | no | no | no | yes | Purchase |
| JW-03 | Elegant Gemstone Necklace (rental) | EG-NCK-R | 30 | | | 10000 | 1 | 4.0 | Event rental necklace | no | no | no | no | yes | no | no | yes | Rental — do not peer with purchase |

---

## Books

Promo-heavy leaf (all have old_price).

| product_id | product_name | sku | price | old_price | product_cost | stock_quantity | reviews | avg_rating | short_description | is_free_shipping | show_on_homepage | is_featured | is_download | is_rental | call_for_price | disable_buy_button | published | notes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| BK-01 | First Prize Pies | BK-FPP-01 | 51 | 67 | | 10000 | 1 | 5.0 | Baking cookbook | no | no | no | no | no | no | no | yes | Valid markdown |
| BK-02 | Fahrenheit 451 | BK-F451 | 27 | 30 | | 10000 | 1 | 4.0 | Classic dystopian novel | yes | no | no | no | no | no | no | yes | Valid markdown |
| BK-03 | Pride and Prejudice | BK-PP-01 | 24 | 20 | | 10000 | 0 | | Classic romance novel | no | no | no | no | no | no | no | yes | Broken markdown: old_price < price |

---

## Pricing & buyability edge cases

Cross-category rows used only for issue detection (group as `QA > Edge cases`).

| product_id | product_name | sku | price | old_price | product_cost | stock_quantity | reviews | avg_rating | short_description | is_free_shipping | show_on_homepage | is_featured | is_download | is_rental | call_for_price | disable_buy_button | published | notes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| QA-01 | Broken Promo Mug | QA-MUG-01 | 12 | 10 | | 500 | 0 | | Ceramic mug | no | no | no | no | no | no | no | yes | old_price < price |
| QA-02 | Negative Price Glitch | QA-NEG-01 | -5 | | | 10 | 0 | | Should never ship | no | no | no | no | no | no | no | yes | Negative price |
| QA-03 | Published Buy Disabled | QA-DIS-01 | 40 | | | 200 | 0 | | Accessory with buy locked | no | no | no | no | no | no | yes | yes | Buy disabled |
| QA-04 | Cost Above Price Widget | QA-COST-01 | 20 | | 35 | 100 | 0 | | Margin inverted | no | no | no | no | no | no | no | yes | product_cost > price |
| QA-05 | Duplicate SKU Alpha | QA-DUP-01 | 18 | | | 50 | 0 | | First of dup SKU pair | no | no | no | no | no | no | no | yes | Shares SKU with QA-06 |
| QA-06 | Duplicate SKU Beta | QA-DUP-01 | 19 | | | 50 | 0 | | Second of dup SKU pair | no | no | no | no | no | no | no | yes | Shares SKU with QA-05 |

---

## Seeded issues (expected)

Update this checklist when editing the tables above. Skill runs should surface these (codes from the skill’s `issues.md`).

| Expected code | Severity | Target | Evidence in this fixture |
| --- | --- | --- | --- |
| `BUY_OOS_PUBLISHED` | critical | AC-01 Reversible Check Belt | stock_quantity = 0, published = yes |
| `BUY_DISABLED` | critical | QA-03 Published Buy Disabled | disable_buy_button = yes |
| `PRICE_NEGATIVE` | critical | QA-02 Negative Price Glitch | price = -5 |
| `CATALOG_DUP_SKU` | critical | QA-05 / QA-06 | both use sku `QA-DUP-01` |
| `PRICE_OLD_NOT_HIGHER` | high | BK-03, QA-01 | old_price ≤ price |
| `PRICE_COST_ABOVE_PRICE` | high | QA-04 | product_cost 35 > price 20 |
| `PRICE_RATIO_EXTREME` | high | Jewelry; Cell phones | ratios ≫ 5× |
| `CATALOG_MIX_RENTAL_PURCHASE` | high | Jewelry | JW-03 rental with purchase peers |
| `CATALOG_MIX_ERA` | high | Cell phones | flagship + legacy demo notes |
| `MERCH_HOMEPAGE_WEAK_PEER` | high | PH-04 Helix smartphone | homepage on legacy while PH-01/02 exist |
| `CONTENT_NO_SKU` | medium | CL-02 | empty sku |
| `CONTENT_NO_SHORT_DESC` | medium | CL-02, CL-04, AC-02 | empty short_description |
| `SOCIAL_PREMIUM_NO_REVIEWS` | medium | NB-01, NB-02, JW-01 | top-quartile / premium, reviews = 0 |
| `DATA_COST_ALL_BLANK` | medium | catalog-wide (except QA-04) | almost all product_cost blank — note QA-04 has cost; treat as mostly-blank / flag remaining blanks if skill scopes catalog-level only when *all* blank |

### How to edit safely in a PR

1. Prefer editing one category section per commit when possible.
2. Keep column headers identical across tables (same field model).
3. After changing seeded defects, update [Seeded issues (expected)](#seeded-issues-expected).
4. Mirror material row changes in [sample-catalog.csv](./sample-catalog.csv) so CSV and Markdown stay in sync.
5. Do not replace intentional blanks with placeholder text like `N/A` unless you also remove the related expected issue.
