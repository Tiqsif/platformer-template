using Unity.Cinemachine;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerMovementStats movementStats;
    [SerializeField] private Collider2D _feetCollider;
    [SerializeField] private Collider2D _bodyCollider;
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField] private PlayerAnimationManager _animationManager;
    private Rigidbody2D _rb;

    [Header("AudioClips")]
    [SerializeField] private AudioClip _jumpSFX;
    [SerializeField] private AudioClip _doubleJumpSFX;
    [SerializeField] private AudioClip _landSFX;
    [SerializeField] private AudioClip _headBumpSFX;
    [SerializeField] private AudioClip _dashSFX;

    // movement
    private Vector2 _moveVelocity;
    private bool isFacingRight = true;

    // collision
    private RaycastHit2D _groundHit;
    private RaycastHit2D _headHit;
    private bool _isGrounded;
    private bool _isHeadBumped;

    // jump
    public float VerticalVelocity { get; private set; }
    private bool _isJumping;
    private bool _isFastFalling;
    private bool _isFalling;
    private float _fastFallTime;
    private float _fastFallReleaseSpeed;
    private int _numberOfJumpsUsed;

    // jump apex
    private float _apexPoint;
    private float _timePastApexThreshold;
    private bool _isPastApexThreshold;
    
    // jump buffer
    private float _jumpBufferTimer;
    private bool _jumpReleasedDuringBuffer; 
     
    // cayote time
    private float _coyoteTimer;

    private float _fallHeight;

    private float _fallSpeedYDampingChangeThreshold;

    private void Awake()
    {
        isFacingRight = true;
        _rb = GetComponent<Rigidbody2D>();
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        _animationManager = GetComponent<PlayerAnimationManager>();
    }
    private void Start()
    {
        _fallSpeedYDampingChangeThreshold = CameraManager.Instance.fallSpeedYDampingChangeThreshold;
    }

    private void Update()
    {
        CountTimers();
        JumpChecks();

        if (_rb.linearVelocityY < _fallSpeedYDampingChangeThreshold && !CameraManager.Instance.isLerpingYDamping && !CameraManager.Instance.lerpedFromPlayerFall)
        {
            CameraManager.Instance.LerpYDamping(true);
        }

        if(_rb.linearVelocityY >= 0f && !CameraManager.Instance.isLerpingYDamping && CameraManager.Instance.lerpedFromPlayerFall)
        {
            CameraManager.Instance.lerpedFromPlayerFall = false;
            CameraManager.Instance.LerpYDamping(false);
        }
        _animationManager.Tick(_rb.linearVelocityX, movementStats.maxWalkSpeed, _isGrounded);
    }
    private void FixedUpdate()
    {
        CollisionChecks();

        // TODO: if not dashing, jump or move
        Jump();
        if (_isGrounded)
        {
            Move(movementStats.groundAcceleration, movementStats.groundDeceleration, InputManager.Movement);
        }
        else
        {
            Move(movementStats.airAcceleration, movementStats.airDeceleration, InputManager.Movement);
        }
    }

    #region Movement

    private void Move(float acceleration, float deceleration, Vector2 moveInput)
    {
        if (moveInput != Vector2.zero)
        {
            TurnCheck(moveInput);
            
            Vector2 targetVelocity = Vector2.zero;
            if (InputManager.DashWasPressed)
            {
                // execute dash
            }
            else
            {
                targetVelocity = new Vector2(moveInput.x, 0) * movementStats.maxWalkSpeed;
            }
            _moveVelocity = Vector2.Lerp(_moveVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(_moveVelocity.x, _rb.linearVelocity.y);
            
        }
        else
        {
            _moveVelocity = Vector2.Lerp(_moveVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(_moveVelocity.x, _rb.linearVelocity.y);
        }
    }

    private void TurnCheck(Vector2 moveInput)
    {
        if (isFacingRight && moveInput.x < 0)
        {
            Turn(false);

        }
        else if (!isFacingRight && moveInput.x > 0)
        {
            Turn(true);
        }
    }

    private void Turn(bool turnRight)
    {
        if (turnRight)
        {
            isFacingRight = true;
            transform.Rotate(0, 180, 0);
        }
        else
        {
            isFacingRight = false;
            transform.Rotate(0, -180, 0);
        }

    }
    #endregion

    #region Jump
    private void JumpChecks()
    {
        // jump pressed
        if (InputManager.JumpWasPressed) // if jumped; reset buffer timer, set released in buffer window to false
        {
            _jumpBufferTimer = movementStats.jumpBufferTime;
            _jumpReleasedDuringBuffer = false;
        }
        // jump released
        if (InputManager.JumpWasReleased) // if released; set "released in buffer window" to true
        {
            if (_jumpBufferTimer > 0)
            {
                _jumpReleasedDuringBuffer = true;
            }

            if (_isJumping && VerticalVelocity > 0f) // (key released) if already jumping and still ascending
            {
                if (_isPastApexThreshold) // if reached the peak of jump, initiate fastfall, set veritcalvelocity to 0
                {
                    _isPastApexThreshold = false;
                    _isFastFalling = true;
                    _fastFallTime = movementStats.timeForUpwardsCancel; // time it takes to change from upwards to downwards
                    VerticalVelocity = 0f;
                }
                else // BEFORE reaching the peak of jump, initiate fastfall, set fastfallreleasespeed
                {
                    _isFastFalling = true;
                    _fastFallReleaseSpeed = VerticalVelocity;
                }
            }
        }

        // jump with buffer and coyote time
        if (_jumpBufferTimer > 0f && !_isJumping && (_isGrounded || _coyoteTimer > 0f))
        {
            InitiateJump(1);
            if (_jumpReleasedDuringBuffer)
            {
                _isFastFalling = true;
                _fastFallReleaseSpeed = VerticalVelocity;
            }
            SFXManager.Instance.PlaySFX(_jumpSFX);
        }

        // double jump
        else if (_jumpBufferTimer > 0f && _isJumping && _numberOfJumpsUsed < movementStats.maxJumpCount)
        {
            _isFastFalling = false;
            InitiateJump(1);
            CameraShakeManager.Instance.ShakeCamera(_impulseSource);
            SFXManager.Instance.PlaySFX(_doubleJumpSFX);
        }
        // airjump after cayote time lapsed
        else if (_jumpBufferTimer > 0f && _isFalling && _numberOfJumpsUsed < movementStats.maxJumpCount - 1)
        {
            InitiateJump(2);
            _isFastFalling = false;
            CameraShakeManager.Instance.ShakeCamera(_impulseSource);
            SFXManager.Instance.PlaySFX(_doubleJumpSFX);
        }
        
        // landed
        if ((_isJumping || _isFalling) && _isGrounded && VerticalVelocity <= 0f)
        {
            _isFalling = false;
            _isJumping = false;
            _isFastFalling = false;
            _fastFallTime = 0f;
            _isPastApexThreshold = false;
            _numberOfJumpsUsed = 0;

            VerticalVelocity = Physics2D.gravity.y;
            float fallDistance = _fallHeight - transform.position.y;
            if (fallDistance > movementStats.AdjustedJumpHeight * 1.5f)
            {
                //Debug.Log("Fell from: " + fallDistance);
                SFXManager.Instance.PlaySFX(_landSFX);
            }
            _fallHeight = transform.position.y;

        }

    }
    private void InitiateJump(int numberOfJumpsUsed)
    {
        if (!_isJumping)
        {
            _isJumping = true;
        }
        _jumpBufferTimer = 0f;
        _numberOfJumpsUsed += numberOfJumpsUsed;
        VerticalVelocity = movementStats.InitialJumpVelocity;
    }
    private void Jump()
    {
        //Debug.Log("PlayerMovement:Jump()");
        // apply gravity while jumping
        if (_isJumping)
        {
            // check headbump
            if (_isHeadBumped)
            {
                _isFastFalling = true;

                Vector3 impulseDefaultVelocity = _impulseSource.DefaultVelocity;
                _impulseSource.DefaultVelocity = new Vector3(0.12f, 0f, 0f);
                CameraShakeManager.Instance.ShakeCamera(_impulseSource);
                _impulseSource.DefaultVelocity = impulseDefaultVelocity;

                SFXManager.Instance.PlaySFX(_headBumpSFX);

            }
            // gravity on ascending
            if (VerticalVelocity >= 0f)
            {
                // apex controls
                _apexPoint = Mathf.InverseLerp(movementStats.InitialJumpVelocity, 0f, VerticalVelocity);

                if (_apexPoint >= movementStats.apexThreshold)
                {
                    if (!_isPastApexThreshold)
                    {
                        _isPastApexThreshold = true;
                        _timePastApexThreshold = 0f;
                    }
                    if (_isPastApexThreshold)
                    {
                        _timePastApexThreshold += Time.deltaTime;
                        if (_timePastApexThreshold < movementStats.apexHangTime)
                        {
                            VerticalVelocity = 0f;
                        }
                        else
                        {
                            VerticalVelocity = -0.01f;
                        }
                    }
                }

                // gravity on ascending but not past apex threshold
                else
                {
                    VerticalVelocity += movementStats.Gravity * Time.fixedDeltaTime;
                    if (_isPastApexThreshold)
                    {
                        _isPastApexThreshold = false;
                    }
                }
            }
            // gravity on descending
            else if (!_isFastFalling) // if is jumping && verticalvel < 0 && not fastfalling
            {
                VerticalVelocity += movementStats.Gravity * movementStats.gravityOnReleaseMultiplier * Time.fixedDeltaTime;
                // SET HEIGHEST POINT OF JUMP
                _fallHeight = Mathf.Max(_fallHeight, transform.position.y);
            }
            else if (VerticalVelocity < 0f) // if is jumping && verticalvel < 0 && fastfalling
            {
                if (!_isFalling) // if is jumping and verticalvel < 0 and not falling
                {
                    _isFalling = true;
                    // SET HEIGHEST POINT OF JUMP
                    _fallHeight = Mathf.Max(_fallHeight, transform.position.y);
                }
            }
        }

        // jumpcut
        if (_isFastFalling)
        {
            if (_fastFallTime >= movementStats.timeForUpwardsCancel)
            {
                VerticalVelocity += movementStats.Gravity * movementStats.gravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if (_fastFallTime < movementStats.timeForUpwardsCancel)
            {
                VerticalVelocity = Mathf.Lerp(_fastFallReleaseSpeed, 0f, _fastFallTime / movementStats.timeForUpwardsCancel);
            }
            _fastFallTime += Time.fixedDeltaTime;
        }
        // normal gravity while falling (not jumping)
        if (!_isGrounded && !_isJumping)
        {
            if (!_isFalling)
            {
                _isFalling = true;
            }

            VerticalVelocity += movementStats.Gravity * Time.fixedDeltaTime;
        }

        // clamp fallspeed
        VerticalVelocity = Mathf.Clamp(VerticalVelocity, -movementStats.maxFallSpeed, 50f);
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, VerticalVelocity);
    }
    #endregion

    #region Timers
    private void CountTimers()
    {
        _jumpBufferTimer -= Time.deltaTime;
        if (!_isGrounded)
        {
            _coyoteTimer -= Time.deltaTime;
        }
        else
        {
            _coyoteTimer = movementStats.cayoteTime;
        }
    }
    #endregion

    #region Collision Check

    private void isGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
        Vector2 boxCastSize = new Vector2(_feetCollider.bounds.size.x, movementStats.groundCheckDistance);

        _groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, movementStats.groundCheckDistance, movementStats.groundLayer);
        if (_groundHit.collider != null)
        {
            _isGrounded = true;
        }
        else
        {
            _isGrounded = false;
        }

        #region Debug Visualization
        if (movementStats.showDebug)
        {
            Color rayColor = _isGrounded ? Color.green : Color.red;

            Debug.DrawRay(boxCastOrigin + new Vector2(-boxCastSize.x / 2, 0), Vector2.down * movementStats.groundCheckDistance, rayColor); // left
            Debug.DrawRay(boxCastOrigin + new Vector2(boxCastSize.x / 2, 0), Vector2.down * movementStats.groundCheckDistance, rayColor); // right
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x/2, boxCastOrigin.y - movementStats.groundCheckDistance), Vector2.right * boxCastSize.x, rayColor); // down from left to right
        }
        #endregion
    }

    private void isHeadBumped()
    {
        Vector2 boxCastOrigin = new Vector2(_feetCollider.bounds.center.x, _bodyCollider.bounds.max.y);
        Vector2 boxCastSize = new Vector2(_feetCollider.bounds.size.x * movementStats.headWidth, movementStats.headCheckDistance);

        _headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, movementStats.headCheckDistance, movementStats.groundLayer);

        if (_headHit.collider != null)
        {
            _isHeadBumped = true;
        }
        else { _isHeadBumped = false; }

        #region Debug Visualization

        if (movementStats.showDebug)
        {
            float headWidth = movementStats.headWidth;

            Color rayColor;
            if (_isHeadBumped)
            {
                rayColor = Color.green;
            }
            else { rayColor = Color.red; }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth, boxCastOrigin.y), Vector2.up * movementStats.headCheckDistance, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + (boxCastSize.x / 2) * headWidth, boxCastOrigin.y), Vector2.up * movementStats.headCheckDistance, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth, boxCastOrigin.y + movementStats.headCheckDistance), Vector2.right * boxCastSize.x * headWidth, rayColor);
        }

        #endregion
    }


    private void CollisionChecks()
    {
        isGrounded();
        isHeadBumped();
    }

    #endregion
}
