using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

#endif

namespace TPP
{
    public enum ToggleName
    {
        Sprint,
        Crouch,
        Jump,
        Shoot
    }
    [System.Serializable]
    public struct ToggleEntry
    {
        public ToggleName toggleName;
        public bool isEnabled;

        public ToggleEntry(ToggleName toggleName, bool isEnabled)
        {
            this.toggleName = toggleName;
            this.isEnabled = isEnabled;
        }
    }
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

        // This will show in Inspector
        public List<ToggleEntry> toggleSettings = new()
        {
            new(ToggleName.Sprint, false),
            new(ToggleName.Crouch, true)
        };

        Dictionary<ToggleName, (Func<bool> getter, Action<bool> setter)> stateHandlers;

#if ENABLE_INPUT_SYSTEM
        void Start()
        {
            InitializeHandlers();
        }

        void InitializeHandlers()
        {
            stateHandlers = new Dictionary<ToggleName, (Func<bool>, Action<bool>)>
            {
                { ToggleName.Sprint, (() => sprint, (val) => sprint = val) },
                { ToggleName.Crouch, (() => crouch, (val) => crouch = val) }
            };
        }

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

        public void SprintInput(bool newSprintState) => ProcessToggleableInput(ToggleName.Sprint, newSprintState);
        
        public void CrouchInput(bool newCrouchState) => ProcessToggleableInput(ToggleName.Crouch, newCrouchState);

        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }

        // Helper method to check a toggle
        public bool IsToggleEnabled(ToggleName name)
        {
            foreach (var entry in toggleSettings)
            {
                if (entry.toggleName == name)
                    return entry.isEnabled;
            }
            return false;
        }

        public void ProcessToggleableInput(ToggleName stateName, bool newInputState)
        {

            var (getter, setter) = stateHandlers[stateName];

            if (IsToggleEnabled(stateName))
            {
                if (newInputState)
                {
                    setter.Invoke(!getter()); // Toggle mode: flip state on key press
                }
            }
            else
            {
               setter(newInputState); // Hold mode: direct state mapping
            }
        }
    }

}