using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;
#endif

namespace TPP
{
    public class TPPInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;
        public bool dash;
        public bool crouch;
        public bool sprintToogleMode = true;

        [Header("Movement Settings")]
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputAction.CallbackContext  value)
        {
            MoveInput(value.ReadValue<Vector2>());
        }

        public void OnLook(InputAction.CallbackContext  value)
        {
            if (cursorInputForLook)
            {
                LookInput(value.ReadValue<Vector2>());
            }
        }

        public void OnJump(InputAction.CallbackContext  value)
        {
            JumpInput(value.performed);
        }

        public void OnSprint(InputAction.CallbackContext  value)
        {
            SprintInput(value.performed);
        }
#endif
        public void OnDash(InputAction.CallbackContext  value)
        {
     
            DashInput(value.performed);
        } 

        public void OnCrouch(InputAction.CallbackContext value)
        {
            CrouchInput(value.performed);   
        }
        

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void DashInput(bool newDashState)
        {
            dash = newDashState;
        }
        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            if (sprintToogleMode)
            {
                if (newSprintState)
                {
                    SetSprint(!sprint);
                }
            }
            else
            {
                SetSprint(newSprintState);
            }

        }

        public void SetSprint(bool newSprint) => sprint = newSprint;
        public void CrouchInput(bool newCrouchState)
        {
            crouch = newCrouchState;
        }

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