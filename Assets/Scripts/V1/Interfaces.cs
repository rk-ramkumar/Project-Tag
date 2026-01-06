// ==============================================
// INTERFACES
// ==============================================

using DG.Tweening;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace TPP.ActionSystem
{
    public interface IAction
    {
        ActionID ID { get; }
        ActionPriority Priority { get; }
        bool IsInterruptible { get; }
        bool CanRunConcurrently { get; }

        bool CanStart(ActionContext context);
        void StartAction(ActionContext context);
        void UpdateAction(float deltaTime);
        void FixedUpdate();
        void Stop(bool wasInterrupted);
        void Cleanup();
    }

    public interface IActionInputHandler
    {
        void ProcessInput(ActionInput input);
    }

    public interface IActionAnimator
    {
        void PlayAnimation(string stateName, float transitionTime = 0.1f);
        void SetParameter(string paramName, object value);
        bool IsAnimationPlaying(string stateName);
    }

    // ==============================================
    // ENUMS & STRUCTS
    // ==============================================

    public enum ActionID
    {
        None,
        Sprint,
        Crouch,
        Slide,
        Dash,
        Vault,
        WallRun,
        WallClimb,
        LedgeGrab,
        Mantle,
        ParkourRoll,
        Zipline,
        Swing,
        Grapple,
        LedgeShimmy,
        CornerTurn,
        Jump
    }

    public enum ActionPriority
    {
        Lowest = 0,
        Low = 100,
        Medium = 200,
        High = 300,
        Critical = 400,
        System = 500
    }

    [System.Flags]
    public enum ActionFlags
    {
        None = 0,
        DisableMovement = 1 << 0,
        DisableJump = 1 << 1,
        DisableSprint = 1 << 2,
        DisableCrouch = 1 << 3,
        FreezeRotation = 1 << 4,
        IgnoreGravity = 1 << 5,
        UseRootMotion = 1 << 6,
        CameraControl = 1 << 7
    }

    public struct ActionContext
    {
        public Transform PlayerTransform;
        public CharacterController CharacterController;
        public Rigidbody Rigidbody;
        public Animator Animator;
        public Camera PlayerCamera;
        public LayerMask GroundLayers;
        public LayerMask ClimbableLayers;
        public float DeltaTime;
        public bool IsGrounded;
        public Vector3 Velocity;
        public Vector2 MoveInput;
        public Vector2 LookInput;
        public float Speed;
        public float MaxSpeed;
        public TPPInputs Inputs;

        // Extension methods
        public RaycastHit? GroundHit { get; set; }
        public RaycastHit? WallHit { get; set; }
        public RaycastHit? LedgeHit { get; set; }
    }

    public struct ActionInput
    {
        public InputType Type;
        public float Value;
        public Vector2 VectorValue;
        public bool IsPressed;
        public bool WasPressedThisFrame;
        public bool WasReleasedThisFrame;
        public float PressTime;

        public enum InputType
        {
            Move,
            Look,
            Jump,
            Sprint,
            Crouch,
            Dash,
            Interact,
            Parkour,
            Cancel
        }
    }

    // ==============================================
    // BASE ACTION DEFINITION
    // ==============================================

    public abstract class BaseAction : ScriptableObject, IAction
    {
        [Header("Identification")]
        [SerializeField] protected ActionID _actionID = ActionID.None;
        [SerializeField] protected ActionPriority _priority = ActionPriority.Medium;
        [SerializeField] protected ActionFlags _actionFlags = ActionFlags.None;

        [Header("Timing")]
        [SerializeField] protected float _duration = 0f;
        [SerializeField] protected float _cooldown = 0f;
        [SerializeField] protected float _inputBufferTime = 0.1f;
        [SerializeField] protected bool _isInterruptible = true;
        [SerializeField] protected bool _canRunConcurrently = false;

        [Header("Animation")]
        [SerializeField] protected AnimationClip _animationClip;
        [SerializeField] protected float _animationTransitionTime = 0.1f;
        [SerializeField] protected bool _useRootMotion = false;

        [Header("Visual Effects")]
        [SerializeField] protected ParticleSystem _startVFX;
        [SerializeField] protected ParticleSystem _loopVFX;
        [SerializeField] protected ParticleSystem _stopVFX;

        [Header("Audio")]
        [SerializeField] protected AudioClip _startSFX;
        [SerializeField] protected AudioClip _loopSFX;
        [SerializeField] protected AudioClip _stopSFX;
        [SerializeField] protected float _sfxVolume = 1f;

        // Properties
        public ActionID ID => _actionID;
        public ActionPriority Priority => _priority;
        public bool IsInterruptible => _isInterruptible;
        public bool CanRunConcurrently => _canRunConcurrently;

        // Runtime state
        protected ActionContext _context;
        protected float _elapsedTime;
        protected float _cooldownTimer;
        protected bool _isActive;
        protected bool _isComplete;

        // References
        protected GameObject _player;
        protected IActionAnimator _animator;
        protected AudioSource _audioSource;
        protected ParticleSystem _activeLoopVFX;

        // ==============================================
        // PUBLIC API
        // ==============================================

        public virtual bool CanStart(ActionContext context)
        {
            if (_isActive)
            {
                Debug.Log($"{_actionID} action already in Use");
                return false;
            }
            if (IsOnCooldown)
            {
                Debug.Log($"{_actionID} action in Cooldown. Use after {_cooldownTimer}s");
                return false;
            };
            return true;
        }

        public virtual void StartAction(ActionContext context)
        {
            _context = context;
            _elapsedTime = 0f;
            _isActive = true;
            _isComplete = false;

            CacheReferences();
            ApplyActionFlags();
            PlayStartEffects();

            OnStart();
        }

        public virtual void UpdateAction(float deltaTime)
        {
            if (!_isActive)
            {
                // Update cooldown
                if (_cooldownTimer > 0)
                {
                    Debug.Log($"CoolDown Started. ReUse in {_cooldownTimer}s");
                    _cooldownTimer -= deltaTime;
                }
                return;
            }

            _elapsedTime += deltaTime;

            // Check duration
            if (_duration > 0 && _elapsedTime >= _duration)
            {
                Complete();
                return;
            }


            OnUpdate(deltaTime);
        }

        public virtual void FixedUpdate()
        {
            if (!_isActive) return;
            OnFixedUpdate();
        }

        public virtual void Stop(bool wasInterrupted)
        {
            if (!_isActive) return;

            _isActive = false;
            RemoveActionFlags();
            PlayStopEffects(wasInterrupted);

            if (!wasInterrupted && _cooldown > 0)
            {
                _cooldownTimer = _cooldown;

            }

            OnStop(wasInterrupted);
            Cleanup();
        }

        public virtual void Cleanup()
        {
            if (_activeLoopVFX != null)
            {
                _activeLoopVFX.Stop(true);
                if (Application.isPlaying)
                {
                    Destroy(_activeLoopVFX.gameObject, 2f);
                }
            }
        }

        // ==============================================
        // PROTECTED VIRTUAL METHODS
        // ==============================================

        protected virtual void OnStart() { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnFixedUpdate() { }
        protected virtual void OnStop(bool wasInterrupted) { }

        protected virtual bool ValidateContext()
        {
            return _context.PlayerTransform != null &&
                   _context.CharacterController != null;
        }

        // ==============================================
        // HELPER METHODS
        // ==============================================

        protected void Complete()
        {
            _isComplete = true;
            Stop(false);
        }

        protected void CacheReferences()
        {
            _player = _context.PlayerTransform.gameObject;
            _animator = _player.GetComponentInChildren<IActionAnimator>();
            _audioSource = _player.GetComponent<AudioSource>();
        }

        protected void ApplyActionFlags()
        {
            // These flags would be applied to a PlayerStateManager
            // For now, we'll just log them
            Debug.Log($"Applying action flags: {_actionFlags}");
        }

        protected void RemoveActionFlags()
        {
            Debug.Log($"Removing action flags: {_actionFlags}");
        }

        protected void PlayStartEffects()
        {
            if (_animator != null && _animationClip != null)
            {
                _animator.PlayAnimation(_animationClip.name, _animationTransitionTime);
            }

            if (_startVFX != null)
            {
                Instantiate(_startVFX, _player.transform.position, Quaternion.identity);
            }

            if (_audioSource != null && _startSFX != null)
            {
                _audioSource.PlayOneShot(_startSFX, _sfxVolume);
            }

            if (_loopVFX != null)
            {
                _activeLoopVFX = Instantiate(_loopVFX, _player.transform);
            }

            if (_audioSource != null && _loopSFX != null)
            {
                _audioSource.loop = true;
                _audioSource.clip = _loopSFX;
                _audioSource.volume = _sfxVolume;
                _audioSource.Play();
            }
        }

        protected void PlayStopEffects(bool wasInterrupted)
        {
            if (_animator != null)
            {
                // Return to idle or appropriate state
            }

            if (_stopVFX != null && !wasInterrupted)
            {
                Instantiate(_stopVFX, _player.transform.position, Quaternion.identity);
            }

            if (_audioSource != null)
            {
                _audioSource.loop = false;

                if (!wasInterrupted && _stopSFX != null)
                {
                    _audioSource.PlayOneShot(_stopSFX, _sfxVolume);
                }
            }
        }

        // ==============================================
        // UTILITY METHODS
        // ==============================================

        protected bool CheckSphereCast(Vector3 origin, float radius, Vector3 direction,
                                     float distance, LayerMask layerMask, out RaycastHit hit)
        {
            return Physics.SphereCast(origin, radius, direction, out hit, distance, layerMask);
        }

        protected bool CheckCapsuleCast(Vector3 point1, Vector3 point2, float radius,
                                       Vector3 direction, float distance,
                                       LayerMask layerMask, out RaycastHit hit)
        {
            return Physics.CapsuleCast(point1, point2, radius, direction, out hit, distance, layerMask);
        }

        protected bool CheckLedge(Vector3 origin, Vector3 direction, float maxDistance,
                                 LayerMask layerMask, out LedgeInfo ledgeInfo)
        {
            ledgeInfo = new LedgeInfo();

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
            {
                // Check if there's a ledge above
                Vector3 ledgeCheckOrigin = hit.point + Vector3.up * 0.5f;
                if (!Physics.Raycast(ledgeCheckOrigin, Vector3.up, 1f, layerMask))
                {
                    ledgeInfo.IsValid = true;
                    ledgeInfo.Point = hit.point;
                    ledgeInfo.Normal = hit.normal;
                    return true;
                }
            }

            return false;
        }

        protected struct LedgeInfo
        {
            public bool IsValid;
            public Vector3 Point;
            public Vector3 Normal;
            public Collider Collider;
        }

        #region Public Api
        public bool IsOnCooldown => _cooldownTimer > 0;
        public bool IsActive => _isActive;
        #endregion

    }

    // ==============================================
    // ACTION ANIMATOR
    // ==============================================

    public class ActionAnimator : MonoBehaviour, IActionAnimator
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private ActionManager _actionManager;

        [Header("Settings")]
        [SerializeField] private float _defaultTransitionTime = 0.1f;

        // Runtime
        private Dictionary<string, int> _animationHashes = new();
        private string _currentState;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();

            if (_actionManager == null)
                _actionManager = GetComponentInParent<ActionManager>();

            if (_actionManager != null)
            {
                _actionManager.OnActionStarted += OnActionStarted;
                _actionManager.OnActionStopped += OnActionStopped;
            }

            CacheAnimationHashes();
        }

        private void OnDestroy()
        {
            if (_actionManager != null)
            {
                _actionManager.OnActionStarted -= OnActionStarted;
                _actionManager.OnActionStopped -= OnActionStopped;
            }
        }

        #endregion

        #region IActionAnimator Implementation

        public void PlayAnimation(string stateName, float transitionTime = 0.1f)
        {
            if (_animator == null || string.IsNullOrEmpty(stateName)) return;

            if (_animationHashes.TryGetValue(stateName, out int hash))
            {
                _animator.CrossFade(hash, transitionTime);
                _currentState = stateName;
            }
            else
            {
                // Try with string directly
                _animator.CrossFade(stateName, transitionTime);
                _currentState = stateName;
            }
        }

        public void SetParameter(string paramName, object value)
        {
            if (_animator == null) return;

            switch (value)
            {
                case bool boolValue:
                    _animator.SetBool(paramName, boolValue);
                    break;
                case float floatValue:
                    _animator.SetFloat(paramName, floatValue);
                    break;
                case int intValue:
                    _animator.SetInteger(paramName, intValue);
                    break;
            }
        }

        public bool IsAnimationPlaying(string stateName)
        {
            if (_animator == null) return false;

            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(stateName);
        }

        #endregion

        #region Event Handlers

        private void OnActionStarted(BaseAction action)
        {
            // You could have animation mappings per action
            // For now, just log

            Debug.Log($"Action {action.ID} started - update animations if needed");
        }

        private void OnActionStopped(BaseAction action, bool wasInterrupted)
        {
            // Return to idle or appropriate state
            if (!wasInterrupted)
            {
                PlayAnimation("Idle", _defaultTransitionTime);
            }
        }

        #endregion

        #region Helper Methods

        private void CacheAnimationHashes()
        {
            if (_animator == null) return;

            var controller = _animator.runtimeAnimatorController;
            if (controller == null) return;

            foreach (var clip in controller.animationClips)
            {
                _animationHashes[clip.name] = Animator.StringToHash(clip.name);
            }
        }

        #endregion
    }
}