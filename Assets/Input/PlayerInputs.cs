using UnityEngine;
using GameCore.EncounterMode;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using PlayerInputComponent = UnityEngine.InputSystem.PlayerInput;
#endif

namespace GameCore
{
	/// <summary>
	/// Handles player input using Unity's Input System.
	/// Provides a clean interface for reading player input values and managing input state.
	/// </summary>
	[RequireComponent(typeof(PlayerInputComponent))]
	public class PlayerInputs : MonoBehaviour
	{
		#region Constants
		private const string PLAYER_ACTION_MAP_NAME = "Player";
		private const string ACTION_MOVE = "Move";
		private const string ACTION_LOOK = "Look";
		private const string ACTION_JUMP = "Jump";
		private const string ACTION_SPRINT = "Sprint";
		private const string ACTION_TOGGLE_PERSPECTIVE = "TogglePerspective";
		private const string ACTION_TOGGLE_ENCOUNTER_MODE = "ToggleEncounterMode";
		#endregion

		#region Public Input Values
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;
		#endregion

		private EncounterModeManager _encounterModeManager;
		private bool? _lastLoggedShouldReadLookInput;

#if ENABLE_INPUT_SYSTEM
		#region Private Fields
		private PlayerInputComponent _playerInput;
		private InputActionMap _playerActionMap;
		private InputAction _moveAction;
		private InputAction _lookAction;
		private InputAction _jumpAction;
		private InputAction _sprintAction;
		private InputAction _togglePerspectiveAction;
		private InputAction _toggleEncounterModeAction;
		private bool _inputEnabled = true;
		#endregion

		#region Events
		/// <summary>
		/// Raised when the perspective toggle action is performed.
		/// </summary>
		public System.Action OnTogglePerspective;

		/// <summary>
		/// Raised when the encounter mode toggle action is performed.
		/// </summary>
		public System.Action OnToggleEncounterMode;
		#endregion

		#region Unity Lifecycle
		private void Awake()
		{
			InitializeInputSystem();
		}

		private void Start()
		{
			EnableInputSystem();
			SetCursorState(cursorLocked);
			cursorInputForLook = true;
		}

		private void OnEnable()
		{
			EnableInputSystem();
			cursorInputForLook = true;
		}

		private void OnDisable()
		{
			_playerActionMap?.Disable();
		}

		private void OnDestroy()
		{
			UnsubscribeFromActions();
		}

		private void Update()
		{
			if (!_inputEnabled)
				return;

			ReadContinuousInput();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}
		#endregion

		#region Input System Initialization
		/// <summary>
		/// Initializes the Input System components and finds all required actions.
		/// </summary>
		private void InitializeInputSystem()
		{
			_playerInput = GetComponent<PlayerInputComponent>();
			if (_playerInput == null)
			{
				Debug.LogError("PlayerInput component is missing! Please add the PlayerInput component to the same GameObject.");
				return;
			}

			_playerActionMap = _playerInput.actions.FindActionMap(PLAYER_ACTION_MAP_NAME);
			if (_playerActionMap == null)
			{
				Debug.LogError($"Player action map '{PLAYER_ACTION_MAP_NAME}' not found in Input Actions!");
				return;
			}

			// Get individual actions
			_moveAction = _playerActionMap.FindAction(ACTION_MOVE);
			_lookAction = _playerActionMap.FindAction(ACTION_LOOK);
			_jumpAction = _playerActionMap.FindAction(ACTION_JUMP);
			_sprintAction = _playerActionMap.FindAction(ACTION_SPRINT);
			_togglePerspectiveAction = _playerActionMap.FindAction(ACTION_TOGGLE_PERSPECTIVE);
			_toggleEncounterModeAction = _playerActionMap.FindAction(ACTION_TOGGLE_ENCOUNTER_MODE);

			SubscribeToActions();
		}

