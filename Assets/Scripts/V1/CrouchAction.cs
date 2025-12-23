using DG.Tweening;
using UnityEngine;

namespace TPP.v1
{
    [CreateAssetMenu(fileName = "CrouchAction", menuName = "TPP/V1/CrouchAction")]
    public class CrouchAction : ActionDefinition
    {
        [Header("Crouch dimensions")]
        public float crouchHeight = 1f;
        public float transitionSpeed = 10f;

        [Header("Movement")]
        [Range(0f, 1f)]
        public float speedMultiplier = 0.55f;

        float _originalHeight;
        Vector3 _originalCenter = Vector3.zero;
        bool _isCrouched;
        float _currentHeight;
        Tween heightTween;

        public override void Register(ActionHub gameObject)
        {
            base.Register(gameObject);
            _originalHeight = gameObject.characterController.height;
            _originalCenter = gameObject.characterController.center;
            
        }
        void OnEnable()
        {
            TPPInputs.OnCrouchChanged += OnCrouch;
        }

        void OnDisable()
        {
            TPPInputs.OnCrouchChanged -= OnCrouch;
        }

        void OnCrouch(bool value)
        {
            _isCrouched = value;

            if (_isCrouched)
            {
                TryStart();
            }
            else if(!CanStandUp()){
                inputs.CrouchInput(true);
            }
            else
            {
                StopInternal();
            }
        }

        private void SmoothHeightTransition()
        {
            float targetHeight = _isCrouched ? crouchHeight : _originalHeight;
            float distance = Mathf.Abs(characterController.height - targetHeight);

            float duration = distance / transitionSpeed;

            heightTween?.Kill();

            heightTween = DOTween.To(() => _currentHeight, x => {
                _currentHeight = x;
                characterController.height = _currentHeight;
                characterController.center = new Vector3(_originalCenter.x, _currentHeight / 2f, _originalCenter.z);
                }, targetHeight, duration);
        }
        private void ApplyAnimator()
        {
            if (playerController.AnimatorExists && animName != "")
                playerController.Animator.SetBool(animName, _isCrouched);
        }
        private bool CanStandUp()
        {
            float checkDistance = _originalHeight - crouchHeight;
            Vector3 origin = playerController.transform.position + Vector3.up * (characterController.height * 0.5f);

            return !Physics.SphereCast(origin, characterController.radius * 0.9f, Vector3.up, out RaycastHit hit, checkDistance, playerController.GroundLayers);
        }

        public override bool CanStart()
        {
            return base.CanStart() && playerController.Grounded && playerController.CurrentState != PlayerState.Sprint;
        }

        public override void TryUse()
        {
            base.TryUse();
        }

        protected override void Use()
        {
            base.Use();
        }
        protected override void OnStop()
        {
            SmoothHeightTransition();
            ApplyAnimator();
        }

        protected override void OnStart()
        {
            SmoothHeightTransition();
            ApplyAnimator();
        }
    }
}