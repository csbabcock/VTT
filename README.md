# VTT

A Unity-based **virtual tabletop** for tabletop role-playing games. VTT is built around **D&D 5e-style** rules and workflows: create a character, host or join a session, run grid-based encounters, and resolve combat with server-authoritative multiplayer.

The goal is a maintainable, multiplayer-first tabletop experience—not a generic game engine wrapper. UI flows use **Model–View–Presenter (MVP)**; gameplay logic lives in plain C# services that are testable outside the Unity scene graph.

## Features

### Character creation
- **Ability scores** via Roll (4d6 drop lowest), Standard Array, Manual entry, or Point Buy (27 points, scores 8–15)
- Drag-and-drop assignment of rolled scores onto ability rows
- **Class** and **Race** selection tabs with a detail panel for options
- Character data persisted through a ruleset-aware player data layer

### Multiplayer sessions
- **Host** or **Join** from the main menu
- Built on **Netcode for GameObjects** with a transport-agnostic `ISessionLauncher` abstraction so UI code does not depend on a specific netcode stack
- Deferred player spawning so participants appear only after the gameplay scene has loaded
- Replicated character identity, combat state, and player view state across clients

### Encounter mode
- Grid-based tactical play with pathfinding, reachable-cell visualization, and D&D 5e diagonal movement rules
- Server-validated movement, dash support, and turn-order management
- Initiative rolling and encounter session state (active encounter, current turn owner)

### Combat
- Attack resolution, action economy tracking, and melee reach validation
- HP, temporary HP, death saves, conditions, exhaustion, and inspiration
- Target highlighting and combat feedback (e.g. damage flash)
- Character sheet mutations applied through dedicated services and authority interfaces

### DM tools
- Fly camera for overhead session control
- Spectate player perspectives during a session

### In-game UI
- UI Toolkit–based main menu and in-game HUD
- Diegetic-inspired shared styling (`DiegeticTheme.uss`)
- Character sheet views driven by ruleset calculators and content queries

## Tech stack

| Area | Choice |
|------|--------|
| Engine | Unity 6 (`6000.5.0a8`) |
| Rendering | Universal Render Pipeline (URP) |
| UI | UI Toolkit (UXML / USS) |
| Networking | Netcode for GameObjects 2.x |
| Input | Unity Input System |
| Camera | Cinemachine |
| Tests | Unity Test Framework (Edit Mode), NUnit |

## Project structure

```
Assets/
├── GameData/Rulesets/DnD5e/   # Classes, races, backgrounds, spells, skills (data)
├── Scenes/
│   ├── MainMenu.unity         # Menu, session launch, character creation
│   └── Playground.unity       # Gameplay / encounter sandbox
├── Scripts/
│   ├── Actors/                # Player actors and sheet authority
│   ├── Combat/                # Attacks, action economy, targeting, feedback
│   ├── DmTools/               # DM camera and spectate utilities
│   ├── EncounterMode/         # Grid, movement, turn order, encounter manager
│   ├── Networking/            # NGO runtime + session abstractions
│   └── PlayerData/            # Character sheets, ruleset adapters, persistence
├── UI/
│   ├── MainMenu/              # MVP: models, views, presenters, UXML/USS
│   ├── InGame/                # In-session UI foundation
│   └── Scripts/Core/          # Shared MVP interfaces, scene loading
└── Tests/EditMode/            # Unit tests for rules, combat, encounter, networking
```

Root-level `CONTEXT.md`, `DECISIONS.md`, and `TASKS.md` capture current UI state, architecture decisions, and active work—they are the authoritative dev notes for ongoing sessions.

## Architecture notes

- **MVP for complex UI**: Views render and forward events; Presenters own logic and state transitions; Models hold domain data.
- **SOLID, pragmatically**: Small interfaces at boundaries (networking, sheet authority, grid selection); composition over deep inheritance; patterns only when they remove duplication or improve testability.
- **Server authority**: Encounter movement and combat mutations are validated on the host/server; clients request changes through authority interfaces.
- **Ruleset layer**: `DnD5e` content and calculators are separated from presentation so new rulesets can be added without rewriting UI.

## Getting started

### Prerequisites
- [Unity 6](https://unity.com/download) (editor version **6000.5.0a8** or compatible)
- A Git client

### Open the project
1. Clone the repository.
2. Open the project folder in Unity Hub.
3. Let Unity import assets and resolve packages (first open may take a few minutes).
4. Open `Assets/Scenes/MainMenu.unity` and press **Play** to try the menu and character creation flow.
5. Use **Host** to start a session (loads `Playground`) or **Join** to connect to another instance.

### Running tests
Open **Window → General → Test Runner**, select **EditMode**, and run all tests. Coverage includes combat resolution, encounter movement, grid math, networking bindings, and character sheet rules.

## Current status

VTT is under active development. Character creation, session hosting, grid encounters, and core combat loops are in place; background selection UI and additional polish items are tracked in `TASKS.md`.

## Contributing

When adding UI with non-trivial logic, follow the existing MVP layout under `Assets/UI/`. When changing rules or services, add or update Edit Mode tests in `Assets/Tests/EditMode/`. See `DECISIONS.md` for product and architecture conventions.
