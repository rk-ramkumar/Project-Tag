// ==============================================
// ACTION MANAGER
// ==============================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace TPP.ActionSystem
{
    [DefaultExecutionOrder(-50)] // Run before other scripts
    public class ActionManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ActionDatabase _actionDatabase;
        [SerializeField] private bool _debugLog = false;
        
        [Header("State")]
        [SerializeField] private List<BaseAction> _activeActions = new();
        [SerializeField] private List<QueuedAction> _queuedActions = new();
        
        // References
        private ActionContext _context;
        private TPPInputs _inputHandler;
        private ActionAnimator _actionAnimator;
        private PlayerController _playerController;
        
        // Runtime
        private Dictionary<ActionID, BaseAction> _actionRegistry = new();
        private Queue<ActionRequest> _requestBuffer = new();
        private float _bufferTime = 0.15f;
        
        // Events
        public event Action<BaseAction> OnActionStarted;
        public event Action<BaseAction, bool> OnActionStopped;
        public event Action<ActionID> OnActionBlocked;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeContext();
            InitializeActions();
            InitializeComponents();
        }
        
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            // Process input buffer
            ProcessBuffer(deltaTime);
            
            // Update all active actions
            for (int i = _activeActions.Count - 1; i >= 0; i--)
            {
                var action = _activeActions[i];
                action.UpdateAction(deltaTime);
                
                // Check if action should be removed
                if (!IsActionActive(action))
                {
                    RemoveAction(action, false);
                }
            }
            
            // Update context
            UpdateContext(deltaTime);
        }
        
        private void FixedUpdate()
        {
            foreach (var action in _activeActions)
            {
                action.FixedUpdate();
            }
        }
        
        private void OnDestroy()
        {
            CleanupAllActions();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeContext()
        {
            _context = new ActionContext
            {
                PlayerTransform = transform,
                CharacterController = GetComponent<CharacterController>(),
                Rigidbody = GetComponent<Rigidbody>(),
                Animator = GetComponentInChildren<Animator>(),
                PlayerCamera = Camera.main,
                GroundLayers = LayerMask.GetMask("Ground", "Default"),
                ClimbableLayers = LayerMask.GetMask("Climbable", "Wall")
            };
        }
        
        private void InitializeActions()
        {
            if (_actionDatabase == null)
            {
                Debug.LogError("ActionDatabase not assigned!");
                return;
            }
            
            foreach (var action in _actionDatabase.GetAllActions())
            {
                if (action != null && action.ID != ActionID.None)
                {
                    var runtimeAction = Instantiate(action);
                    _actionRegistry[action.ID] = runtimeAction;
                    
                    if (_debugLog)
                        Debug.Log($"Registered action: {action.ID}");
                }
            }
        }
        
        private void InitializeComponents()
        {
            _inputHandler = GetComponent<TPPInputs>();
            _actionAnimator = GetComponentInChildren<ActionAnimator>();
            _playerController = GetComponent<PlayerController>();
            
            //if (_inputHandler == null)
            //{
            //    _inputHandler = gameObject.AddComponent<ActionInputHandler>();
            //}
            
            if (_actionAnimator == null)
            {
                var animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    _actionAnimator = animator.gameObject.AddComponent<ActionAnimator>();
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        public bool RequestAction(ActionID actionID, 
                                ActionPriority overridePriority = ActionPriority.Medium,
                                bool bufferInput = false)
        {
            if (!_actionRegistry.TryGetValue(actionID, out var action))
            {
                Debug.LogWarning($"Action {actionID} not registered!");
                return false;
            }
            
            var request = new ActionRequest
            {
                Action = action,
                Priority = overridePriority,
                RequestTime = Time.time,
                BufferTime = bufferInput ? _bufferTime : 0f
            };
            
            if (bufferInput)
            {
                _requestBuffer.Enqueue(request);
                return true;
            }
            
            return ProcessRequest(request);
        }
        
        public void ForceStopAction(ActionID actionID)
        {
            var action = GetActiveAction(actionID);
            if (action != null)
            {
                RemoveAction(action, true);
            }
        }
        
        public void StopAllActions(bool immediate = false)
        {
            foreach (var action in _activeActions.ToArray())
            {
                RemoveAction(action, immediate);
            }
        }
        
        public bool IsActionActive(ActionID actionID)
        {
            return GetActiveAction(actionID) != null;
        }
        
        public BaseAction GetActiveAction(ActionID actionID)
        {
            return _activeActions.Find(a => a.ID == actionID);
        }
        
        public float GetActionCooldown(ActionID actionID)
        {
            if (_actionRegistry.TryGetValue(actionID, out var action))
            {
                // We would need to track cooldowns in the manager
                // For now, return 0
                return 0f;
            }
            return 0f;
        }
        
        #endregion
        
        #region Internal Processing
        
        private void ProcessBuffer(float deltaTime)
        {
            while (_requestBuffer.Count > 0)
            {
                var request = _requestBuffer.Peek();
                
                if (Time.time - request.RequestTime > request.BufferTime)
                {
                    _requestBuffer.Dequeue();
                    continue;
                }
                
                if (ProcessRequest(request))
                {
                    _requestBuffer.Dequeue();
                    break;
                }
                
                // Can't process yet, keep in buffer
                break;
            }
        }
        
        private bool ProcessRequest(ActionRequest request)
        {
            var action = request.Action;
            
            // Check if action can start
            if (!action.CanStart(_context))
            {
                OnActionBlocked?.Invoke(action.ID);
                return false;
            }
            
            // Check for conflicts with active actions
            if (!CanStartAction(action, request.Priority))
            {
                return false;
            }
            
            // Start the action
            return StartAction(action);
        }
        
        private bool CanStartAction(BaseAction newAction, ActionPriority priority)
        {
            foreach (var activeAction in _activeActions)
            {
                // Check if actions can run concurrently
                if (!newAction.CanRunConcurrently && !activeAction.CanRunConcurrently)
                {
                    // Check priority
                    if ((int)priority <= (int)activeAction.Priority)
                    {
                        if (_debugLog)
                            Debug.Log($"Action {newAction.ID} blocked by {activeAction.ID} (priority)");
                        return false;
                    }
                    
                    // Higher priority - interrupt current action
                    if (!activeAction.IsInterruptible)
                    {
                        if (_debugLog)
                            Debug.Log($"Action {newAction.ID} blocked - {activeAction.ID} is not interruptible");
                        return false;
                    }
                    
                    // Interrupt the current action
                    RemoveAction(activeAction, true);
                }
            }
            
            return true;
        }
        
        private bool StartAction(BaseAction action)
        {
            try
            {
                action.StartAction(_context);
                _activeActions.Add(action);
                
                if (_debugLog)
                    Debug.Log($"Started action: {action.ID}");
                
                OnActionStarted?.Invoke(action);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start action {action.ID}: {e.Message}");
                return false;
            }
        }
        
        private void RemoveAction(BaseAction action, bool wasInterrupted)
        {
            if (_activeActions.Remove(action))
            {
                action.Stop(wasInterrupted);
                
                if (_debugLog)
                    Debug.Log($"Stopped action: {action.ID} (interrupted: {wasInterrupted})");
                
                OnActionStopped?.Invoke(action, wasInterrupted);
            }
        }
        
        private bool IsActionActive(BaseAction action)
        {
            // This would check if the action is still valid
            // Could check timers, conditions, etc.
            return true; // Simplified
        }
        
        private void UpdateContext(float deltaTime)
        {
            _context.DeltaTime = deltaTime;
            _context.IsGrounded = _playerController?.Grounded ?? false;
            _context.Velocity = _playerController?.Velocity ?? Vector3.zero;
            
            if (_inputHandler != null)
            {
                _context.MoveInput = _inputHandler.GetMoveInput();
                _context.LookInput = _inputHandler.GetLookInput();
            }
        }
        
        private void CleanupAllActions()
        {
            foreach (var action in _activeActions)
            {
                action.Cleanup();
            }
            _activeActions.Clear();
            
            foreach (var action in _actionRegistry.Values)
            {
                Destroy(action);
            }
            _actionRegistry.Clear();
        }
        
        #endregion
        
        #region Helper Classes
        
        private struct ActionRequest
        {
            public BaseAction Action;
            public ActionPriority Priority;
            public float RequestTime;
            public float BufferTime;
        }
        
        private struct QueuedAction
        {
            public BaseAction Action;
            public float QueueTime;
            public float ExpireTime;
        }
        
        #endregion
        
        #region Debug
        
        private void OnGUI()
        {
            if (!_debugLog) return;
            
            GUILayout.BeginArea(new Rect(10, 100, 10000, 10000));
            GUILayout.Label("=== ACTION MANAGER DEBUG ===");
            GUILayout.Label($"Active Actions: {_activeActions.Count}");
            
            foreach (var action in _activeActions)
            {
                GUILayout.Label($"- {action.ID}");
            }
            
            GUILayout.Label($"Queued Actions: {_queuedActions.Count}");
            GUILayout.Label($"Buffered Requests: {_requestBuffer.Count}");
            GUILayout.EndArea();
        }
        
        #endregion
    }
    
    // ==============================================
    // ACTION DATABASE
    // ==============================================
    
    [CreateAssetMenu(fileName = "ActionDatabase", menuName = "TPP/Actions/Database")]
    public class ActionDatabase : ScriptableObject
    {
        [SerializeField] private List<BaseAction> _actions = new();
        
        private Dictionary<ActionID, BaseAction> _actionMap;
        
        public BaseAction GetAction(ActionID actionID)
        {
            InitializeMap();
            return _actionMap.TryGetValue(actionID, out var action) ? action : null;
        }
        
        public List<BaseAction> GetAllActions()
        {
            return _actions;
        }
        
        public List<ActionID> GetAllActionIDs()
        {
            return _actions.Select(a => a.ID).ToList();
        }
        
        private void InitializeMap()
        {
            if (_actionMap != null) return;
            
            _actionMap = new Dictionary<ActionID, BaseAction>();
            foreach (var action in _actions)
            {
                if (action != null && action.ID != ActionID.None)
                {
                    _actionMap[action.ID] = action;
                }
            }
        }
    }
}