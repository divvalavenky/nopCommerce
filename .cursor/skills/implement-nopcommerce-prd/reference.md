# nopCommerce PRD implementation — reference

## Platform conventions

| Concern | Convention in this repo |
|---|---|
| Pickup providers | `IPickupPointProvider` in `src/Libraries/Nop.Services/Shipping/Pickup/`; stock provider plugin `src/Plugins/Nop.Plugin.Pickup.PickupInStore/` |
| Plugin DI | Plugin `Infrastructure/NopStartup.cs` |
| Schema | FluentMigrator + `[NopMigration("…", "…", MigrationProcessType.…)]`; prefer `CreateTableIfNotExists` / upgrade migrations over editing old install migrations |
| Mapping | Entity builders under plugin `Data/Mapping/` (see `StorePickupPointBuilder`) |
| Settings | Typed settings + admin configure views; per-store overrides where existing shipping/order settings do |
| Localization | Locale resources for admin and public strings; no hard-coded UI copy |
| Time | Store UTC in DB; convert with store time zone for display (watch DST) |
| Caching | Existing `IStaticCacheManager` (or current cache abstraction); short TTL for availability; invalidate on mutating events |
| Orders | Persist feature data on the order (or linked entity); surface in public order details, admin order details, PDF invoices |
| Email / PDF | `IMessageTokenProvider` / `%Order.…%` tokens |
| Events | Publish domain/plugin events for integrations; keep payload stable |
| Tests | `src/Tests/Nop.Tests` patterns; fake/test pickup providers exist (`PickupPointTestProvider`) |

## Case study: Scheduled Pickup Time Slots (BOPIS)

Source PRD shape (sample): shoppers reserve a pickup date/time per pickup point; admins configure hours, slot length, capacity, lead time, blackouts.

### Goals vs non-goals (v1)

**In:** slot pick at checkout; per-point schedule/capacity/lead time; slot on order, emails, admin views.

**Out:** curbside check-in; delivery windows; staff workforce scheduling.

### Suggested entity split

| Entity | Role |
|---|---|
| `PickupSlotSchedule` | Weekly hours, slot length, capacity, lead time, max days ahead (per pickup point) |
| `PickupSlotBlackout` | Holiday / closed dates per point |
| `PickupSlotReservation` | Hold during checkout + committed reservation linked to order |

### FR priority map (from PRD)

| ID | Priority | Implementation note |
|---|---|---|
| FR1 | Must | Admin settings on pickup point (Alpha) |
| FR2 | Must | Checkout date + slot UI with remaining capacity (Alpha) |
| FR3 | Must | 15-min hold; release if order not placed |
| FR4 | Must | Atomic capacity at place-order |
| FR5 | Must | Persist on order; details, PDF, `%Order.PickupSlot%` (Beta surfacing) |
| FR6 | Should | Admin order list filter |
| FR7 | Should | My Account reschedule until cutoff |
| FR8 | Should | Blackout dates |
| FR9 | Could | API / `PickupSlotReservedEvent` |

### Open questions → defaults

| Question | Proposed default until product decides |
|---|---|
| Slots mandatory or optional? | Admin setting per store |
| Payment pending past hold? | Release hold on TTL; require a free slot again before place-order |
| Third-party pickup plugins | Additive interface members or parallel optional service; no hard break |
| DST | Compute slot boundaries in store TZ, persist UTC instants |

### Release gates

1. **Alpha:** setting off by default; admin config + checkout selection.
2. **Beta:** emails, invoices, admin filters; gather feedback.
3. **GA:** reschedule, blackouts, public docs.

## Anti-patterns

- New top-level plugin when `Pickup.PickupInStore` (or the owning plugin) can host the feature.
- Breaking `IPickupPointProvider` without a compatibility path for third-party providers.
- Enforcing capacity only in UI or only via cache.
- Storing local wall times without TZ/UTC policy.
- Shipping Should/Could before Must hold + atomic commit work.
- Hard-coded English strings in views.
