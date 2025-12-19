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
        ActionDefinition _active;
        float _lastUsedTime = -999f;

        // Use this for initialization
        void Start()
        {
            playerController = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();
            inputs = GetComponent<TPPInputs>();

            foreach (var action in _actions)
            {
                action.OnStart(this);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}