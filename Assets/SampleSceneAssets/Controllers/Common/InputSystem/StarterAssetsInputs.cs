using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool crouch;
        public bool interact;
        public bool aim;
        public bool canAim;
        public bool slide;
        public bool roll;
        [Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;
        private ThirdPersonController controller;

        private void Awake()
        {
            controller = GetComponent<ThirdPersonController>();
        }
        
        private void Update()
        {
            if (controller.InputLocked || !canAim)
            {
                aim = false;
                return;
            }

            aim = Mouse.current.rightButton.isPressed;
        }
        public void OnMove(InputValue value)
        {
            if (controller.InputLocked)
                return;

            MoveInput(value.Get<Vector2>());
        }
        /*  public void OnAim(InputValue value)
          {
              Debug.Log($"OnAim gọi: {value.isPressed}");

              aim = value.isPressed;
          }*/
        public void OnRoll(InputValue value)
        {
            roll = value.isPressed;
        }
        public void OnSlide(InputValue value)
        {
            if (controller.InputLocked)
                return;

            if (value.isPressed)
                slide = true;
        }
        public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
            if (controller.InputLocked)
                return;

            JumpInput(value.isPressed);
        }

		public void OnSprint(InputValue value)
		{
            if (controller.InputLocked)
                return;

            SprintInput(value.isPressed);
        }

		public void OnCrouch(InputValue value)
		{
            if (controller.InputLocked)
                return;

            if (value.isPressed)
                crouch = !crouch;
        }
        public void OnInteract(InputValue value)
        {
            if (controller.InputLocked)
                return;

            interact = value.isPressed;
        }

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

		public void CrouchInput(bool newCrouchState)
		{
			crouch = newCrouchState;
		}
		public void InteractInput(bool newInteractState)
		{
			interact = newInteractState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
        public void ClearInput()
        {
            interact = false;
            jump = false;
            sprint = false;
            crouch = false;
            slide = false;
            aim = false;
            roll = false;
        }
    }
	
}
