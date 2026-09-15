# D2 · Microgrid Inventory Management

Status: **Agreed** · Owner: **Member 2** · Individual marks: **20 / 65**

## Goal

Maintain an accurate, geographically real register of what the grid actually
has to offer — hubs, their capacity, their bookable windows — and make that
inventory findable by a prosumer standing somewhere with a phone. Protect the
register from an administrative mistake: **a hub cannot vanish while energy is
still booked at it.**

## Scope, as agreed

**Web application** — Administrators create and configure physical solar grid
hubs, inputting GPS coordinates, overall capacity (kW/h), and battery storage
slot limits.

**Mobile application** — Prosumers view the active grid nodes plotted on a
Google Maps interface to find nearby energy drop-off locations.

**Web Service & DB** — The backend strictly enforces a business rule that
prevents a microgrid node from being deactivated if it currently holds active
energy reservations. Infrastructure data is persisted in a dedicated MongoDB
collection.

## Requirements owned

| Doc | Covers |
|---|---|
| `requirements/REQ-NODE-microgrid-node-management.md` | Node CRUD, GPS/capacity/battery-slot validation, the deactivation guard |
| `requirements/REQ-SLOT-energy-slot-management.md` §4.1–4.2 | Slot CRUD and operator availability updates — the *administration* half; D3 owns the capacity-consumption half (see below) |
| `requirements/REQ-VIEW-dashboards-views-and-maps.md` §2.5 | The map screen: pins, bottom sheet, "Book here" handoff |

## Business rules owned

**BR-09** (no deactivation with active reservations) · BR-19 (capacity
reduction guard) · BR-20 (deactivated nodes invisible/unbookable)

## Data owned

`SolarStationInfo`, `EnergyBookingSlots` collections · SQLite `node_cache` table

## Technical tiers this domain has code in

| Tier | What lives here |
|---|---|
| C1 Web API | `NodesController`, `SlotsController` (shared with D3), `NodeService`, `SlotService` (shared with D3) |
| C2 Web app | `features/nodes/`, `features/slots/` — node admin, schedule editor, slot calendar |
| C3 Android | `ui/map/`, `data/local/` node-cache portion — Google Maps fragment, pin rendering, bottom sheet |

## The handoff this domain owns the near side of

Tapping a map pin carries `nodeId` into D3's booking flow. This domain is
responsible for the map screen and the node data on it; D3 is responsible for
everything from the moment "Book here" is tapped. See `domains/README.md`
"Cross-domain handoffs".

## Definition of done

Everything in `project-config.md` §8, plus:

- Deactivating a node with one future `Approved` reservation is refused with
  `E4004` and names the blocking reservation — screenshot this for the report,
  it is the clearest demonstration of BR-09.
- The map renders real Colombo-area coordinates (see `seed/seed.js`), not
  placeholder points, and a deactivated node's pin disappears on refresh.
- Reducing `batterySlotCount` below the count of approved future reservations
  is refused (`E4003`, BR-19) — this is easy to forget and easy to test.
- Google Maps API key setup is documented in the deployment section of the
  report; the key itself is never committed (see `.gitignore`).

## Viva questions this domain should be able to answer cold

1. Why does node deactivation check *future* reservations only, and how does
   the API decide "future"? → `REQ-NODE` AC-5, glossary "Active reservation"
2. What happens to a reservation if the node's schedule is later narrowed so
   the slot falls outside opening hours? → `REQ-NODE` EC-3
3. Where does capacity accounting for a slot actually live, and why isn't it
   here? → `REQ-SLOT` §4.2, BR-15 (it's D3's — the slot's availability is
   consumed by the reservation engine, not decided by inventory admin)
