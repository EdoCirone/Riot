## README.v0.18.1.md


## DISSENSO
A turn-based political tactics game played on a hex grid.

# Play DISSENSO on itch.io

Current version: v0.18.1
Status: playable early prototype, under active development

# About
DISSENSO is a 2D turn-based tactics game in which you lead a political march through police lines and toward strategic objectives.

Units spend Action Points to move and act, while Morale replaces conventional health. Combat is deterministic: positioning, available actions and unit statistics decide the outcome. Confrontational and non-violent tactics serve different purposes and can change how a turn develops.

The current build focuses on the tactical layer. It contains one complete scenario with win and loss conditions, but it is not yet representative of the planned campaign structure.

DISSENSO is an independent project inspired in part by the 1980 Italian board game Corteo. It is not an official adaptation.

# Current prototype
Flat-top hex grid using axial coordinates

A* pathfinding and shared tactical legality checks

Turn structure: player actions followed by a sequential police phase

Action Points, Attack, Defense and Morale

Deterministic skirmishes with morale loss

Movement, automatic move-and-attack and charges with knockback

Throwable items and placeable barricades

Chant action for restoring group morale

Sit/Stand action, trading mobility for additional defense

Inventory-based consumable actions

Basic police AI

Objective control and turn-limit win/loss conditions

Reachable-cell and valid-target previews

Boot sequence, scene transitions and loading screen

Separate Master, Music and SFX controls with saved settings

# Controls
Input	Action
Left mouse button	Select units, choose destinations or targets, and confirm
Right mouse button	Cancel the current action or deselect
Space	End the player turn
C	Charge
T	Throw
B	Place barricade
R	Chant
G	Sit or stand
WASD	Move the camera
Mouse wheel or Q/E	Zoom
Actions can also be selected through the on-screen action panel. Throwing and placing a barricade require the corresponding inventory item.

# Development direction
The long-term game is intended to extend beyond isolated tactical battles. Planned systems include:

A persistent movement roster

Recruitment, arrests, desertions and unit recovery

Political groups with distinct roles and internal tensions

Aggressiveness, repression and cohesion across missions

A campaign played across a larger city map

Consequences for violent and non-violent choices

These systems are development goals and are not part of the current public prototype.

# Technology
Unity 6000.4.5f1

C#

Universal Render Pipeline, 2D

Unity Input System

DOTween

ScriptableObject-based data and event channels

# Music credits
Main Menu: Furious by FASS, available through Uppbeat

Gameplay: music from Three Red Hearts by Abstraction, released under CC0

# Developer
Game design and development by Edoardo Cirone.
