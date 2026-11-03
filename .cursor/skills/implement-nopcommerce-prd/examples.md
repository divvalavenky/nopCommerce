# Examples: PRD → codebase mapping

## Example 1 — Pickup time slots (Must slice)

**PRD ask:** Shopper selects a slot at checkout; admin configures capacity; no overbooking.

**Placement:**

| Piece | Target |
|---|---|
| Schedule + reservation entities | `src/Plugins/Nop.Plugin.Pickup.PickupInStore/Domain/` |
| Migrations | `…/Data/Migrations/` (new upgrade migration; leave install schema migration intact) |
| Admin CRUD for hours/capacity | Extend `StorePickupPoint` models/views + `PickupInStoreController` |
| Availability service | New service under plugin `Services/` |
| Checkout UI | `Nop.Web` checkout models/views for pickup-in-store step |
| Hold + atomic commit | Service called from checkout place-order path; DB transaction |
| Feature flag | Setting e.g. `PickupSlotSettings.Enabled` (off for Alpha) |

**Out of this slice:** blackouts, reschedule, admin grid filter, public API event.

## Example 2 — Surfacing a reserved value (Beta)

**PRD ask:** Show reserved slot on order details, invoice PDF, and email.

**Placement:**

1. Persist slot id / start-end UTC on order (or `Order` custom values / linked row) at place-order.
2. Public + admin order detail factories/views read and format in store TZ.
3. Add `%Order.PickupSlot%` (or PRD token name) in `MessageTokenProvider` order tokens.
4. Ensure invoice PDF generation uses the same formatted string.

## Example 3 — Deprecation-safe provider extension

**PRD ask:** Compatible with custom pickup providers.

**Approach:**

- Prefer a new optional service (`IPickupSlotService`) consumed by checkout when the active provider supports slots, **or**
- Add optional methods via a new interface (`IPickupSlotAwareProvider`) that providers may implement; checkout checks `is` / plugin capability instead of breaking `IPickupPointProvider.GetPickupPointsAsync` signature.

Do not require every `IPickupPointProvider` to implement slot APIs in v1.

## Example 4 — Parsing any nopCommerce feature PRD

Input excerpt:

```text
Non-goals (v1): … workforce management.
FR3 | Slot reservation held 15 min … | Must
FR7 | Shopper can reschedule … | Should
```

Agent output (planning only):

```text
v1 Must: FR3 hold/release — implement in Alpha with FR1/FR2/FR4.
Defer FR7 to GA.
Non-goal: no staff scheduling tables or UI.
Open question: mandatory slots → admin setting default false.
```
