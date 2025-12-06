using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPP
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class PlayerController : MonoBehaviour
    {

        [Header("Player")]
        public float MoveSpeed = 6.0f;
        public float SprintSpeed = 10.0f;

        [Header("Dash Settings")]
        public float DashForce = 20f;
        public float DashDuration = 0.2f;
        public float DashCooldown = 2.0f;
        public GameObject DashTrail;

        private bool _isDashing = false;
        private float _dashTimeRemaining = 0f;
        private Vector3 _dashDirection;

        [Space(10)]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        public float SprintRotationSmoothTime = 0.14f;
        public float SpeedChangeRate = 20.0f;
        public float DecelerationRate = 80.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootStepAudioClips;
        [Range(0, 1)] public float FootStepAudioVolume = 0.5f;

        [Space(10)]
        public float JumpHeight = 1.8f;
        public float Gravity = -22.0f;

        [Space(20)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Space(10)]
        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public GameObject CinemachineCameraSource;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        CinemachineImpulseSource _cinemachineImpulseSource;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _lastDashDT;
        private Coroutine _trailCoroutine;
        enum State
        {
            Idle,
            Walk,
            Sprint,
            Crouch,
            Dashing,
            Jumping,
            Falling,
            MidAir
        }
        private State _currentState;
        // movement blocking:
        private readonly HashSet<object> _movementBlockers = new HashSet<object>();

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDDash;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private TPPInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        private bool _jumpBlocked;
        public float ExternalCrouchSpeedMultiplier = 1f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }
        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }
        void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<TPPInputs>();
            _cinemachineImpulseSource = GetComponent<CinemachineImpulseSource>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
            AssignAnimatorIDs();
            SetDashTrail(false);
            
            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _lastDashDT = -DashCooldown;
        }

        void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            GroundedCheck();
            Dash();
            JumpAndGravity();
            Move();
            SetState();
        }

        void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimatorIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDDash = Animator.StringToHash("Dash");
        }
        private void GroundedCheck()
        {
            Vector3 spherePosition = transform.position;
            spherePosition.y -= GroundedOffset;
            Grounded = Physics.CheckSphere(spherePosition, _controller.radius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }

        }
        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            //clamp our rotation so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if (_movementBlockers.Count > 0) return;

            if (_isDashing) return;

            // Determine base speed
            bool isSprinting = _input.sprint && _input.move != Vector2.zero;
            float targetSpeed = isSprinting ? SprintSpeed : MoveSpeed;
            targetSpeed *= ExternalCrouchSpeedMultiplier;

            // Zero speed if no input
            if (_input.move == Vector2.zero)
                targetSpeed = 0f;

            // Current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;

            // Smooth acceleration / deceleration
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1.0f;

            float target = targetSpeed * inputMagnitude;

            float changeRate =
                target < currentHorizontalSpeed ? DecelerationRate : SpeedChangeRate;

            // Apply movement momentum
            _speed = Mathf.MoveTowards(currentHorizontalSpeed, target, changeRate * Time.deltaTime);

            // Animation blend
            _animationBlend = Mathf.MoveTowards(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // Movement direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0f, _input.move.y).normalized;

            // ROTATION HANDLING
            if (_input.move != Vector2.zero)
            {
                // Sprinting has slightly slower rotation → prevents zig-zag abuse
                float rotationSmooth = isSprinting ? SprintRotationSmoothTime : RotationSmoothTime;

                // Calculate target rotation
                _targetRotation =
                    Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                    _mainCamera.transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    _targetRotation,
                    ref _rotationVelocity,
                    rotationSmooth
                );

                transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }

            // Apply movement
            _controller.Move(
                transform.forward.normalized * (_speed * Time.deltaTime) +
                new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime
            );

            // Animator
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if(_isDashing)
            {
                return;
            }

            if (_jumpBlocked)
            {
                return ;
            }
            
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void Dash()
        {
            if (_input.dash)
            {
                _input.dash = CanDash();
                
            }
            // Start dash
            if (_input.dash)
            {
                StartDash();
            }

            // Process ongoing dash
            if (_isDashing)
            {
                float dashSpeed = DashForce / DashDuration; // speed per second
                _controller.Move(dashSpeed * Time.deltaTime * _dashDirection);

                _dashTimeRemaining -= Time.deltaTime;

                if (_dashTimeRemaining <= 0)
                {
                    EndDash();
                }

                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, dashSpeed);
                    //_animator.SetFloat(_animIDMotionSpeed, dashSpeed / SprintSpeed);

                }
            }
        }

        private void StartDash()
        {
            _input.dash = false; // reset input
            _isDashing = true;
            _dashTimeRemaining = DashDuration;
            _lastDashDT = Time.time;

            // dash direction = current forward
            _dashDirection = transform.forward;

            if (_trailCoroutine != null)
                StopCoroutine(_trailCoroutine);
            SetDashTrail(true);

            if (_hasAnimator && Grounded)
            {
                _animator.SetTrigger(_animIDDash);
            }
        }

        private void EndDash()
        {
            _isDashing = false;
            _trailCoroutine = StartCoroutine(ResetTrailEffect());
        }

        private bool CanDash() => !(_isDashing || _input.crouch || Time.time < _lastDashDT + DashCooldown );

        private void SetDashTrail(bool active )
        {
            if (DashTrail != null) DashTrail.SetActive(active);
        }

        private IEnumerator ResetTrailEffect()
        {
            yield return new WaitForSeconds(0.5f);
            SetDashTrail(false);
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {

            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }


        private void SetState()
        {
            // Highest priority: dashing
            if (_isDashing)
            {
                _currentState = State.Dashing;
                return;
            }

            if (!Grounded)
            {
                _currentState = (_verticalVelocity > 0.1f) ? State.Jumping : State.Falling;
                return;
            }

            // Grounded states
            bool hasMoveInput = _input.move.sqrMagnitude > 0.0001f;
            bool isSprinting = _input.sprint && hasMoveInput;
            bool isCrouching = _input.crouch;
            float epsilon = 0.01f;

            if (isCrouching)
            {
                _currentState = State.Crouch;
                return;
            }

            if (!hasMoveInput || _speed <= epsilon)
            {
                _currentState = State.Idle;
                return;
            }

            // Use explicit sprint intent rather than raw speed comparison
            if (isSprinting)
            {
                _currentState = State.Sprint;
                return;
            }

            _currentState = State.Walk;
        }

        // Methods called by animation events
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootStepAudioClips.Length > 0)
                {
                    var index = UnityEngine.Random.Range(0, FootStepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootStepAudioClips[index], transform.TransformPoint(_controller.center), FootStepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootStepAudioVolume);
                _cinemachineImpulseSource.GenerateImpulseWithForce(0.3f);
            }
        }

        public void SetSprint(bool on) => _input.sprint = on;
        public void BlockJump(bool block) => _jumpBlocked = block;


        public bool AnimatorExists => _hasAnimator;
        public Animator Animator => _animator;

        public void SetMovementBlocked(bool blocked, object source)
        {
            if (blocked) _movementBlockers.Add(source);
            else _movementBlockers.Remove(source);
        }

    };    
}