using System;
using UnityEngine;

namespace TPP
{
    [RequireComponent(typeof(PlayerController))]    
    public abstract class ActionComponent : MonoBehaviour
    {
        protected PlayerController Player;
        protected TPPInputs TPPInputs;
        protected bool Active = false;
        protected float LastUsedTime = -999f;

        public virtual void Awake()
        {
            Player = GetComponent<PlayerController>();
            TPPInputs = GetComponent<TPPInputs>();  
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

