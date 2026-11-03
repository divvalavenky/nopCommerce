---
name: implement-nopcommerce-prd
description: Turn a nopCommerce feature PRD into a scoped implementation plan and code that follows platform conventions (plugins, FluentMigrator, multi-store, localization, UTC, caching, message tokens, events). Use when implementing a feature from a PRD, planning BOPIS/pickup/checkout work, or mapping Must/Should/Could requirements onto the nopCommerce codebase.
disable-model-invocation: true
---

# Implement nopCommerce from a PRD

Agents read this cold mid-task. Follow **Parse → Place → Sequence → Build → Prove**. Do not invent scope beyond Must (v1) unless the user expands it.

For end-to-end UI proof after code lands, use [verify-nopcommerce](../verify-nopcommerce/SKILL.md).

## Parse the PRD

Extract these fields before writing code. If any are missing, note the gap; do not silently invent product decisions.

| Field | What to capture |
|---|---|
| Summary | One-sentence capability |
| Goals / non-goals | v1 in-scope vs explicitly deferred |
| Users | Shopper, admin, staff (or other roles) |
| User stories | Acceptance-oriented flows |
| FRs | ID, text, priority (`Must` / `Should` / `Could`) |
| Technical notes | Plugin vs core, entities, migrations, caching, events |
| UX outline | Checkout / admin touchpoints and copy patterns |
| Risks / open questions | Settings, concurrency, plugin compatibility, time zones |
| Release plan | Alpha / Beta / GA gates |

**Rule:** Non-goals and open questions stay out of the first implementation unless the user decides them. Prefer an admin setting when the PRD proposes one (e.g. mandatory vs optional slots).

## Place the work

Map each Must FR to the smallest correct layer:

1. **Existing plugin first** — Prefer extending the owning plugin (e.g. BOPIS → `Nop.Plugin.Pickup.PickupInStore`) over a new plugin or core-only change.
2. **Interface changes** — Extend `IPickupPointProvider` (or similar) in a deprecation-safe way so third-party providers keep compiling; default/no-op behavior for providers that ignore the new capability.
3. **Domain + migrations** — New entities under the plugin `Domain/`; schema via FluentMigrator (`[NopMigration(...)]`, `CreateTableIfNotExists` / upgrade migrations). See [reference.md](reference.md).
4. **Services** — Business rules (capacity, lead time, holds) in plugin or `Nop.Services`; keep controllers thin.
5. **Storefront / admin** — Checkout models + views in `Nop.Web`; admin configure/edit under the plugin Views; localize every new string.
6. **Cross-cutting** — Persist times in UTC, display in store time zone; multi-store settings where the PRD says “per store” or “per pickup point”; short-TTL cache for availability with invalidation on place-order; message tokens (`%Order.…%`) for email/PDF; consumer events for integrations (`Could`).

## Sequence by priority

Implement in this order unless the PRD’s release plan says otherwise:

1. **Alpha (Must config + selection):** settings/entities/migrations → admin configuration → checkout selection behind a feature setting.
2. **Hard Must invariants next:** reservation hold/release, atomic capacity at order placement, slot persisted on the order.
3. **Beta (Must surfacing):** order details (public + admin), invoices, message tokens, optional admin filters.
4. **GA (Should):** reschedule, blackouts, docs — only after Alpha/Beta Musts work.
5. **Could:** Web API / plugin events last.

Skip Should/Could in the same change set as Alpha unless the user asks for a full v1.

## Build checklist

Copy and track:

```
PRD → code:
- [ ] Goals/non-goals restated; open questions listed with proposed defaults
- [ ] Must FRs mapped to files/layers
- [ ] Plugin vs interface extension chosen (deprecation-safe)
- [ ] Entities + FluentMigrator migration(s)
- [ ] UTC storage / store TZ display
- [ ] Localization resources for new strings
- [ ] Multi-store (or per-pickup-point) settings as specified
- [ ] Availability cache + invalidation on order placement
- [ ] Concurrency: hold TTL + atomic capacity check at place-order
- [ ] Order persistence + admin/public display for Must surfacing
- [ ] Message tokens for emails/PDF when FR requires them
- [ ] Feature gated by setting for Alpha
- [ ] Unit/integration tests for capacity and lead-time rules
- [ ] verify-nopcommerce proof for checkout + admin config paths
```

## Concurrency pattern (capacity-like resources)

When the PRD requires holds and no overbooking:

1. **Hold** a reservation with a short TTL (PRD example: 15 minutes) at checkout selection.
2. **Release** holds that expire or when checkout is abandoned.
3. **Commit atomically** at order placement (DB transaction or equivalent) so two checkouts cannot exceed capacity.
4. **Do not** trust only the cached “slots remaining” count for the final write.

## UX touchpoints

Follow the PRD’s outline; default BOPIS pattern when unspecified:

- Shipping method → Pick up in store → pickup point → date (next N days) → slot list with remaining capacity.
- Full slots disabled and labeled (e.g. “Full”).
- Confirmation + email state the reserved window and location in one sentence.

## Prove

1. Automated tests for lead time, blackout/full slots, and atomic overbooking.
2. Manual/UI: configure slots in admin, complete a pickup checkout with a slot, confirm order details/email tokens — via `verify-nopcommerce`.
3. Capture proof under that skill’s artifacts path.

## Additional resources

- Platform conventions and Pickup Time Slots case study: [reference.md](reference.md)
- Worked FR → file mapping example: [examples.md](examples.md)
