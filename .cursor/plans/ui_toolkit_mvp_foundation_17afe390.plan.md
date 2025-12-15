---
name: UI Toolkit MVP Foundation
overview: Set up UI Toolkit with MVP architecture following Unity best practices, create a main menu scene with scene loading, and establish a foundation for diegetic in-game UI.
todos:
  - id: setup_packages
    content: Verify and add UI Toolkit packages (UI Builder if needed) to manifest.json
    status: completed
  - id: create_folder_structure
    content: Create organized UI folder structure (MainMenu, InGame, Shared, Scripts/Core)
    status: completed
  - id: create_core_interfaces
    content: Create core MVP interfaces (IUIModel, IUIView, IUIPresenter) following SOLID principles
    status: completed
  - id: create_scene_loader
    content: Implement SceneLoader service for scene management and transitions
    status: completed
  - id: implement_main_menu_model
    content: Create MainMenuModel with state management and events
    status: completed
  - id: create_main_menu_uxml
    content: Design MainMenuView.uxml with basic menu structure (extensible for future buttons)
    status: completed
  - id: create_main_menu_uss
    content: Create MainMenuView.uss with diegetic-inspired styling
    status: completed
  - id: implement_main_menu_view
    content: Create MainMenuView.cs component that binds to UI Toolkit and exposes events
    status: completed
  - id: implement_main_menu_presenter
    content: Create MainMenuPresenter that connects model, view, and handles scene loading
    status: completed
  - id: create_ingame_ui_foundation
    content: Create minimal InGame UI foundation (Model, View, Presenter) for future expansion
    status: completed
  - id: create_shared_styles
    content: Create DiegeticTheme.uss with base styling system for consistent UI
    status: completed
  - id: create_main_menu_scene
    content: Create MainMenu.unity scene with UI Document setup and configure Build Settings
    status: completed
---

# UI Toolkit MVP Foundation Setup

## Overview

Implement UI Toolkit with Model-View-Presenter (MVP) architecture following Unity's best practices. Create a main menu scene that loads the level scene, and establish a foundation for diegetic in-game UI.

## Architecture

The implementation will follow the MVP pattern as described in Unity's design patterns guide:

```
Model (Data) → Presenter (Logic) → View (UI Toolkit)
```

- **Model**: Stores UI state and data (e.g., menu state, player stats)
- **View**: UI Toolkit UXML/USS files and UI Document components
- **Presenter**: Handles logic, updates model, and refreshes view via events

## Implementation Steps

### 1. Package Setup

- Verify UI Toolkit packages are installed (already present: `com.unity.modules.uielements`)
- Add UI Builder package if needed for visual editing
- Update `Packages/manifest.json` if additional packages required

### 2. Project Structure

Create organized folder structure:

```
Assets/
  UI/
    MainMenu/
      Models/
      Views/
      Presenters/
      UXML/
      USS/
    InGame/
      Models/
      Views/
      Presenters/
      UXML/
      USS/
    Shared/
      Styles/
      Components/
    Scripts/
      Core/
 - IUIModel.cs (interface)
 - IUIView.cs (interface)
 - IUIPresenter.cs (interface)
 - SceneLoader.cs (scene management)
```

### 3. Core MVP Interfaces

Create base interfaces following SOLID principles:

- `IUIModel<T>`: Generic model interface for UI data
- `IUIView`: Base view interface for UI Toolkit integration
- `IUIPresenter<TModel, TView>`: Generic presenter interface

### 4. Main Menu Implementation

#### 4.1 Main Menu Model

- `MainMenuModel.cs`: Stores menu state (selected button, menu visibility, etc.)
- Events: `MenuStateChanged`

#### 4.2 Main Menu View

- `MainMenuView.uxml`: UI structure (buttons, layout)
- `MainMenuView.uss`: Styling (diegetic-inspired design)
- `MainMenuView.cs`: UI Toolkit view component that implements `IUIView`
- Handles UI Toolkit callbacks and exposes events to presenter

#### 4.3 Main Menu Presenter

- `MainMenuPresenter.cs`: Implements `IUIPresenter<MainMenuModel, MainMenuView>`
- Handles button clicks
- Manages scene loading via `SceneLoader`
- Updates model and refreshes view

### 5. Scene Loading System

- `SceneLoader.cs`: Singleton or service for scene management
- Methods: `LoadScene(string sceneName)`, `LoadSceneAsync(string sceneName)`
- Handles loading states and transitions
- Integrates with Unity's SceneManager

### 6. In-Game UI Foundation

Minimal MVP foundation for diegetic UI:

- `InGameUIModel.cs`: Base model for in-game UI state
- `InGameUIView.cs`: Base view component
- `InGameUIPresenter.cs`: Base presenter
- Structure ready for future elements (health, stamina, inventory, etc.)

### 7. Main Menu Scene Setup

- Create `Assets/Scenes/MainMenu.unity`
- Add UI Document GameObject
- Configure UI Document to use MainMenuView.uxml
- Attach MainMenuPresenter component
- Set up scene in Build Settings

### 8. Diegetic UI Styling Foundation

- Create base USS theme with diegetic-inspired styles
- Design system for consistent UI elements
- Styles that can be extended for in-game UI elements

## Key Files to Create

1. **Core Interfaces**:

                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/Scripts/Core/IUIModel.cs`
                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/Scripts/Core/IUIView.cs`
                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/Scripts/Core/IUIPresenter.cs`

2. **Scene Management**:

                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/Scripts/Core/SceneLoader.cs`

3. **Main Menu**:

                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/MainMenu/Models/MainMenuModel.cs`
                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/MainMenu/Presenters/MainMenuPresenter.cs`
                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/MainMenu/Views/MainMenuView.cs`
                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/MainMenu/UXML/MainMenuView.uxml`
                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/MainMenu/USS/MainMenuView.uss`

4. **In-Game UI Foundation**:

                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/InGame/Models/InGameUIModel.cs`
                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/InGame/Presenters/InGameUIPresenter.cs`
                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/InGame/Views/InGameUIView.cs`

5. **Shared Styles**:

                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/UI/Shared/Styles/DiegeticTheme.uss`

6. **Scene**:

                                                                                                                                                                                                                                                                                                                                                                                                - `Assets/Scenes/MainMenu.unity`

## Design Considerations

- **Diegetic UI Style**: Inspired by "The Little Devil Inside" - UI elements appear as part of the game world
- **Event-Driven**: Use C# events for Model → Presenter → View communication
- **Extensible**: Foundation allows easy addition of new UI elements
- **Consistent**: Shared styling system ensures visual consistency
- **Testable**: MVP separation allows unit testing of logic without UI

## Integration Points

- Scene loading will use Unity's `SceneManager`
- UI Toolkit integration via `UIDocument` component
- Input System integration for menu navigation (already configured)
- Follows existing project's SOLID principles (similar to PlayerController architecture)