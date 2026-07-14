# DISSENSO (RIOT)

Hex-grid tactical combat game in development (Unity, C#).

**Status:** core gameplay loop is playable end-to-end (winnable/losable), no public build yet.

## Concept

A turn-based tactical game on a hexagonal grid where you lead a protest march ("corteo") 
of demonstrator units against police lines. The core resource is Morale rather than 
straightforward HP, and units can choose between confrontational tactics and non-violent 
ones that trade mobility for resilience.

## Implemented systems

- **Hex grid** (flat-top, axial coordinates) with unified BFS-based pathfinding and 
  adjacency logic, shared across movement, attack targeting, and highlight rendering
- **Four core unit stats**: Attack, Defense, Morale, Action Points
- **Deterministic combat resolution** — no randomness
- **Arm-then-target action system**: select a unit, arm a special action, then click a 
  valid target to execute
  - **Charge** — fast dash into an enemy with knockback
  - **Throw** — ranged attack at fixed distance, unobstructed by units in its path
  - **Chant** — restores morale to the caster and all adjacent allies
  - **Sit/Stand** — trade the ability to move or attack for a defense bonus and immunity 
    to charges
- **5-slot action panel UI** with keyboard shortcuts
- **Inventory system** (polymorphic item data, stack-based) powering consumable actions
- **Turn structure**: player phase → basic police AI (approach + attack) → player phase
- **Visual feedback**: shader-based selection outline, camera follow/centering on the 
  selected unit
- **Win/lose conditions** based on objective-cell control and turn limits

## Known open issues

- Police AI still evaluates movement by straight-line distance rather than real path 
  cost (already fixed on the player side)
- Defender has no bump/recoil animation yet on charge or melee
