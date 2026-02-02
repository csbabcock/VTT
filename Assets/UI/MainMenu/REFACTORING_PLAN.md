# Character Creation View Refactoring Plan

## Current Issues (SOLID/MVP Violations)

1. **View contains business logic** - HandleDropOnAbility, ReturnDraggedScoreToPool contain assignment logic
2. **View manages drag state** - Should be in Presenter
3. **View has complex detection logic** - OnGlobalPointerUp, FindAncestorByClass should be in service
4. **View tracks model state** - _currentAssignedRolledScoreIndices should come from Model
5. **Single Responsibility violated** - View does UI + logic + state management

## Refactoring Goals

### View Responsibilities (MVP - Passive View)
- ✅ Display UI elements
- ✅ Raise events on user input
- ✅ Update UI based on Presenter commands
- ❌ NO business logic
- ❌ NO state management
- ❌ NO drag detection logic

### Presenter Responsibilities
- ✅ Handle all business logic
- ✅ Manage drag state
- ✅ Coordinate between Model and View
- ✅ Use DragAndDropHandler for detection
- ✅ Call View methods to update UI

### Service Responsibilities (DragAndDropHandler)
- ✅ Element detection (FindAbilityRow, FindDropZone)
- ✅ Visual feedback helpers
- ✅ Utility methods

## Implementation Steps

1. ✅ Create DragAndDropHandler service
2. ✅ Create DragState struct
3. ⏳ Refactor View to remove business logic
4. ⏳ Add public UI update methods to View
5. ⏳ Simplify View events
6. ⏳ Move drag state to Presenter
7. ⏳ Move business logic to Presenter
8. ⏳ Update Presenter to use DragAndDropHandler

## New View API (Public Methods for Presenter)

```csharp
// UI Update Methods (called by Presenter)
public void ShowDragPreview(int scoreValue)
public void UpdateDragPreviewPosition(Vector2 position)
public void HideDragPreview()
public void HighlightDropZone(int abilityIndex)
public void ClearDropZoneHighlights()
public void MarkElementAsDragging(VisualElement element)
public void UnmarkElementAsDragging(VisualElement element)
```

## New View Events (Simplified)

```csharp
// Simple events - View just notifies Presenter
public event Action<int, int> DragStartedFromRolledScore; // index, value
public event Action<int> DragStartedFromAbility; // abilityIndex
public event Action<Vector2> DropOccurred; // position
```

## Presenter Responsibilities

- Subscribe to View events
- Use DragAndDropHandler to detect drop targets
- Manage DragState
- Handle assignment/swapping logic
- Call View methods to update UI
- Update Model
