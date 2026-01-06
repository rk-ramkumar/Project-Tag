using UnityEngine;

namespace TPP.ActionSystem
{
    // ==============================================
    // DASH ACTION
    // ==============================================
    
    [CreateAssetMenu(fileName = "DashAction", menuName = "TPP/Actions/Dash")]
    public class DashAction : BaseAction
    {
        [Header("Dash Settings")]
        [SerializeField] private float _dashSpeed = 20f;
        [SerializeField] private float _dashDuration = 0.3f;
        [SerializeField] private float _dashCooldown = 1f;
        [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Air Dash")]
        [SerializeField] private bool _allowAirDash = true;
        [SerializeField] private int _maxAirDashes = 1;
        [SerializeField] private float _airDashSpeedMultiplier = 0.8f;
        
        [Header("Visuals")]
        [SerializeField] private GameObject _dashTrailPrefab;
        [SerializeField] private float _trailDuration = 0.5f;
        
        // Runtime
        private Vector3 _dashDirection;
        private int _airDashCount;
        private GameObject _activeTrail;
        private ParticleSystem[] _dashParticles;
        
        protected override void OnStart()
        {
            // Calculate dash direction
            if (_context.MoveInput.magnitude > 0.1f)
            {
                Vector3 inputDirection = new Vector3(_context.MoveInput.x, 0, _context.MoveInput.y);
                _dashDirection = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * inputDirection;
            }
            else
            {
                _dashDirection = _context.PlayerTransform.forward;
            }
            
            _dashDirection.Normalize();
            
            // Apply air dash logic
            if (!_context.IsGrounded)
            {
                if (!_allowAirDash || _airDashCount >= _maxAirDashes)
                {
                    Complete();
                    return;
                }
                
                _airDashCount++;
            }
            
            // Create trail effect
            if (_dashTrailPrefab != null)
            {
                Vector3 spawnPos = _context.PlayerTransform.position;
                spawnPos.y = 1.3f;
                _activeTrail = Instantiate(_dashTrailPrefab, spawnPos, _context.PlayerTransform.rotation);
                _activeTrail.transform.parent = _context.PlayerTransform;
                _dashParticles = _activeTrail.GetComponentsInChildren<ParticleSystem>();
            }
            
            // Start dash coroutine (would be handled by manager)
            _duration = _dashDuration;
            _cooldown = _dashCooldown;
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isActive) return;
            
            // Calculate dash speed with curve
            float curveValue = _speedCurve.Evaluate(_elapsedTime / _duration);
            float currentSpeed = _dashSpeed * curveValue;

            // Apply dash velocity
            if (_context.Rigidbody != null)
            {
                Vector3 velocity = _dashDirection * currentSpeed;
                velocity.y = _context.Rigidbody.linearVelocity.y; // Maintain vertical velocity
                _context.Rigidbody.linearVelocity = velocity;
            }
            else if (_context.CharacterController != null)
            {
                Vector3 movement = _dashDirection * currentSpeed * deltaTime;
                _context.CharacterController.Move(movement);
            }
        }
        
        protected override void OnStop(bool wasInterrupted)
        {
            // Reset air dash count when grounded
            if (_context.IsGrounded)
            {
                _airDashCount = 0;
            }
            
            // Clean up trail
            if (_activeTrail != null)
            {
                foreach (var ps in _dashParticles)
                {
                    ps.Stop();
                }
                Destroy(_activeTrail, _trailDuration);
            }
        }
        
        public override bool CanStart(ActionContext context)
        {
            if (!base.CanStart(context)) return false;
            
            // Can't dash if sliding or wall running
            // (You'd check for conflicting actions here)
            
            return true;
        }
    }
    
    // ==============================================
    // WALL RUN ACTION
    // ==============================================
    
    [CreateAssetMenu(fileName = "WallRunAction", menuName = "TPP/Actions/WallRun")]
    public class WallRunAction : BaseAction
    {
        [Header("Wall Run Settings")]
        [SerializeField] private float _wallRunSpeed = 8f;
        [SerializeField] private float _wallRunGravity = 2f;
        [SerializeField] private float _maxWallRunTime = 2f;
        [SerializeField] private float _wallRunCameraTilt = 15f;
        [SerializeField] private float _cameraTiltSpeed = 5f;
        