		/// <summary>
		/// Subscribes to input action events.
		/// </summary>
		private void SubscribeToActions()
		{
			if (_moveAction != null)
				_moveAction.performed += OnMovePerformed;
			
			if (_lookAction != null)
				_lookAction.performed += OnLookPerformed;
			
			if (_jumpAction != null)
			{
				_jumpAction.performed += OnJumpPerformed;
				_jumpAction.canceled += OnJumpCanceled;
			}
			
			if (_sprintAction != null)
			{
				_sprintAction.performed += OnSprintPerformed;
				_sprintAction.canceled += OnSprintCanceled;
			}

			if (_togglePerspectiveAction != null)
			{
				_togglePerspectiveAction.performed += OnTogglePerspectivePerformed;
				_togglePerspectiveAction.Enable();
			}

			if (_toggleEncounterModeAction != null)
			{
				_toggleEncounterModeAction.performed += OnToggleEncounterModePerformed;
				_toggleEncounterModeAction.Enable();
			}
		}

		/// <summary>
		/// Unsubscribes from input action events.
		/// </summary>
		private void UnsubscribeFromActions()
		{
			if (_moveAction != null)
				_moveAction.performed -= OnMovePerformed;
			
			if (_lookAction != null)
				_lookAction.performed -= OnLookPerformed;
			
			if (_jumpAction != null)
			{
				_jumpAction.performed -= OnJumpPerformed;
				_jumpAction.canceled -= OnJumpCanceled;
			}
			
			if (_sprintAction != null)
			{
				_sprintAction.performed -= OnSprintPerformed;
				_sprintAction.canceled -= OnSprintCanceled;
			}

			if (_togglePerspectiveAction != null)
				_togglePerspectiveAction.performed -= OnTogglePerspectivePerformed;

			if (_toggleEncounterModeAction != null)
				_toggleEncounterModeAction.performed -= OnToggleEncounterModePerformed;
		}

		/// <summary>
		/// Enables the Input System action map and all actions.
		/// </summary>
		private void EnableInputSystem()
		{
			if (_playerActionMap != null && !_playerActionMap.enabled)
			{
				_playerActionMap.Enable();
			}
			
			_moveAction?.Enable();
			_lookAction?.Enable();
			_jumpAction?.Enable();
			_sprintAction?.Enable();
		}
		#endregion

		#region Input Reading
		/// <summary>
		/// Reads continuous input values for movement and look.
		/// </summary>
		private void ReadContinuousInput()
		{
			if (_moveAction != null && _moveAction.enabled)
			{
				move = _moveAction.ReadValue<Vector2>();
			}

			if (_lookAction != null && _lookAction.enabled && ShouldReadLookInput())
			{
				look = _lookAction.ReadValue<Vector2>();
			}
			else
			{
				// Clear look input when cursor input is disabled
				look = Vector2.zero;
			}

			// Read sprint state continuously (PassThrough actions don't reliably fire canceled events)
			if (_sprintAction != null && _sprintAction.enabled)
			{
				// Read the current value of the sprint action (returns > 0.5f when pressed for button-like controls)
				float sprintValue = _sprintAction.ReadValue<float>();
				sprint = sprintValue > 0.5f;
			}
			else
			{
				sprint = false;
			}

		}
		#endregion

		#region Action Event Handlers
		private void OnMovePerformed(InputAction.CallbackContext context)
		{
			move = context.ReadValue<Vector2>();
		}

		private void OnLookPerformed(InputAction.CallbackContext context)
		{
			if (ShouldReadLookInput())
			{
				look = context.ReadValue<Vector2>();
			}
			else
			{
				look = Vector2.zero;
			}
		}

		private void OnJumpPerformed(InputAction.CallbackContext context)
		{
			jump = true;
		}

		private void OnJumpCanceled(InputAction.CallbackContext context)
		{
			jump = false;
		}

		private void OnSprintPerformed(InputAction.CallbackContext context)
		{
			sprint = true;
		}

		private void OnSprintCanceled(InputAction.CallbackContext context)
		{
			sprint = false;
		}

		private void OnTogglePerspectivePerformed(InputAction.CallbackContext context)
		{
			OnTogglePerspective?.Invoke();
		}

