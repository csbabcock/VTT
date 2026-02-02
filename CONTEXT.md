This file is authoritative. Changes must be additive or explicitly marked.



# CONTEXT

## Relevant UI Objects

* CharacterCreationView
* ability-scores-grid
* roll-abilities-button
* tabs-container (Class, Race tabs)
* stats-panel
* detail-panel

## Relevant Scripts

* Assets/UI/MainMenu/Views/CharacterCreationView.cs
* Assets/UI/MainMenu/USS/CharacterCreationView.uss
* Assets/UI/MainMenu/UXML/CharacterCreationView.uxml

## Relevant Prefabs

* (none identified)

## Current Unity Scene(s)

* (not identified in session)

## Current state

* Character creation uses MVP: View is passive (display + events only); Presenter holds drag-and-drop and assignment logic.
* Drag-and-drop for ability scores: rolled scores draggable, ability rows are drop zones; drag-from-ability and swap/replace supported.
* DragAndDropHandler (MainMenu/Services) handles element detection and drop-zone visual feedback; DragState holds drag state; Presenter owns state and calls View UI methods (e.g. ShowDragPreview, HighlightDropZone).
* Stats-panel: rolled-scores-pool (rolled-scores-container), ability-scores-grid (Labels in drop zones), roll-abilities-button. Ability score display is Labels in drop zones, not IntegerFields.
* Relevant scripts include MainMenu Presenters, Views, Models, and Services (e.g. CharacterCreationDataService, DragAndDropHandler).
