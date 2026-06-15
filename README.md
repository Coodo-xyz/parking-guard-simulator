# 🅿️ Parking Guard Simulator

> A S&box game — manage your parking lot, deal with chaos, expand your empire.

## Concept

Start with an empty lot and build your way up to a full underground parking complex. Manage barriers, collect fees, handle troublemakers, and keep your spots clean.

### Core mechanics
- **Vehicles** arrive, park, and leave — collect payment at the barrier
- **Pricing** varies by vehicle type and parking duration
- **Problems** : thieves, vandals, non-payers, abandoned cars, degraded spots
- **Upgrades** : automate the barrier, build a booth, add a vending machine, go underground

## Tech stack

- **Engine**: [S&box](https://sbox.game) (Facepunch)
- **Language**: C#

## Project structure

```
code/
  GameManager.cs       # Core game loop
  entities/
    Vehicle.cs         # Vehicle logic (arrival, parking, departure)
    Barrier.cs         # Entry/exit barrier
    ParkingSpot.cs     # Individual spot state
  systems/
    CashSystem.cs      # Money, fees, pricing
    PopularitySystem.cs
    EventSystem.cs     # Random events (thieves, vandals...)
  ui/
    HUD.cs
```

## Getting started

1. Clone this repo into your S&box addons folder
2. Open S&box and load the addon
3. Hit Play

## Team

Built by [Coodo Interactive](https://github.com/Coodo-xyz)