		private void OnToggleEncounterModePerformed(InputAction.CallbackContext context)
		{
			OnToggleEncounterMode?.Invoke();
		}
		#endregion

		#region Public Input Methods
		/// <summary>
		/// Sets the move input value. Useful for external input sources (e.g., mobile UI).
		/// </summary>
		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		/// <summary>
		/// Sets the look input value. Useful for external input sources (e.g., mobile UI).
		/// </summary>
		public void LookInput(Vector2 newLookDirection)
		{
			if (!ShouldReadLookInput())
			{
				look = Vector2.zero;
				return;
			}

			look = newLookDirection;
		}

		/// <summary>
		/// Sets the jump input state. Useful for external input sources (e.g., mobile UI).
		/// </summary>
		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		/// <summary>
		/// Sets the sprint input state. Useful for external input sources (e.g., mobile UI).
		/// </summary>
		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
		#endregion

		#region Input State Management
		/// <summary>
		/// Enable or disable player input. Useful for pausing input during UI interactions.
		/// Note: We keep PlayerInput enabled but disable only movement actions to preserve
		/// Input System functionality that UI Toolkit's InputSystemUIInputModule needs.
		/// </summary>
		public void SetInputEnabled(bool enabled)
		{
			_inputEnabled = enabled;

			if (enabled)
			{
				EnablePlayerActions();
			}
			else
			{
				DisablePlayerActions();
			}
		}

		/// <summary>
		/// Enables all player movement actions and restores cursor settings.
		/// </summary>
		private void EnablePlayerActions()
		{
			// Ensure PlayerInput component is enabled first
			if (_playerInput != null && !_playerInput.enabled)
			{
				_playerInput.enabled = true;
			}
			
			// Enable the entire action map first to ensure all actions are available
			if (_playerActionMap != null && !_playerActionMap.enabled)
			{
				_playerActionMap.Enable();
			}
			
			// Then enable individual actions (they should already be enabled via the map, but ensure it)
			_moveAction?.Enable();
			_lookAction?.Enable();
			_jumpAction?.Enable();
			_sprintAction?.Enable();
			
			// Restore cursor input for look - this is critical for mouse camera control
			cursorInputForLook = true;
		}

		/// <summary>
		/// Disables player movement actions while keeping PlayerInput enabled for UI Toolkit.
		/// Clears all input values to prevent residual movement.
		/// </summary>
		private void DisablePlayerActions()
		{
			_moveAction?.Disable();
			_lookAction?.Disable();
			_jumpAction?.Disable();
			_sprintAction?.Disable();
			
			cursorInputForLook = false;
			
			// Clear current input values when disabled
			move = Vector2.zero;
			look = Vector2.zero;
			jump = false;
			sprint = false;
		}
		#endregion
#endif

		private bool ShouldReadLookInput()
		{
			bool shouldRead = cursorInputForLook && !IsEncounterModeActive();
			LogLookInputState(shouldRead);
			return shouldRead;
		}

		private bool IsEncounterModeActive()
		{
			if (_encounterModeManager == null)
			{
				_encounterModeManager = FindAnyObjectByType<EncounterModeManager>();
			}

			return _encounterModeManager != null && _encounterModeManager.IsEncounterModeActive;
		}

		private void LogLookInputState(bool shouldRead)
		{
			if (_lastLoggedShouldReadLookInput.HasValue && _lastLoggedShouldReadLookInput.Value == shouldRead)
				return;

			_lastLoggedShouldReadLookInput = shouldRead;
			Debug.Log(
				$"[EncounterCameraDebug] PlayerInputs shouldReadLook={shouldRead}, " +
				$"cursorInputForLook={cursorInputForLook}, encounterActive={IsEncounterModeActive()}, " +
				$"look={look}, playerInputs={name}");
		}

		#region Cursor Management
		/// <summary>
		/// Sets the cursor lock state.
		/// </summary>
		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
		#endregion
	}
	
}