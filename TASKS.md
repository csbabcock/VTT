This file is authoritative. Changes must be additive or explicitly marked.



\# TASKS



\## Definition of Done (lightweight)

\* Behavior matches acceptance criteria.

\* No obvious duplication introduced.

\* Classes remain single purpose; refactor if responsibilities merged.

\* If UI logic is non trivial, it lives in a Presenter, not the View.

\* Public methods and types have clear names and minimal side effects.

\* Edge cases handled or explicitly tracked as a follow up task.

\* Build passes and basic runtime sanity check completed.



\## Completed (this session)



\* Removed Abilities and Background tabs from CharacterCreationView

\* Moved ability score inputs into stats-panel ability-scores-grid

\* Consolidated ability-input-grid into ability-scores-grid (single grid for input/display)

\* Fixed IntegerField text visibility (styled #unity-text-input for proper colors)

\* Updated CharacterCreationView.cs to dynamically create IntegerFields in ability stat rows

\* Cleaned up unused CSS for removed ability input section

\* Refactored character creation drag-and-drop to SOLID/MVP: View passive (events + UI updates only), Presenter holds drag state and assignment/swapping logic, DragAndDropHandler service for detection and visual feedback

\* SOLID/MVP refactor: Model RolledDroppedIndices and SetRolledScores(..., droppedIndices); Presenter uses IAbilityScoreRoller, validates manual score 3–18; View uses state for dice chip styling; IAbilityScoreRoller service + CharacterCreationView.RolledScores.cs partial; duplicate rolled-score code removed from main View

\* Dice chip styling: kept chips default (primary) text color; dropped chip greyed-out inactive, not red

\* Implemented Point Buy: 27 points, scores 8–15, cost table in Model; pool shows remaining points; minus/plus per row; Presenter computes enabled states; View receives data only. Refactored for maintainability: single source for ability names in View; Point Buy display split into ShowPointBuyPool/HidePointBuyPool; domain logic removed from View.



\## Next Steps



\* Add validation for ability score ranges

\* Connect ability score changes to modifier calculations

\* Add background selection UI in alternative location (removed from tabs)



\## Pattern justification (required when adding a pattern)

Problem:

Why now:

Alternative considered:

Cost (files, complexity):

Payoff (testability, reuse, decoupling):

Decision:



