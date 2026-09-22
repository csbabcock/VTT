# Roadmap

This roadmap is organized around project capability, not calendar dates. Keep each milestone small enough that the next session can pick up one concrete task without rediscovering the whole project.

## Current Status

Last reviewed: 2026-08-02.

Implemented foundations:

- Character creation flow with ability score generation, drag-and-drop assignment, standard array, manual mode, point buy, class/race selection, and rules-backed stat updates.
- Main menu host/join flow.
- Multiplayer spawning, networked character identity, replicated player/combat state, and server validation for important gameplay actions.
- Encounter mode with grid movement, reachable cells, pathfinding, diagonal movement support, dash support, initiative, turn order, and active-turn ownership.
- Foundational combat with unarmed strike, melee reach validation, action economy, HP/temp HP, death saves, conditions, exhaustion, inspiration, target highlighting, and combat feedback.
- DM fly camera, player spectate support, player sheet viewing, combat state adjustment, and encounter controls.
- UI Toolkit foundation with MVP-style main menu, character creation, in-game HUD, and character sheet views.
- EditMode test suite covering rules, combat, grid, encounter, networking bindings, UI helpers, and state logic.

Known current gaps:

- Character creation save-to-disk and character list refresh are still TODOs.
- Ability score validation and modifier wiring are still listed as next steps in `TASKS.md`.
- Background selection was removed from tabs and needs a new location.
- Test logs in the repo are stale and show Unity returning `1`; current test health needs a fresh run.
- Assembly boundaries are still broad, with most runtime code under `GameCore`.

## Milestone 0: Project Operating System

Goal: Make the project easy to resume with AI assistance.

Status: Complete as of the 2026-08-02 documentation pass.

Acceptance criteria:

- AI check-in guide exists.
- Architecture standards exist.
- Roadmap exists.
- Health snapshot exists.

## Milestone 1: Baseline Stabilization

Goal: Establish a trustworthy current baseline before adding larger systems.

Recommended tasks:

- Get EditMode tests running from Unity Test Runner or batchmode and record the current result.
- Fix or document any compile/test blocker.
- Update `docs/PROJECT_HEALTH.md` after the test run.
- Resolve or intentionally keep pre-existing local changes in packages, project settings, and sample scenes.
- Confirm current scenes in Build Settings: MainMenu and Playground.

Acceptance criteria:

- There is a known passing/failing baseline.
- Any failure has an owner or follow-up task.
- Local worktree status is understood.
- The next feature can start from a known state.

## Milestone 2: Character Creation Completion

Goal: Finish the current character creation loop so a player can create, save, and select a usable character.

Recommended tasks:

- Add missing ability score validation from `TASKS.md`.
- Ensure modifier calculations update from score changes in every mode.
- Implement save-to-disk through `CharacterFileService`.
- Refresh the character list after save.
- Add background selection in a better location than the old tabs.
- Add focused EditMode tests for validation, modifier calculation, point buy boundaries, and save data mapping.

Acceptance criteria:

- A created character can be saved and selected in a later session.
- Invalid ability scores are rejected or clamped consistently.
- All ability score modes produce correct derived values.
- View remains passive; Presenter owns UI logic; Model owns state.

## Milestone 3: Ruleset Modularity

Goal: Keep D&D 5e support strong while preventing the whole project from becoming hard-coded to one ruleset.

Recommended tasks:

- Review `GameCore.PlayerData.Rulesets` boundaries and identify what is truly D&D 5e-specific.
- Keep D&D 5e IDs, formulas, and data in D&D 5e-specific types.
- Define a small ruleset-facing API for character creation, derived stats, combat stats, and content queries.
- Avoid generalizing everything until another ruleset or clear extension point exists.
- Consider a future `GameCore.Rulesets` asmdef only after the boundary is stable.

Acceptance criteria:

- New ruleset-specific behavior does not leak into unrelated UI or combat services.
- Ruleset calculations remain EditMode-testable.
- Character creation asks ruleset services for data instead of hard-coding content where practical.

## Milestone 4: Encounter Usability

Goal: Make tactical play clear, predictable, and comfortable for players and the DM.

Recommended tasks:

