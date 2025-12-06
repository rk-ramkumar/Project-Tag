using UnityEngine;

namespace TPP
{
    public class CrouchAction : ActionComponent
    {
        public CrouchDefinition CrouchDefinition;

        private bool _isCrouched = false;
        private float _currentHeight;
        private float _standHeight;
        private Vector3 _standCenter = Vector3.zero;

        public bool IsCrouched => _isCrouched;

        public override void Awake()
        {

            base.Awake();

            _standHeight = cc.height;
            _standCenter = cc.center;

        }


        private void Update()
        {
            HandleCrouchInput();
            SmoothHeightTransition();
            ApplyAnimator();
            ApplyMovementSlowdown();
        }

        private void HandleCrouchInput()
        {
            if(CrouchDefinition.toggleMode)
            {
                if (input.crouch)
                {
                    input.crouch = false;
                    if(_isCrouched)
                    {
                        if (CanStandUp())
                        {
                            _isCrouched = false ;
                        }

                    }
                    else
                    {
                        _isCrouched= true ;
                    }
                }

            }
            else
            {
                _isCrouched = input.crouch;
            }

            if (CrouchDefinition.disableSprint && input.sprint && input.move != Vector2.zero)
            {
                _isCrouched = false;
                input.crouch = false;
            }


            if (_isCrouched && CrouchDefinition.disableSprint)
                player.SetSprint(false);

            
            player.BlockJump(_isCrouched && CrouchDefinition.disableJump);
        }

        private void SmoothHeightTransition()
        {
            float targetHeight = _isCrouched ? CrouchDefinition.crouchHeight : _standHeight;

            _currentHeight = Mathf.Lerp(
                _currentHeight,
                targetHeight,
                Time.deltaTime * CrouchDefinition.transitionSpeed
            );

            cc.height = _currentHeight;
            cc.center = new Vector3(
                _standCenter.x,
                _currentHeight / 2f,
                _standCenter.z
            );
        }

        private void ApplyAnimator()
        {
            if (player.AnimatorExists && CrouchDefinition.animBool != "")
                    player.Animator.SetBool(CrouchDefinition.animBool, _isCrouched);
        }

        private void ApplyMovementSlowdown() => player.ExternalCrouchSpeedMultiplier = _isCrouched ? CrouchDefinition.speedMultiplier : 1f;

        private bool CanStandUp()
        {
            float checkDistance = _standHeight - CrouchDefinition.crouchHeight;
            Vector3 origin = transform.position + Vector3.up * (cc.height * 0.5f);

            return !Physics.SphereCast(origin, cc.radius * 0.9f, Vector3.up, out RaycastHit hit, checkDistance, player.GroundLayers);
        }
    }

}