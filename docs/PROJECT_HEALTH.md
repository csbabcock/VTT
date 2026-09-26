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

## Attack animation integration — 2026-09-21

- Targeted attacks now request presentation after a completed combat result, including misses. Rejected actions do not animate.
- The gameplay ThirdPerson controller has an Attack trigger and state using Standing Melee Attack Downward. It returns to Idle Walk Run Blend after one cycle; the locomotion default and existing test controller are preserved.
- InGameCombatController routes the result through AttackAnimationFeedback and IAttackAnimationPlayer. The existing OwnerNetworkAnimator supplies synchronized trigger playback for the owner and direct Animator playback when unspawned.
- Damage still resolves immediately. Impact-frame damage, facing adjustments, movement locks, and animation queues are outside this first integration.
- Boundary rationale: the small presentation interface keeps Netcode out of GameCore and attack rules out of the animation adapter; no new ECS framework or assembly is introduced.
- Manual check: start MainMenu, host a session, choose Attack, and click an in-range target. Check both hits and misses, return to locomotion, a second attack, rejected actions, and visibility from another client.
- Automated validation: see the focused test result recorded below. Live host/client playback remains unverified.
- Validation: Unity 6000.7.0a6 compiled the temporary project and passed all 16 focused EditMode tests (AttackAnimationFeedbackTests and CombatActionExecutorTests). Result: Temp/AttackAnimationValidation/results.xml. git diff --check passed. Visual playback and live network replication still require the manual check above.

## Unity warning cleanup — 2026-09-21

- MainMenuView, CharacterCreationView, and InGameUIView use the versioned PanelRenderer reload callback signature. Reload behavior is unchanged; version-based deduplication is not introduced.
- Four render-pipeline assets have internal names matching their filenames, with GUIDs and references preserved.
- Unity 6000.7.0a6 reserialized LiberationSans.ttf importer metadata from version 2 to 4, preserving its GUID.
- Validation: a separate Library/CodexWarningValidation project compiled without the reported obsolete-callback warnings and verified all four imported asset names. No new tests were added for this API/metadata cleanup; interactive UI behavior was not exercised.
- The Pipeline package's non-automated-mode warning is informational during normal interactive Editor use; package source and launch flags are unchanged.
- User confirmed the integrated attack animation works in the Editor after warning cleanup. A separate host/client replication check has not been reported.

## Melee approach movement — 2026-09-21

- Attack approach no longer uses WorldMeleeApproach teleport fallbacks or unconditional final repositioning. Same-cell adjustments use the existing animated locomotion; actors without usable locomotion fail the approach instead of teleporting.
- Finalization waits for movement to finish, including approved network movement that starts after the first wait. Movement timeouts cancel locomotion instead of forcing the character into range; combat still validates reach before resolving.
- Grid arrival now uses a 0.05-world-unit horizontal tolerance rather than half a cell. Horizontal steps cannot overshoot the destination, short final adjustments can complete, and small grounding offsets are tolerated in either direction.
- Validation: Unity 6000.7.0a6 passed 44 focused EditMode tests in the isolated Library/CodexWarningValidation project. Results: Library/CodexWarningValidation/movement-final-results.xml. git diff --check passed.
- Live visual and host/client verification remains outstanding: test long approaches, the final partial cell, short moves, and blocked movement; confirm attack playback begins after locomotion stops. Arrival still permits a tiny positional correction within its tolerance. Impact-frame damage timing is unchanged.

## Target-facing attack lunge — 2026-09-21

- Completed attacks pass the target to the presentation adapter. AttackMotionPresentation turns the Skeleton toward that target, moves it to body-contact spacing during the swing, then eases it back to its original pose. Timing follows the Attack state's normalized playback time; interruption, disable and despawn restore the pose.
- The actor root and gameplay collider remain stationary. This supplies visual contact, not physics-based hit detection; combat damage timing is unchanged. Player locomotion is held during the presentation.
- Grid attack approaches now end at cell centers in local and network play, replacing the former local contact-position destination. The lunge handles the temporary distance from that center to the target.
- OwnerNetworkAnimator retains synchronized attack triggers and sends the visual target position/contact spacing through an owner-restricted RPC. GameCore presentation contains no Netcode dependency; pure AttackMotion geometry/timing is covered by EditMode tests.
- Validation: Unity 6000.7.0a6 passed 55 focused EditMode tests, including actual prefab/Animator sampling to verify visible displacement, return, and unchanged actor position. Results: Library/CodexWarningValidation/lunge-final-results.xml. git diff --check passed.
- Still requires live host/client visual checks for side/rear/diagonal targets, contact timing, repeat attacks, and interruption. The current lunge reaches contact at 30% of the clip, holds through 55%, and returns by the end; facing returns to the resting orientation afterward.

## Attack presentation sequencing — 2026-09-25

- Visual melee movement now approaches contact before triggering the attack, holds at contact until the Animator leaves the attack state (including its outgoing blend), then returns to the original pose.
- The owner delays the replicated attack trigger until arrival; remote clients receive the approach RPC as before.
- Updated EditMode regression coverage for delayed triggering, holding through clip completion, returning after state exit, and cancellation.
- Validation: git diff --check passed. Focused Unity EditMode batch run exited during startup with code 1 and produced no test results; compilation, in-game playback, and multiplayer timing remain unverified.

### Run presentation follow-up — 2026-09-25

- Approach plays the existing forward run clip; return plays that same looping clip backward while facing the target, then restores locomotion at the starting position.
- Attack state playback speed is now 2x. Completion still follows the actual Animator state, so the faster swing finishes before return begins.
- Regression checks now cover forward/reverse playback, shared run clip wiring, restoration to locomotion, and attack speed. Runtime verification remains pending the Unity startup/test-run limitation noted above.

### Normal attack travel speed — 2026-09-25

- Replaced fixed 0.2-second approach and 0.15-second return durations with distance divided by the character's configured run speed (PlayerController.SprintSpeed), using constant travel speed in both directions.
- Forward/reverse run clips remain at normal playback speed; only the attack state remains at 2x.
- Updated regression cases to check configured run speeds of 3 and 6 units/second and equal outbound/return timing. Static diff checks passed; Unity playback tests remain unverified due to the previously observed startup failure.

### Smooth attack sequence and retained facing — 2026-09-25

- Removed forced idle switches between approach, attack, and return. Entry/return-to-idle now crossfade over 0.12 seconds; the attack state blends directly into reverse run after its full clip completes.
- CombatMotion selects the return transition for targeted attacks and retains the idle exit for attacks without a movement sequence. Travel still uses normal run speed, with only attack playback at 2x.
- On normal return completion, transfer target-facing rotation from the visual to the actor before restoring the skeleton pose, avoiding the old orientation snap. Cancellation still restores the visual pose.
- Updated regressions for blend transitions, the full-clip exit gate, and a target to the right so facing preservation is exercised. Controller structure/transition checks and git diff --check pass. Unity execution remains unverified because the focused runner previously failed during startup.
