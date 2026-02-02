This file is authoritative. Changes must be additive or explicitly marked.



\# CONTEXT



\## Relevant UI Objects



\* CharacterCreationView

\* ability-scores-grid

\* roll-abilities-button

\* tabs-container (Class, Race tabs)

\* stats-panel

\* detail-panel



\## Relevant Scripts



\* Assets/UI/MainMenu/Views/CharacterCreationView.cs

\* Assets/UI/MainMenu/USS/CharacterCreationView.uss

\* Assets/UI/MainMenu/UXML/CharacterCreationView.uxml



\## Relevant Prefabs



\* (none identified)



\## Current Unity Scene(s)



\* (not identified in session)



\## Current state



\* Character creation uses MVP: View is passive (display + events only); Presenter holds drag-and-drop, assignment, and validation logic; Model holds state (including RolledDroppedIndices for dice breakdown).

\* Ability score options: Roll (4d6 drop lowest), Standard Array, Manual (six editable fields with drag handle), Point Buy (27 points, scores 8–15, cost table 8=0 through 15=9). Roll shows dice breakdown chips under each score; kept dice chips use default (primary) text color; dropped (lowest) die chip is greyed-out inactive (muted grey + opacity), not red.

\* Point Buy: pool shows remaining points; each ability row has minus (left of score) and plus (right of score); Presenter computes points remaining and +/- enabled states and calls View.UpdatePointBuyPointsRemaining and View.UpdatePointBuyButtonStates; Model has PointBuyCostTable and SetPointBuyAbilityScore; View has no Point Buy domain logic.

\* IAbilityScoreRoller (MainMenu/Services) used by Presenter for rolling; rolled-score pool UI lives in CharacterCreationView.RolledScores.cs partial. Drag-and-drop: rolled scores draggable, ability rows drop zones; drag-from-ability and swap/replace supported.

\* DragAndDropHandler (MainMenu/Services) handles element detection and drop-zone visual feedback; DragState holds drag state; Presenter owns state and calls View UI methods (e.g. ShowDragPreview, HighlightDropZone).

\* Stats-panel: rolled-scores-pool (rolled-scores-container or Point Buy points label), ability-scores-grid (Labels in drop zones; Point Buy shows +/- per row), roll / Standard Array / Manual / Point Buy buttons. Ability score display is Labels in drop zones, not IntegerFields.

\* View uses single source for ability names (AbilityNamesShort, AbilityNamesDisplay). Relevant scripts include MainMenu Presenters, Views, Models, and Services (CharacterCreationDataService, DragAndDropHandler, IAbilityScoreRoller, AbilityScoreRoller; CharacterCreationView.cs + CharacterCreationView.RolledScores.cs).



\## Engineering guardrails



\### Goals

\* Maintainable code that stays easy to change.

\* Consistent architecture for UI and gameplay code.

\* Avoid pattern theater. Patterns are tools, not requirements.



\### Default approach

\* Start simple, refactor when a second use or clear pain appears.

\* Prefer small modules and clear seams over deep hierarchies.

\* Optimize for readability and testability, not cleverness.



\### SOLID guidance (pragmatic)

\* Single Responsibility: one reason to change. If a class keeps growing, split by responsibility.

\* Open/Closed: extend with new components; avoid repeatedly editing core logic.

\* Liskov: avoid inheritance unless subclasses are truly substitutable.

\* Interface Segregation: small focused interfaces; no god interfaces for convenience.

\* Dependency Inversion: depend on abstractions at boundaries (UI, persistence, networking). Do not inject everything everywhere.



\### UI architecture

\* MVP for screens with non trivial logic or multiple states.

\* Small UI widgets can stay simple (View only) until logic grows.



MVP rules of thumb:

\* View: dumb render + forward events; no domain logic.

\* Presenter: owns UI logic and state transitions; calls services; updates the view.

\* Model: domain data and services, independent of UI concerns.



\### When NOT to apply patterns

\* One off feature with no expected reuse.

\* Pattern adds more files than the feature adds value.

\* Indirection without a clear testing or decoupling benefit.



\### When to introduce a pattern

\* Same logic appears twice.

\* A class has 3+ responsibilities.

\* Changes keep touching too many files.

\* UI logic is getting tangled with rendering code.



