using UnityEngine;

namespace TPP.v1
{
    [CreateAssetMenu(fileName = "CrouchAction", menuName = "TPP/V1/CrouchAction")]
    public class CrouchAction : Action
    {
        [Header("Crouch dimensions")]
        public float crouchHeight = 1f;
        public float transitionSpeed = 10f;

        [Header("Movement")]
        [Range(0f, 1f)]
        public float speedMultiplier = 0.55f;

        float _originalHeight;
        Vector3 _originalCenter = Vector3.zero;
        public override void OnStart(ActionHub gameObject)
        {
            base.OnStart(gameObject);
            _originalHeight = gameObject.characterController.height;
            
        }
        public override bool CanUse()
        {
            return base.CanUse();
        }

        public override void TryUse()
        {
            base.TryUse();
        }

        protected override void Use()
        {
            base.Use();
        }
    }
}