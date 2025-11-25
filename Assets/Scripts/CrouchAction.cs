using UnityEngine;

namespace TPP
{
    public class CrouchAction : ActionComponent
    {
        public CrouchDefinition CrouchDefinition;
        public CharacterController Controller;

        private bool _isCrouched = false;
        private float _currentHeight;
        private float _standHeight;
        private Vector3 _standCenter = Vector3.zero;

        public bool IsCrouched => _isCrouched;

        public override void Awake()
        {

            base.Awake();
            Controller = GetComponent<CharacterController>();

            if(Controller != null )
            {
                _standHeight = Controller.height;
                _standCenter = Controller.center;
            }
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
                if (TPPInputs.crouch)
                {
                    TPPInputs.crouch = false;
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
                _isCrouched = TPPInputs.crouch;
            }

            if(CrouchDefinition.disableSprint && TPPInputs.sprint)
            {
                _isCrouched = false;
                TPPInputs.crouch = false;
            }


            if (_isCrouched && CrouchDefinition.disableSprint)
                Player.SetSprint(false);

            
            Player.BlockJump(_isCrouched && CrouchDefinition.disableJump);
        }

        private void SmoothHeightTransition()
        {
            float targetHeight = _isCrouched ? CrouchDefinition.crouchHeight : _standHeight;

            _currentHeight = Mathf.Lerp(
                _currentHeight,
                targetHeight,
                Time.deltaTime * CrouchDefinition.transitionSpeed
            );

            Controller.height = _currentHeight;
            Controller.center = new Vector3(
                _standCenter.x,
                _currentHeight / 2f,
                _standCenter.z
            );
        }

        private void ApplyAnimator()
        {
            if (Player.AnimatorExists && CrouchDefinition.animBool != "")
                    Player.Animator.SetBool(CrouchDefinition.animBool, _isCrouched);
        }

        private void ApplyMovementSlowdown() => Player.ExternalCrouchSpeedMultiplier = _isCrouched ? CrouchDefinition.speedMultiplier : 1f;

        private bool CanStandUp()
        {
            float checkDistance = _standHeight - CrouchDefinition.crouchHeight;
            Vector3 origin = transform.position + Vector3.up * (Controller.height * 0.5f);

            return !Physics.SphereCast(origin, Controller.radius * 0.9f, Vector3.up, out RaycastHit hit, checkDistance, Player.GroundLayers);
        }
    }

}