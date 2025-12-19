using UnityEngine;

namespace TPP
{
    public class CrouchAction : ActionComponent
    {
        public CrouchDefinition def;

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
            if(input.sprint) return;
            HandleCrouchInput();
            SmoothHeightTransition();
            ApplyAnimator();
            ApplyMovementSlowdown();
        }

        private void HandleCrouchInput()
        {
            if(def.toggleMode)
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

            if (def.disableSprint && input.sprint && input.move != Vector2.zero)
            {
                _isCrouched = false;
                input.crouch = false;
            }


            if (_isCrouched && def.disableSprint)
                player.SetSprint(false);

            
            player.BlockJump(_isCrouched && def.disableJump);
        }

        private void SmoothHeightTransition()
        {
            float targetHeight = _isCrouched ? def.crouchHeight : _standHeight;

            _currentHeight = Mathf.Lerp(
                _currentHeight,
                targetHeight,
                Time.deltaTime * def.transitionSpeed
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
            if (player.AnimatorExists && def.animBool != "")
                    player.Animator.SetBool(def.animBool, _isCrouched);
        }

        private void ApplyMovementSlowdown() => player.ExternalCrouchSpeedMultiplier = _isCrouched ? def.speedMultiplier : 1f;

        private bool CanStandUp()
        {
            float checkDistance = _standHeight - def.crouchHeight;
            Vector3 origin = transform.position + Vector3.up * (cc.height * 0.5f);

            return !Physics.SphereCast(origin, cc.radius * 0.9f, Vector3.up, out RaycastHit hit, checkDistance, player.GroundLayers);
        }

        public void ForceEnterCrouch()
        {
            _isCrouched = true;
            // optionally snap capsule immediately to crouch height
            cc.height = def.crouchHeight;
            cc.center = new Vector3(_standCenter.x, cc.height / 2f, _standCenter.z);
            player.ExternalCrouchSpeedMultiplier = def.speedMultiplier;
            if (player.AnimatorExists && def.animBool != "") player.Animator.SetBool(def.animBool, true);
        }

    }

}