        [Header("Wall Detection")]
        [SerializeField] private float _wallCheckDistance = 0.5f;
        [SerializeField] private float _minWallHeight = 1.5f;
        [SerializeField] private LayerMask _wallLayers;
        
        [Header("Exit Options")]
        [SerializeField] private float _wallJumpForce = 10f;
        [SerializeField] private float _wallJumpUpForce = 5f;
        [SerializeField] private bool _allowWallClimb = true;
        
        // Runtime
        private Vector3 _wallNormal;
        private Vector3 _wallRunDirection;
        private bool _isOnRightWall;
        private float _cameraTilt;
        
        protected override void OnStart()
        {
            if (!FindWall(out _wallNormal, out _isOnRightWall))
            {
                Complete();
                return;
            }
            
            // Calculate run direction (parallel to wall)
            _wallRunDirection = Vector3.Cross(_wallNormal, Vector3.up);
            if (_isOnRightWall)
            {
                _wallRunDirection = -_wallRunDirection;
            }
            
            // Align player with wall
            Vector3 lookDirection = Vector3.Cross(_wallNormal, Vector3.up);
            _context.PlayerTransform.rotation = Quaternion.LookRotation(lookDirection);
            
            _duration = _maxWallRunTime;
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isActive) return;
            
            // Check if still on wall
            if (!IsOnWall())
            {
                Complete();
                return;
            }
            
            // Apply wall run movement
            Vector3 velocity = _wallRunDirection * _wallRunSpeed;
            velocity.y -= _wallRunGravity * deltaTime;
            
            if (_context.Rigidbody != null)
            {
                _context.Rigidbody.linearVelocity = velocity;
            }
            else if (_context.CharacterController != null)
            {
                _context.CharacterController.Move(velocity * deltaTime);
            }
            
            // Handle camera tilt
            UpdateCameraTilt(deltaTime);
            
            // Check for exit conditions
            if (_context.Inputs.jump)
            {
                PerformWallJump();
                Complete();
            }
            else if (_context.Inputs.crouch && _allowWallClimb)
            {
                PerformWallClimb();
                Complete();
            }
        }
        
        protected override void OnStop(bool wasInterrupted)
        {
            // Reset camera tilt
            _cameraTilt = 0f;
            UpdateCameraTilt(Time.deltaTime);
        }
        
