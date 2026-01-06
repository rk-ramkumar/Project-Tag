using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPP.v1
{ 
    
    [RequireComponent(typeof(CharacterController), typeof(TPPInputs), typeof(PlayerController))]
    public class ActionHub : MonoBehaviour
    {
        public PlayerController playerController;
        public CharacterController characterController;
        public TPPInputs inputs;
        [SerializeField] List<ActionDefinition> _actions = new();
        //ActionDefinition _active;
        //float _lastUsedTime = -999f;
        ActionDefinition currentAction;
        static int count = 0;

        // Use this for initialization
        void Start()
        {
            playerController = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();
            inputs = GetComponent<TPPInputs>();

            foreach (var action in _actions)
            {
                action.Register(this);
            }
        }

        void OnEnable()
        {
            Debug.Log($"Hub OnEnable: {count}");
            count++;
         
        }

        void OnDisable()
        {
            Debug.Log($"Hub OnDisable: {count}");
            count--;
            
        }

        public bool TryStart(ActionDefinition action)
        {
            if (!action.CanStart())
                return false;

            // No action running → start immediately
            if (currentAction == null)
            {
                StartAction(action);
                return true;
            }

            // Same action already active
            if (currentAction == action)
                return false;

            // Priority check
            if (action.priority > currentAction.priority)
            {
                // Can we interrupt the current action?
                //if (!currentAction.interruptible)
                //    return false;

                InterruptCurrent();
                StartAction(action);
                return true;
            }

            // Lower or equal priority → denied
            return false;
        }

        private void StartAction(ActionDefinition action)
        {
            currentAction = action;
            action.StartInternal();
        }

        private void InterruptCurrent()
        {
            if (currentAction == null) return;

            var old = currentAction;
            currentAction = null;
            old.InterruptInternal();
        }

        public void Stop(ActionDefinition action)
        {
            if (currentAction != action) return;

            currentAction = null;
            action.StopInternal();
        }


    }
}