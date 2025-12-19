using UnityEngine;

namespace TPP.v1
{
    [CreateAssetMenu(fileName = "Action", menuName = "TPP/V1/Actions")]
    public abstract class Action : ScriptableObject
    {
        public string actionName;
        public float duration = 0.0f;

        [Header("Animation / VFX")]
        public string animName;
        public GameObject vfxPrefab;
        public AudioClip sfx;


        [Header("Rules")]
        public bool toggleMode = true;   // true = press to toggle, false = hold
        public bool disableSprint = true;
        public bool disableJump = false;

        public virtual bool CanUse() { return true; }

        public virtual void OnStart(ActionHub gameObject) { 
            
        }

        public virtual void TryUse()
        {
            if (!CanUse()) return;
            Use();
        }

        protected virtual void Use()
        {
        }

    }
}
