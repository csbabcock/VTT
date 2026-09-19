# Architecture Standards

These standards are guardrails for keeping the VTT project understandable after long gaps between sessions. They are not meant to force ceremony. Use the simplest structure that preserves clear ownership, testability, and future reuse.

## Architecture Goals

- Gameplay rules should be testable without scenes.
- UI should be easy to change without breaking rules or networking.
- Networking should make authority explicit and keep client requests validated.
- Reusable systems should not depend on VTT-specific UI, scenes, or D&D 5e data unless that dependency is the point of the module.
- Code should be organized so a returning maintainer can find the owner of a behavior quickly.

## Layering

Use these layers as the default mental model:

| Layer | Owns | Should avoid |
| --- | --- | --- |
| Domain/rules | Combat math, character state, ruleset calculations, movement validation, data models | MonoBehaviour lifecycle, UI Toolkit types, Netcode types, scene lookups |
| Application services | Multi-step use cases such as attack execution, encounter movement, turn order, persistence workflows | Rendering details, direct input polling, direct scene searches |
| Unity adapters | MonoBehaviours, UI views, networking behaviours, scene bootstrap code | Duplicating business rules |
| Presentation | UI Toolkit views, presenters, visual effects, highlights, camera presentation | Owning authoritative gameplay state |
| Composition/wiring | Bootstraps, locators, registries, serialized references | Business rules and complex branching |

When a class mixes layers, either split it or write down why the coupling is currently acceptable.

## SOLID Guidance

- Single Responsibility: each class should have one reason to change. Split when a class owns state, rendering, input, networking, and rules at once.
- Open/Closed: add new implementations or strategies for new variants instead of repeatedly expanding central switch logic.
- Liskov Substitution: interfaces must have honest contracts. Implementations should not throw or silently no-op in ways callers cannot predict.
- Interface Segregation: prefer small role interfaces such as movement authority, sheet authority, target resolution, or UI input gates.
- Dependency Inversion: depend on abstractions at volatile boundaries: UI, persistence, networking, ruleset selection, random sources, and scene-level lookup.

Do not inject everything by default. Use constructor injection for plain C# classes, serialized references for scene-owned objects, and locators or registries only for deliberate scene-wide lookup.

## MVP For UI Toolkit

Use MVP for screens or panels with non-trivial state, validation, multiple modes, or service calls.

| Type | Responsibilities | Must not |
| --- | --- | --- |
| View | Bind UI Toolkit elements, expose events, render provided state, manage visual-only details | Calculate domain rules, mutate persistence, own gameplay state |
| Presenter | Handle user flow, validation, state transitions, service calls, and view updates | Query raw UI elements deeply when the view can expose intent methods |
| Model | Hold UI state and domain-facing data | Reference VisualElement, Button, UIDocument, MonoBehaviour, or scene objects |

Small widgets can stay View-only until logic grows. When the view starts calculating, branching across modes, or coordinating services, introduce a Presenter.

## Unity Code Organization

Current top-level runtime domains include:

- `Assets/Scripts/Actors`
- `Assets/Scripts/Combat`
- `Assets/Scripts/DmTools`
- `Assets/Scripts/EncounterMode`
- `Assets/Scripts/Interaction`
- `Assets/Scripts/Networking`
- `Assets/Scripts/PlayerController`
- `Assets/Scripts/PlayerData`
- `Assets/Scripts/Visuals`
- `Assets/UI`

New code should usually live in the closest existing domain. Avoid creating new top-level folders for a single class.

Recommended subfolders by domain:

- `Models` for data/state objects.
- `Services` for use cases and pure coordination.
- `Adapters` for translating between systems.
- `Definitions` for static content definitions.
- `Runtime` for Unity runtime components when a domain also has editor or data code.
- `Editor` for editor-only tooling.

UI folders should keep the established structure:

- `Models`
- `Views`
- `Presenters`
- `Services`
- `UXML`
- `USS`

## Namespaces

Namespaces should mirror ownership:

- `Assets/Scripts/Combat/Targeting` -> `GameCore.Combat.Targeting`
- `Assets/Scripts/EncounterMode/Grid` -> `GameCore.EncounterMode.Grid`
- `Assets/UI/MainMenu/Services` -> `GameCore.UI.MainMenu.Services`
- `Assets/Scripts/Networking/Runtime` -> `GameCore.Networking`

Use the root `GameCore` namespace only for truly shared gameplay infrastructure or legacy code being migrated. New feature work should prefer a specific namespace.

Third-party assets should remain isolated under their vendor namespace or folder. Do not modify third-party sample scenes or source unless that is the explicit task.

## Assembly Definitions

Current assemblies:

- `GameCore`
- `GameCore.Networking`
- `GameCore.Editor`
- `GameCore.Tests.EditMode`

Near-term standard:

- Keep networking code in `GameCore.Networking` when it depends on Netcode packages.
- Keep editor-only scripts in `GameCore.Editor`.
- Keep EditMode tests in `GameCore.Tests.EditMode`.
- Do not add an asmdef just for neatness.

Add a new runtime asmdef when one of these becomes true:

- A domain needs dependencies that the rest of `GameCore` should not inherit.
- A domain has a stable boundary and enough files to benefit from compile isolation.
- A reusable module should be consumed independently by another project or package.
- Tests need to target a domain boundary directly.

Candidate future split:

- `GameCore.Rulesets`
- `GameCore.PlayerData`
- `GameCore.Combat`
- `GameCore.EncounterMode`
- `GameCore.UI`
- `GameCore.Visuals`

Do this incrementally. Splitting assemblies too early creates churn and serialized reference risk in Unity.

## Reuse And Modularity

Make functionality generic only when there is a real second use, a clear extension point, or a volatile dependency to isolate.

Reusable code should:

- Have a small public API.
- Avoid scene references.
- Avoid hard-coded D&D 5e concepts unless it lives in a D&D 5e-specific namespace.
- Be covered by EditMode tests.
- Use adapters to connect to Unity, UI, Netcode, or VTT-specific models.

Prefer this dependency direction:

```text
UI / Networking / Unity components -> application services -> domain/rules models
```

Avoid this direction:

```text
domain/rules models -> UI Toolkit, Netcode, scenes, prefabs, or MonoBehaviours
```

## Testing Standard

Add or update tests for:

- Ruleset calculations.
- Combat resolution.
- Character state mutation.
- Encounter movement and validation.
- Action economy.
- Pathfinding and grid rules.
- Network authority and binding policy.
- UI presenter logic when it can be tested without a scene.
- Bug fixes that can regress.

Prefer EditMode tests for pure logic. Use PlayMode tests only when Unity lifecycle, scenes, physics, or actual networking behavior is required.

Test names should describe behavior, for example:

```text
DashMovement_AllowsSecondMoveWithinRemainingBudget
AttackResolution_NaturalOneAlwaysMisses
PointBuy_CannotIncreaseScorePastFifteen
```

## Pattern Justification

When adding a new pattern or abstraction, include this in the task notes or PR description:

```text
Problem:
Why now:
Alternative considered:
Cost:
Payoff:
Decision:
```

## Definition Of Done

- Behavior matches the requested acceptance criteria.
- Classes still have clear ownership.
- UI logic follows View-only or MVP deliberately.
- Server-authoritative gameplay mutations remain validated.
- Folder and namespace match the domain.
- Tests were added or a reason is recorded.
- Relevant docs were updated if state, roadmap, or decisions changed.
- Validation commands and unknowns are recorded in the handoff.
