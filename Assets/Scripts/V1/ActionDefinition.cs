using UnityEngine;

namespace TPP.v1
{
    [SerializeField]
    public enum ActionNames
    {
        Sprint,
        Crouch,
        Slide,
        Dash,
        Vault,
        WallRun
    }
    public abstract class ActionDefinition : ScriptableObject
    {
        public ActionNames actionName;
        public float duration = 0.0f;
        public int priority;

        [Header("Animation / VFX")]
        public string animName;
        public GameObject vfxPrefab;
        public AudioClip sfx;


        [Header("Rules")]
        public bool disableSprint = true;
        public bool disableJump = false;
        public bool blocksMovement = false;

        protected ActionHub hub;
        protected PlayerController playerController;
        protected CharacterController characterController;
        protected TPPInputs inputs;
        public virtual bool CanStart() { return true; }

        public virtual void Register(ActionHub actionHub) {

            if (!actionHub)
            {
                Debug.Log("ActionHub Not Defined");
            }

            hub = actionHub;
            playerController = hub.GetComponent<PlayerController>();
            characterController = hub.GetComponent<CharacterController>();
            inputs = hub.GetComponent<TPPInputs>();
        }
        public bool TryStart()
        {
            return hub.TryStart(this);
        }

        internal void StartInternal()
        {
            OnStart();
        }

        internal void StopInternal()
        {
            OnStop();
        }

        internal void InterruptInternal()
        {
            OnInterrupt();
        }
        protected abstract void OnStart();
        protected abstract void OnStop();
        protected virtual void OnInterrupt() => OnStop();


    }
}
