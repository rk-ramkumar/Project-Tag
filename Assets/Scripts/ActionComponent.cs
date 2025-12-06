using System;
using UnityEngine;

namespace TPP
{
    [RequireComponent(typeof(PlayerController), typeof(CharacterController))]

    public abstract class ActionComponent : MonoBehaviour
    {
        protected PlayerController player;
        protected TPPInputs input;
        protected CharacterController cc;
        protected bool Active = false;
        protected float LastUsedTime = -999f;


        public virtual void Awake()
        {
            player = GetComponent<PlayerController>();
            input = GetComponent<TPPInputs>();
            cc = GetComponent<CharacterController>();

        }

        public virtual bool CanUse() { return true; }

        public virtual void TryUse()
        {
            if (!CanUse()) return;
            Use();
        }

        // Use should be overridden to start the action
        protected virtual void Use()
        {
            LastUsedTime = Time.time;
            Active = true;
        }

        // Stops action and resets
        public virtual void StopAction()
        {
            Active = false;
        }

        public bool IsActive() => Active;

    }
}