        private bool FindWall(out Vector3 wallNormal, out bool isRightWall)
        {
            wallNormal = Vector3.zero;
            isRightWall = false;
            
            Vector3[] checkDirections = 
            {
                _context.PlayerTransform.right,    // Right
                -_context.PlayerTransform.right    // Left
            };
            
            for (int i = 0; i < checkDirections.Length; i++)
            {
                if (Physics.Raycast(_context.PlayerTransform.position,
                                   checkDirections[i],
                                   out RaycastHit hit,
                                   _wallCheckDistance,
                                   _wallLayers))
                {
                    // Check wall height
                    if (CheckWallHeight(hit.point))
                    {
                        wallNormal = hit.normal;
                        isRightWall = (i == 0);
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private bool CheckWallHeight(Vector3 hitPoint)
        {
            Vector3 start = hitPoint + Vector3.up * 0.1f;
            return !Physics.Raycast(start, Vector3.up, _minWallHeight, _wallLayers);
        }
        
        private bool IsOnWall()
        {
            Vector3 checkDirection = _isOnRightWall ? 
                _context.PlayerTransform.right : -_context.PlayerTransform.right;
            
            return Physics.Raycast(_context.PlayerTransform.position,
                                 checkDirection,
                                 _wallCheckDistance * 1.2f,
                                 _wallLayers);
        }
        
        private void UpdateCameraTilt(float deltaTime)
        {
            float targetTilt = _isOnRightWall ? -_wallRunCameraTilt : _wallRunCameraTilt;
            _cameraTilt = Mathf.Lerp(_cameraTilt, targetTilt, deltaTime * _cameraTiltSpeed);
            
            // Apply to camera (you'd need a camera controller reference)
            // _cameraController.SetTilt(_cameraTilt);
        }
        
        private void PerformWallJump()
        {
            Vector3 jumpDirection = (_wallNormal + Vector3.up).normalized;
            Vector3 jumpForce = jumpDirection * _wallJumpForce;
            jumpForce.y = _wallJumpUpForce;
            
            if (_context.Rigidbody != null)
            {
                _context.Rigidbody.linearVelocity = Vector3.zero;
                _context.Rigidbody.AddForce(jumpForce, ForceMode.VelocityChange);
            }
        }
        
        private void PerformWallClimb()
        {
            // Transition to wall climb action
            //_actionManager.RequestAction(ActionID.WallClimb);
        }
        
        public override bool CanStart(ActionContext context)
        {
            if (!base.CanStart(context)) return false;
            
            // Can only wall run when not grounded and moving
            if (context.IsGrounded) return false;
            if (context.MoveInput.magnitude < 0.1f) return false;
            
            // Check for nearby wall
            return FindWall(out _, out _);
        }
    }
    
    // ==============================================
    // VAULT ACTION
    // ==============================================
    
    [CreateAssetMenu(fileName = "VaultAction", menuName = "TPP/Actions/Vault")]
    public class VaultAction : BaseAction
    {
        [Header("Vault Settings")]
        [SerializeField] private float _vaultHeight = 1.2f;
        [SerializeField] private float _vaultSpeed = 5f;
        [SerializeField] private AnimationCurve _heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Detection")]
        [SerializeField] private float _obstacleCheckDistance = 1f;
        [SerializeField] private float _obstacleHeightRange = 0.5f;
        [SerializeField] private LayerMask _obstacleLayers;
        
        // Runtime
        private Vector3 _vaultStart;
        private Vector3 _vaultEnd;
        private float _vaultDistance;
        
        protected override void OnStart()
        {
            if (!FindVaultObstacle(out Vector3 obstaclePoint, out Vector3 landingPoint))
            {
                Complete();
                return;
            }
            
            _vaultStart = _context.PlayerTransform.position;
            _vaultEnd = landingPoint;
            _vaultDistance = Vector3.Distance(_vaultStart, _vaultEnd);
            
            _duration = _vaultDistance / _vaultSpeed;
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isActive) return;
            
            float t = _elapsedTime / _duration;
            
            // Calculate position
            Vector3 horizontalPos = Vector3.Lerp(_vaultStart, _vaultEnd, t);
            float height = _heightCurve.Evaluate(t) * _vaultHeight;
            Vector3 position = horizontalPos + Vector3.up * height;
            
            // Calculate speed
            float speedMultiplier = _speedCurve.Evaluate(t);
            Vector3 velocity = (_vaultEnd - _vaultStart).normalized * _vaultSpeed * speedMultiplier;
            
            // Move player
            if (_context.CharacterController != null)
            {
                _context.CharacterController.Move(position - _context.PlayerTransform.position);
            }
            else
            {
                _context.PlayerTransform.position = position;
            }
        }
        
        private bool FindVaultObstacle(out Vector3 obstaclePoint, out Vector3 landingPoint)
        {
            obstaclePoint = Vector3.zero;
            landingPoint = Vector3.zero;
            
            Vector3 origin = _context.PlayerTransform.position + Vector3.up * 0.5f;
            Vector3 direction = _context.PlayerTransform.forward;
            
            // Check for obstacle
            if (Physics.Raycast(origin, direction, out RaycastHit obstacleHit, 
                               _obstacleCheckDistance, _obstacleLayers))
            {
                obstaclePoint = obstacleHit.point;
                
                // Check landing point on other side
                Vector3 landingOrigin = obstaclePoint + direction * 0.5f + Vector3.up * _vaultHeight;
                if (Physics.Raycast(landingOrigin, Vector3.down, out RaycastHit landingHit, 
                                   _vaultHeight + 0.5f, _context.GroundLayers))
                {
                    landingPoint = landingHit.point;
                    return true;
                }
            }
            
            return false;
        }
        
        public override bool CanStart(ActionContext context)
        {
            if (!base.CanStart(context)) return false;
            
            // Need to be moving forward
            if (context.MoveInput.y <= 0.1f) return false;
            
            // Check for vaultable obstacle
            return FindVaultObstacle(out _, out _);
        }
    }
}