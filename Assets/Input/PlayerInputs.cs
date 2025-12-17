using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using PlayerInputComponent = UnityEngine.InputSystem.PlayerInput;
#endif

namespace GameCore
{
	[RequireComponent(typeof(PlayerInputComponent))]
	public class PlayerInputs : MonoBehaviour
	{
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
		private bool _cursorInputForLookOriginal;

#if ENABLE_INPUT_SYSTEM
		private PlayerInputComponent _playerInput;
		private InputActionMap _playerActionMap;
		private InputAction _moveAction;
		private InputAction _lookAction;
		private InputAction _jumpAction;
		private InputAction _sprintAction;
		private InputAction _togglePerspectiveAction;

		// Events for action callbacks
		public System.Action OnTogglePerspective;

		private void Awake()
		{
			_cursorInputForLookOriginal = cursorInputForLook;
			
			_playerInput = GetComponent<PlayerInputComponent>();
			if (_playerInput == null)
			{
				Debug.LogError("PlayerInput component is missing! Please add the PlayerInput component to the same GameObject.");
				return;
			}

			// Get the Player action map
			_playerActionMap = _playerInput.actions.FindActionMap("Player");
			if (_playerActionMap == null)
			{
				Debug.LogError("Player action map not found in Input Actions!");
				return;
			}

			// Get individual actions
			_moveAction = _playerActionMap.FindAction("Move");
			_lookAction = _playerActionMap.FindAction("Look");
			_jumpAction = _playerActionMap.FindAction("Jump");
			_sprintAction = _playerActionMap.FindAction("Sprint");
			_togglePerspectiveAction = _playerActionMap.FindAction("TogglePerspective");

			// Subscribe to action events
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
		}

		private void Start()
		{
			// When using project-wide actions asset, we need to manually ensure
			// the Player action map is enabled (as per Unity's warning)
			if (_playerActionMap != null && !_playerActionMap.enabled)
			{
				_playerActionMap.Enable();
			}
			
			// Ensure all actions are enabled
			_moveAction?.Enable();
			_lookAction?.Enable();
			_jumpAction?.Enable();
			_sprintAction?.Enable();
			
			// Ensure cursor is locked and input is enabled
			SetCursorState(cursorLocked);
			// Always set to true for camera control when starting
			cursorInputForLook = true;
		}

		private void OnEnable()
		{
			// Enable the action map and ensure all actions are enabled
			if (_playerActionMap != null)
			{
				_playerActionMap.Enable();
				// Explicitly enable all actions to ensure they work
				_moveAction?.Enable();
				_lookAction?.Enable();
				_jumpAction?.Enable();
				_sprintAction?.Enable();
			}
			
			// Ensure cursor input for look is enabled
			// Always set to true for camera control when component is enabled
			cursorInputForLook = true;
		}

		private void OnDisable()
		{
			// Disable the action map
			_playerActionMap?.Disable();
		}

		private void OnDestroy()
		{
			// Unsubscribe from action events
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
		}

		private bool _inputEnabled = true;

		private void Update()
		{
			if (!_inputEnabled)
				return;

			ReadContinuousInput();
		}

		/// <summary>
		/// Reads continuous input values for movement and look.
		/// Separated for clarity and potential optimization.
		/// </summary>
		private void ReadContinuousInput()
		{
			if (_moveAction != null && _moveAction.enabled)
			{
				move = _moveAction.ReadValue<Vector2>();
			}

			if (_lookAction != null && _lookAction.enabled && cursorInputForLook)
			{
				look = _lookAction.ReadValue<Vector2>();
			}
			else
			{
				// Clear look input when cursor input is disabled
				look = Vector2.zero;
			}

			// Check if toggle perspective button was pressed this frame
			if (_togglePerspectiveAction != null && _togglePerspectiveAction.WasPressedThisFrame())
			{
				OnTogglePerspective?.Invoke();
			}
		}

		private void OnMovePerformed(InputAction.CallbackContext context)
		{
			move = context.ReadValue<Vector2>();
		}

		private void OnLookPerformed(InputAction.CallbackContext context)
		{
			if (cursorInputForLook)
			{
				look = context.ReadValue<Vector2>();
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

		// Public methods for external input (mobile UI, etc.)
		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

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
			if (_playerActionMap != null)
			{
				if (!_playerActionMap.enabled)
				{
					_playerActionMap.Enable();
				}
			}
			
			// Then enable individual actions (they should already be enabled via the map, but ensure it)
			_moveAction?.Enable();
			_lookAction?.Enable();
			_jumpAction?.Enable();
			_sprintAction?.Enable();
			
			// Restore cursor input for look - this is critical for mouse camera control
			// Always set to true when enabling player actions (camera control should be active)
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
#endif

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}