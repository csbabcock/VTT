using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameCore
{
	[RequireComponent(typeof(PlayerInput))]
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

#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
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
			_playerInput = GetComponent<PlayerInput>();
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
				Debug.Log("TogglePerspective action found and subscribed!");
			}
			else
			{
				Debug.LogError("TogglePerspective action not found in Player action map!");
			}
		}

		private void OnEnable()
		{
			// Enable the action map
			_playerActionMap?.Enable();
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

		private void Update()
		{
			// Read continuous input values (for analog movement and look)
			if (_moveAction != null)
			{
				move = _moveAction.ReadValue<Vector2>();
			}

			if (_lookAction != null && cursorInputForLook)
			{
				look = _lookAction.ReadValue<Vector2>();
			}

			// Fallback: Check if toggle perspective button was pressed this frame
			if (_togglePerspectiveAction != null && _togglePerspectiveAction.WasPressedThisFrame())
			{
				Debug.Log("TogglePerspective detected via WasPressedThisFrame!");
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
			Debug.Log("TogglePerspective action triggered!");
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