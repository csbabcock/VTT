# Project Health

Last reviewed: 2026-08-02.

This is a snapshot, not a permanent truth. Update it after meaningful code changes, test runs, architecture decisions, or long breaks.

## Summary

| Area | Health | Notes |
| --- | --- | --- |
| Product direction | Strong | `README.md` has a clear VTT vision: 3D tabletop, 2D support, DM workflow, player perspectives, combat, encounter mode, and UI. |
| Architecture direction | Good | Existing docs already favor pragmatic SOLID, MVP for complex UI, server validation, and testable plain C# services. |
| Test surface | Good, current result unknown | There are 48 EditMode test files across combat, encounter, rules, networking bindings, player data, UI helpers, and utilities. Existing test logs are stale and show Unity returning `1`. |
| Assembly boundaries | Medium | Runtime code is mostly in broad `GameCore`; networking is split into `GameCore.Networking`; editor and tests have their own asmdefs. |
| Namespace/folder consistency | Mostly good | Most domains use `GameCore.<Domain>` namespaces. Some legacy or package-style scripts still live in root `GameCore` or no namespace. |
| AI handoff | Improving | New docs define check-in, roadmap, architecture standards, and AI assistant guidance. |
| Current worktree | Dirty before this doc pass | Pre-existing modifications were observed in `Assets/QuickOutline/Samples/Scenes/QuickOutline.unity`, `Packages/manifest.json`, `Packages/packages-lock.json`, and `ProjectSettings/ProjectSettings.asset`. |

## Current Architecture Map

Primary runtime assembly:

- `GameCore`: most gameplay, UI, rules, actors, encounter, combat, player data, visuals, and non-networking runtime code.

Additional assemblies:

- `GameCore.Networking`: Netcode runtime components and multiplayer/session integration.
- `GameCore.Editor`: editor-only tooling.
- `GameCore.Tests.EditMode`: NUnit/EditMode tests.

Major domains:

- Actors: player actor identity and character sheet authority.
- Combat: attacks, targeting, action economy, damage, and feedback.
- EncounterMode: grid, movement, pathing, reachable cells, turn order, and encounter state.
- Networking: Netcode behaviours, spawning, session launcher, network identity, replicated state.
- PlayerData: character sheets, ruleset adapters, D&D 5e calculations, persistence services.
- UI: UI Toolkit main menu, character creation, in-game views, presenters, models, and services.
- DmTools: fly camera, player spectate gateway, DM session bootstrapping.
- Visuals/Interaction: highlights, screen picking, overlays, tint effects.

## Architecture Strengths

- Clear intent to keep rules and state logic testable.
- MVP pattern already exists for MainMenu, CharacterCreation, and InGame UI.
- Many services are plain C# and covered by EditMode tests.
- Networking has its own asmdef and uses authority-oriented interfaces.

## Architecture Risks

- `GameCore` is broad enough that domain boundaries can blur over time.
- Some legacy scripts still use broad or missing namespaces, especially mobile/controller utility scripts.
- Character creation has active TODOs around persistence and refresh flow.
- Large UI Toolkit files may become hard to maintain if more logic drifts into Views.
- Test result is currently unknown because existing logs do not show a clean passing baseline.
- Third-party/sample assets have local modifications; confirm whether they are intentional before touching them.

## Check-In Procedure

Use this procedure whenever returning after a break.

1. Read project memory:

```text
README.md
docs/AI_ASSISTANT_GUIDE.md
docs/PROJECT_HEALTH.md
docs/ROADMAP.md
docs/ARCHITECTURE_STANDARDS.md
DECISIONS.md
TASKS.md
```

2. Inspect local state:

```powershell
git status --short
rg --files Assets -g "*.asmdef"
rg --files Assets\Tests\EditMode -g "*Tests.cs"
rg "TODO|FIXME|HACK|NotImplemented" Assets docs README.md CONTEXT.md DECISIONS.md TASKS.md -g "*.cs" -g "*.md"
```

3. Inspect the target area before editing:

```powershell
rg "namespace GameCore.Combat|class .*Attack|interface IAttack" Assets\Scripts\Combat Assets\Tests\EditMode -g "*.cs"
```

Replace the query and folders with the domain being changed.

4. Verify:

- Run the smallest relevant EditMode tests first.
- Run the full EditMode suite before considering a broad refactor complete.
- Use PlayMode or manual Unity validation for scene/lifecycle/networking behavior.

## Health Check Output Format

When asked for a project health check, return:

```text
Current progress:
Git state:
Architecture health:
Roadmap position:
Tests/builds verified:
Unknowns:
Risks:
Recommended next 3 tasks:
Files to read before coding:
```

Do not say tests pass unless they were actually run in the current session.

## Immediate Recommendations

1. Establish a fresh test baseline.
2. Finish character creation persistence and refresh flow.
3. Keep character creation MVP boundaries tight while adding validation and background selection.
4. Delay assembly splitting until after the next feature stabilizes.
5. When a new reusable module appears twice, extract it behind a small API and add tests.