- Improve movement intent feedback: selected actor, reachable cells, path preview, invalid move reason.
- Make turn state and action state visible in the in-game UI.
- Tighten player/DM permissions for encounter actions.
- Add tests for edge cases in movement, dash, grounding, path validation, and turn ownership.
- Add one lightweight runtime sanity checklist for MainMenu -> Host -> Playground -> Encounter flow.

Acceptance criteria:

- A returning player can tell whose turn it is, where they can move, and why an action is blocked.
- Movement decisions are validated server-side.
- Encounter services remain testable without scenes where practical.

## Milestone 5: Combat Expansion

Goal: Grow from unarmed strike into a reusable tabletop combat foundation.

Recommended tasks:

- Add weapon-backed melee attacks.
- Add ranged attack definitions and range validation.
- Add combat log entries that explain rolls, modifiers, hit/miss, damage, and resource use.
- Add condition/effect application through services rather than direct UI mutation.
- Add spell attack or saving throw groundwork only after weapon/ranged attack flow is stable.

Acceptance criteria:

- Combat actions flow through shared attack/action services.
- UI and networking do not duplicate combat rules.
- Tests cover attack outcomes, action costs, range, damage, and invalid actions.

## Milestone 6: Networking Authority Hardening

Goal: Make multiplayer behavior predictable and resistant to invalid client state.

Recommended tasks:

- Audit all gameplay mutations for authority owner and validation path.
- Standardize client request -> server validation -> replicated result flow.
- Add tests around binding policies and authority helpers where possible.
- Document what is trusted client-side for responsiveness versus validated server-side.

Acceptance criteria:

- Important gameplay state cannot be mutated directly by a non-authoritative client.
- Networking code stays isolated in `GameCore.Networking` or explicit adapters.
- Domain services can be tested without Netcode when practical.

## Milestone 7: DM Toolkit

Goal: Reduce DM overhead during preparation and live play.

Recommended tasks:

- Encounter setup panel for start/stop encounter, participant list, initiative, and turn controls.
- Lightweight 3D blockout tools for fast scene shaping.
- Hazard/trap markers with simple reveal/trigger states.
- Notes/journal foundation.
- Handout/image sharing foundation.
- Audio hooks for ambience and sound effects.

Acceptance criteria:

- The DM can run a small encounter without switching between unrelated tools.
- DM tools are modular and do not own core gameplay rules.
- Any tool that mutates shared session state uses the same authority pattern as gameplay.

## Milestone 8: Content And Packaging

Goal: Make the project usable outside the editor for real sessions.

Recommended tasks:

- User data folder policy for characters and saved sessions.
- Import/export format for characters and rules data.
- Build pipeline notes.
- Basic settings screen.
- Error reporting/logging strategy for failed loads or network setup.

Acceptance criteria:

- A build can host/join and load saved character data.
- Content paths are not editor-only.
- Recoverable errors are shown clearly to the user.

## Backlog Buckets

- Character data: save/load, import/export, validation, backgrounds, equipment.
- Rules: D&D 5e coverage, derived stats, proficiencies, weapons, spells, conditions.
- Encounter: movement UX, elevation, line of sight, cover, visibility, darkness.
- Combat: attacks, saves, effects, reactions, resources, log clarity.
- DM tools: blockout, hazards, notes, handouts, audio, session controls.
- UI: in-game HUD, character sheet editing, accessibility, navigation polish.
- Networking: authority, reconnection, lobby/session discovery, latency handling.
- Architecture: assembly splits, namespace cleanup, reusable modules, tests.

## Roadmap Update Rule

After each meaningful session:

- Mark completed tasks.
- Move the next smallest actionable item to the top of its milestone.
- Add newly discovered risks to `docs/PROJECT_HEALTH.md`.
- Add lasting product or architecture decisions to `DECISIONS.md`.
- Do not rewrite the roadmap just because priorities shifted; add a dated note if the shift matters.

### 2026-09-21 — First attack animation

The existing targeted unarmed-strike flow now drives the imported attack clip through the gameplay Animator and existing owner-authoritative NetworkAnimator. Both hits and misses animate; rejected actions do not. Next: verify host/client playback in the gameplay scene before adjusting foot placement or impact timing.
