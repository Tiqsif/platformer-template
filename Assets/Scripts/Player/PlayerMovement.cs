using Unity.Cinemachine;
using UnityEngine;

[SelectionBase]
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
    [SerializeField] private AudioClip _wallJumpSFX;
    [SerializeField] private AudioClip _landSFX;
    [SerializeField] private AudioClip _headBumpSFX;
    [SerializeField] private AudioClip _dashSFX;

    // movement
    public float HorizontalVelocity { get; private set; }
    private bool _isFacingRight = true;

    // collision
    private RaycastHit2D _groundHit;
    private RaycastHit2D _headHit;
    private RaycastHit2D _wallHit;
    private RaycastHit2D _lastWallHit;
    private bool _isGrounded;
    private bool _isHeadBumped;
    private bool _isTouchingWall;

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

    // wall slide
    private bool _isWallSliding;
    private bool _isWallSlideFalling;

    // wall jump
    private bool _useWallJumpMoveStats;
    private bool _isWallJumping;
    private float _wallJumpTime;
    private bool _isWallJumpFastFalling;
    private bool _isWallJumpFalling;
    private float _wallJumpFastFallTime;
    private float _wallJumpFastFallReleaseSpeed;

    private float _wallJumpPostBufferTimer;

    private float _wallJumpApexPoint;
    private float _timePastWallJumpApexThreshold;
    private bool _isPastWallJumpApexThreshold; 

    //dash vars 
    private bool _isDashing;
    private bool _isAirDashing;
    private float _dashTimer;
    private float _dashOnGroundTimer;
    private int _numberOfDashesUsed;
    private Vector2 _dashDirection;
    private bool _isDashFastFalling;
    private float _dashFastFallTime; 
    private float _dashFastFallReleaseSpeed;

    // falling from high and fallmode
    private float _fallHeight;
    private float _fallingTimer;

    private float _fallSpeedYDampingChangeThreshold;

    // --------------------------------------------------------------
    // events
    public delegate void onGroundedChanged(bool isGrounded);
    public static event onGroundedChanged GroundedChangedEvent;

    public delegate void onHeadBumped(bool isHeadBumped);
    public static event onHeadBumped HeadBumpedEvent;

    public delegate void onJumpStarted();
    public static event onJumpStarted JumpStartedEvent;
    public delegate void onFirstJumpStarted();
    public static event onFirstJumpStarted FirstJumpStartedEvent;

    public delegate void OnDoubleJumpStarted();
    public static event OnDoubleJumpStarted DoubleJumpStartedEvent;

    public delegate void OnLanded();
    public static event OnLanded LandedEvent;

    public delegate void OnWallSlideChange(bool isWallSliding);
    public static event OnWallSlideChange WallSlideChangeEvent;

    public delegate void OnWallJumpStarted();
    public static event OnWallJumpStarted WallJumpStartedEvent;

    public delegate void OnDashStarted();
    public static event OnDashStarted DashStartedEvent;

    public delegate void OnFellHigh();
    public static event OnFellHigh FellHighEvent;

    private void Awake()
    {
        _isFacingRight = true;
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
        LandCheck();
        WallJumpCheck();

        WallSlideCheck();
        DashCheck();

        #region Camera
        if (_rb.linearVelocityY < _fallSpeedYDampingChangeThreshold && !CameraManager.Instance.isLerpingYDamping && !CameraManager.Instance.lerpedFromPlayerFall)
        {
            CameraManager.Instance.LerpYDamping(true);
        }

        if(_rb.linearVelocityY >= 0f && !CameraManager.Instance.isLerpingYDamping && CameraManager.Instance.lerpedFromPlayerFall)
        {
            CameraManager.Instance.lerpedFromPlayerFall = false;
            CameraManager.Instance.LerpYDamping(false);
        }

        if (_rb.linearVelocityY <= 0 && _fallingTimer <=0f && !CameraManager.Instance.isLerpingLookAheadTime) // high falling mode
        {
            CameraManager.Instance.SetLookAheadTime(true);
        }
        else if (!CameraManager.Instance.isLerpingLookAheadTime)//if (_rb.linearVelocityY > 0 && !CameraManager.Instance.isLerpingLookAheadTime) // jumping
        {
            //if(!CameraManager.Instance.isLerpingYDamping) CameraManager.Instance.LerpYDamping(false);
            CameraManager.Instance.SetLookAheadTime(false);
        }
        _animationManager.Tick(_rb.linearVelocityX, movementStats.maxWalkSpeed, _isGrounded);
        #endregion
    }
    private void FixedUpdate()
    {
        CollisionChecks();

        // TODO: if not dashing, jump or move
        Jump();
        Fall();
        WallSlide();
        WallJump();
        Dash();

        if (_isGrounded)
        {
            Move(movementStats.groundAcceleration, movementStats.groundDeceleration, InputManager.Movement);
        }
        else
        {
            if (_useWallJumpMoveStats) // if welljumping, use walljump stats
            {
                Move(movementStats.wallJumpMoveAcceleration, movementStats.wallJumpMoveDeceleration, InputManager.Movement);
            }
            else
            {
                Move(movementStats.airAcceleration, movementStats.airDeceleration, InputManager.Movement);
            }
        }

        //-- apply velocity at the end of fixedupdate
        ApplyVelocity();
    }

    private void ApplyVelocity()
    {

        // clamp fallspeed
        if (!_isDashing)
        {
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, -movementStats.maxFallSpeed, 50f);
        }
        else
        {
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, -50f, 50f);
        }
        _rb.linearVelocity = new Vector2(HorizontalVelocity, VerticalVelocity);
    }

    #region Movement

    private void Move(float acceleration, float deceleration, Vector2 moveInput)
    {
        if (!_isDashing)
        {
            if (Mathf.Abs(moveInput.x) >= movementStats.moveThreshold)
            {
                TurnCheck(moveInput);
            
                float targetVelocity = 0f;
                if (InputManager.DashWasPressed)
                {
                    // execute dash
                }
                else
                {
                    targetVelocity = moveInput.x * movementStats.maxWalkSpeed;
                }
                HorizontalVelocity = Mathf.Lerp(HorizontalVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            
            }
            else if (Mathf.Abs(moveInput.x) < movementStats.moveThreshold)
            {
                HorizontalVelocity = Mathf.Lerp(HorizontalVelocity, 0f, deceleration * Time.fixedDeltaTime);
            }

        }
    }

    private void TurnCheck(Vector2 moveInput)
    {
        if (_isFacingRight && moveInput.x < 0)
        {
            Turn(false);

        }
        else if (!_isFacingRight && moveInput.x > 0)
        {
            Turn(true);
        }
    }

    private void Turn(bool turnRight)
    {
        if (turnRight)
        {
            _isFacingRight = true;
            transform.Rotate(0, 180, 0);
        }
        else
        {
            _isFacingRight = false;
            transform.Rotate(0, -180, 0);
        }

    }
    #endregion

    #region Land/Fall

    private void LandCheck()
    {
        // landed
        if ((_isJumping || _isFalling || _isWallJumpFalling || _isWallJumping || _isWallSlideFalling || _isWallSliding || _isDashFastFalling) && _isGrounded && VerticalVelocity <= 0f)
        {
            ResetJumpValues();
            StopWallSlide();
            ResetWallJumpValues();
            ResetDashes();

            _numberOfJumpsUsed = 0;

            VerticalVelocity = Physics2D.gravity.y;

            if (_isDashFastFalling && _isGrounded) // landing from a dash, ground dashing included
            {
                ResetDashValues();
            }
            else // normal landing
            {
                ResetDashValues();

                float fallDistance = _fallHeight - transform.position.y;
                if (fallDistance > movementStats.AdjustedJumpHeight * 1.5f)
                {
                    //Debug.Log("Fell from: " + fallDistance);
                    SFXManager.Instance.PlaySFX(_landSFX);
                    FellHighEvent?.Invoke();
                    CameraShakeManager.Instance.ShakeCamera(_impulseSource);

                }
                LandedEvent?.Invoke();

            }
            _fallHeight = transform.position.y;

        }
    }

    private void Fall()
    {
        // normal gravity while falling (not jumping)
        if (!_isGrounded && !_isJumping && !_isWallSliding && !_isWallJumping && !_isDashing && !_isDashFastFalling)
        {
            if (!_isFalling)
            {
                _isFalling = true;
            }

            VerticalVelocity += movementStats.Gravity * Time.fixedDeltaTime;
            _fallingTimer -= Time.fixedDeltaTime;
        }
    }
    #endregion

    #region Jump

    private void ResetJumpValues()
    {
        _isJumping = false;
        _isFalling = false;
        _isFastFalling = false;
        _fastFallTime = 0f;
        _isPastApexThreshold = false;
    
    }

    private void JumpChecks()
    {
        // jump pressed
        if (InputManager.JumpWasPressed) // if jumped; reset buffer timer, set released in buffer window to false
        {
            if (_isWallSlideFalling && _wallJumpPostBufferTimer >= 0f) // if true walljump will be called
            {
                return; // so dont go on with a normal jump
            }
            else if (_isWallSliding || (_isTouchingWall && !_isGrounded)) // walljump
            {
                return;
            }

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
            FirstJumpStartedEvent?.Invoke();
        }

        // double jump
        else if (_jumpBufferTimer > 0f && (_isJumping || _isWallJumping || _isWallSlideFalling || _isAirDashing || _isDashFastFalling) && !_isTouchingWall && _numberOfJumpsUsed < movementStats.maxJumpCount)
        {
            _isFastFalling = false;
            InitiateJump(1);
            CameraShakeManager.Instance.ShakeCamera(_impulseSource);
            SFXManager.Instance.PlaySFX(_doubleJumpSFX);
            DoubleJumpStartedEvent?.Invoke();
            if (_isDashFastFalling)
            {
                _isDashFastFalling = false;
            }
        }
        // airjump after cayote time lapsed
        else if (_jumpBufferTimer > 0f && _isFalling && !_isWallSlideFalling && _numberOfJumpsUsed < movementStats.maxJumpCount - 1)
        {
            InitiateJump(2);
            _isFastFalling = false;
            CameraShakeManager.Instance.ShakeCamera(_impulseSource);
            SFXManager.Instance.PlaySFX(_doubleJumpSFX);
            DoubleJumpStartedEvent?.Invoke();
        }
        
        

    }
    private void InitiateJump(int numberOfJumpsUsed)
    {
        if (!_isJumping)
        {
            _isJumping = true;
        }

        ResetWallJumpValues();

        _jumpBufferTimer = 0f;
        _numberOfJumpsUsed += numberOfJumpsUsed;
        VerticalVelocity = movementStats.InitialJumpVelocity;
        JumpStartedEvent?.Invoke();

        _fallingTimer = movementStats.fallModeRequiredTime;
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
                HeadBumpedEvent?.Invoke(true);

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
                else if (!_isFastFalling)
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

                _fallingTimer -= Time.fixedDeltaTime;
            }
            else if (VerticalVelocity < 0f) // if is jumping && verticalvel < 0 && fastfalling
            {
                if (!_isFalling) // if is jumping and verticalvel < 0 and not falling
                {
                    _isFalling = true;
                    // SET HEIGHEST POINT OF JUMP
                    _fallHeight = Mathf.Max(_fallHeight, transform.position.y);

                }
                _fallingTimer -= Time.fixedDeltaTime;
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
        
    }
    #endregion

    #region WallSlide
    
    private void WallSlideCheck() // update
    {
        if (_isTouchingWall && !_isGrounded && !_isDashing)
        {
            if (VerticalVelocity < 0f && !_isWallSliding)
            {
                ResetJumpValues();
                ResetWallJumpValues();
                ResetDashValues();

                if (movementStats.resetDashOnWallSlide)
                {
                    ResetDashes();
                
                }

                _isWallSlideFalling = false;
                _isWallSliding = true;

                if (movementStats.resetJumpOnWallSlide)
                {
                    _numberOfJumpsUsed = 0;
                }
            }
        }
        else if (_isWallSliding && !_isTouchingWall && !_isGrounded && !_isWallSlideFalling)
        {
            _isWallSlideFalling = true;
            StopWallSlide();
        }
        else
        {
            StopWallSlide();
        }
    }

    private void StopWallSlide()
    {
        if (_isWallSliding)
        {
            _numberOfJumpsUsed++; // if player falls off of a wall, it counts as the first jump, might change this
            _isWallSliding = false;
            WallSlideChangeEvent?.Invoke(false);
        }
    }
    private void WallSlide() //fixedupdate
    {
        if (_isWallSliding)
        {
            VerticalVelocity = Mathf.Lerp(VerticalVelocity, -movementStats.wallSlideSpeed, movementStats.wallSlideDecelerationSpeed * Time.fixedDeltaTime);
            _fallingTimer = movementStats.fallModeRequiredTime;

            WallSlideChangeEvent?.Invoke(true);
        }
    }


    #endregion

    #region WallJump

    private void WallJumpCheck()
    {
        if (ShouldApplyPostWallJumpBuffer())
        {
            _wallJumpPostBufferTimer = movementStats.wallJumpPostBufferTime;

        }

        // walljump fastfalling
        if (InputManager.JumpWasReleased && !_isWallSliding && !_isTouchingWall && _isWallJumping)
        {
            if (VerticalVelocity > 0f)
            {
                if (_isPastWallJumpApexThreshold)
                {
                    _isPastWallJumpApexThreshold = false;
                    _isWallJumpFastFalling = true;
                    _wallJumpFastFallTime = movementStats.timeForUpwardsCancel;

                    VerticalVelocity = 0f;
                }
                else
                {
                    _isWallJumpFastFalling = true;
                    _wallJumpFastFallReleaseSpeed = VerticalVelocity;
                }
            }
        }

        // actual jump with post walljump buffer time
        if (InputManager.JumpWasPressed && _wallJumpPostBufferTimer > 0f)
        {
            InitiateWallJump();
            SFXManager.Instance.KillAndPlaySFX(_wallJumpSFX, volume:0.8f);

        }

    }

    private void InitiateWallJump()
    {
        if (!_isWallJumping)
        {
            _isWallJumping = true;
            _useWallJumpMoveStats = true;
        }
        StopWallSlide();
        ResetJumpValues();


        VerticalVelocity = movementStats.InitialWallJumpVelocity;

        int dirMultiplier = 0;
        Vector2 hitPoint = _lastWallHit.collider.ClosestPoint(_bodyCollider.bounds.center); // last wall hit if not touching at the moment and using buffer time
        
        if (hitPoint.x > transform.position.x)
        {
            dirMultiplier = -1;
        }
        else
        {
            dirMultiplier = 1;
        }

        HorizontalVelocity = Mathf.Abs(movementStats.wallJumpDirection.x) * dirMultiplier;

        _fallingTimer = movementStats.fallModeRequiredTime;

        WallJumpStartedEvent?.Invoke();
    }

    private void WallJump()
    {
        // apply walljump gravity
        if (_isWallJumping)
        {
            // time to takeover movement controls while walljumping
            _wallJumpTime += Time.fixedDeltaTime;
            if (_wallJumpTime >= movementStats.timeToJumpApex)
            {
                _useWallJumpMoveStats = false;
            }

            // hit head while walljumping
            if (_isHeadBumped)
            {
                _isWallJumpFastFalling = true;
                _useWallJumpMoveStats = false;
            }

            // gravity on ascending
            if (VerticalVelocity >= 0f)
            {
                // apex controls
                _wallJumpApexPoint = Mathf.InverseLerp(movementStats.wallJumpDirection.y, 0f, VerticalVelocity);

                if (_wallJumpApexPoint >= movementStats.apexThreshold)
                {
                    if (!_isPastWallJumpApexThreshold)
                    {
                        _isPastWallJumpApexThreshold = true;
                        _timePastWallJumpApexThreshold = 0f;
                    }
                    if (_isPastWallJumpApexThreshold)
                    {
                        _timePastWallJumpApexThreshold += Time.deltaTime;
                        if (_timePastWallJumpApexThreshold < movementStats.apexHangTime)
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
                else if (!_isWallJumpFastFalling)
                {
                    VerticalVelocity += movementStats.WallJumpGravity * Time.fixedDeltaTime;
                    if (_isPastWallJumpApexThreshold)
                    {
                        _isPastWallJumpApexThreshold = false;
                    }
                }
            }
            // gravity on descending
            else if (!_isWallJumpFastFalling)
            {
                VerticalVelocity += movementStats.WallJumpGravity * Time.fixedDeltaTime;
                _fallingTimer -= Time.fixedDeltaTime;
            }
            else if (VerticalVelocity < 0f)
            {
                if (!_isWallJumpFalling)
                {
                    _isWallJumpFalling = true;
                }
                _fallingTimer -= Time.fixedDeltaTime;
            }
        }

        // hande walljump cut time
        if (_isWallJumpFastFalling)
        {
            if (_wallJumpFastFallTime >= movementStats.timeForUpwardsCancel)
            {
                VerticalVelocity += movementStats.WallJumpGravity * movementStats.wallJumpGravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if (_wallJumpFastFallTime < movementStats.timeForUpwardsCancel)
            {
                VerticalVelocity = Mathf.Lerp(_wallJumpFastFallReleaseSpeed, 0f, _wallJumpFastFallTime / movementStats.timeForUpwardsCancel);
            }
            _wallJumpFastFallTime += Time.fixedDeltaTime;
        }
    }

    private bool ShouldApplyPostWallJumpBuffer()
    {
        if (!_isGrounded && (_isTouchingWall || _isWallSliding))
        {
            return true;
        }
        return false;
    }

    private void ResetWallJumpValues()
    {
        _isWallSlideFalling = false;
        _useWallJumpMoveStats = false;
        _isWallJumping = false;
        _isWallJumpFastFalling = false;
        _isWallJumpFalling = false;
        _isPastWallJumpApexThreshold = false;

        _wallJumpFastFallTime = 0f;
        _wallJumpTime = 0f;
    
    }

    #endregion

    #region Dash

    private void DashCheck()
    {
        if (!movementStats.enableDash)
        {
            return;
        }
        if (InputManager.DashWasPressed)
        {
            // ground dash
            if (_isGrounded && _dashOnGroundTimer < 0 && !_isDashing)
            {

                SFXManager.Instance.KillAndPlaySFX(_dashSFX);

                InitiateDash();

            }
            // air dash
            else if (!_isGrounded && !_isDashing && _numberOfDashesUsed < movementStats.numberOfDashes)
            {
                _isAirDashing = true;

                SFXManager.Instance.KillAndPlaySFX(_dashSFX);

                InitiateDash();

                // left a wallslide but dashed withing the walljump buffer time
                if (_wallJumpPostBufferTimer > 0f)
                {
                    _numberOfJumpsUsed--; // when leaving a wallslide, it counts as a jump, but when dashing, it should not
                    _numberOfJumpsUsed = Mathf.Max(0, _numberOfJumpsUsed);
                }
            }
        }
    }

    private void InitiateDash()
    {

        if(!movementStats.enableDash)
        {
            return;
        }

        _dashDirection = InputManager.Movement;

        Vector2 closestDirection = Vector2.zero;
        float minDistance = Vector2.Distance(_dashDirection, movementStats.DashDirections[0]);
        for (int i = 0; i < movementStats.DashDirections.Length; i++)
        {
            // if its exactly matching one of the dash directions
            if (_dashDirection == movementStats.DashDirections[i])
            {
                closestDirection = _dashDirection;
                break;
            }
            float distance = Vector2.Distance(_dashDirection, movementStats.DashDirections[i]);

            // if diagonal dir, apply bias
            bool isDiagonal = Mathf.Abs(movementStats.DashDirections[i].x) == 1 && Mathf.Abs(movementStats.DashDirections[i].y) == 1;
            if (isDiagonal)
            {
                distance -= movementStats.dashDiagonallyBias;
            }
            else if (distance < minDistance)
            {
                minDistance = distance;
                closestDirection = movementStats.DashDirections[i];
            }
        }

        // if no direction input / no movement
        if (closestDirection == Vector2.zero)
        {
            if (_isFacingRight)
            {
                closestDirection = Vector2.right;
            }
            else
            {
                closestDirection = Vector2.left;
            }
        }

        _dashDirection = closestDirection;
        _numberOfDashesUsed++;
        _isDashing = true;
        _dashTimer = 0f;
        _dashOnGroundTimer = movementStats.timeBtwDashesOnGround;

        _fallingTimer = movementStats.fallModeRequiredTime;


        ResetJumpValues();
        ResetWallJumpValues();
        StopWallSlide();

        DashStartedEvent?.Invoke();
    }
    private void Dash()
    {
        if (!movementStats.enableDash)
        {
            return;
        }

        // gravity and all that

        if (_isDashing)
        {
            // stop the dash after timer
            _dashTimer += Time.fixedDeltaTime;
            if (_dashTimer >= movementStats.dashTime)
            {
                if (_isGrounded)
                {
                    ResetDashes();
                }

                _isDashing = false;
                _isAirDashing = false;

                if (!_isJumping && !_isWallJumping)
                {
                    _dashFastFallTime = 0f;
                    _dashFastFallReleaseSpeed = VerticalVelocity;
                    if (!_isGrounded)
                    {
                        _isDashFastFalling = true;
                    }
                }

                return; // dash timer is over, exit
            }

            HorizontalVelocity = movementStats.dashSpeed * _dashDirection.x;
            if (_dashDirection.y != 0f || _isAirDashing)
            {
                VerticalVelocity = movementStats.dashSpeed * _dashDirection.y;
            }
        }
        // handle dash cut time (fastfall)
        else if (_isDashFastFalling)
        {
            if (VerticalVelocity > 0f)
            {
                if (_dashFastFallTime < movementStats.dashTimeForUpwardsCancel)
                {
                    VerticalVelocity = Mathf.Lerp(_dashFastFallReleaseSpeed, 0f, _dashFastFallTime / movementStats.dashTimeForUpwardsCancel);
                }
                else if (_dashFastFallTime >= movementStats.dashTimeForUpwardsCancel)
                {
                    VerticalVelocity += movementStats.Gravity * movementStats.dashGravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }

                _dashFastFallTime += Time.fixedDeltaTime;
            }
            else
            {
                VerticalVelocity += movementStats.Gravity * movementStats.dashGravityOnReleaseMultiplier * Time.fixedDeltaTime;
                _fallingTimer -= Time.fixedDeltaTime;
            }
        }
    }
    private void ResetDashValues()
    {
        _isDashFastFalling = false;
        _dashOnGroundTimer = -0.01f;
    }

    private void ResetDashes()
    {
        _numberOfDashesUsed = 0;
    }

    #endregion

    #region Timers
    private void CountTimers()
    {
        // jump buffer
        _jumpBufferTimer -= Time.deltaTime;

        // jump cayote time
        if (!_isGrounded)
        {
            _coyoteTimer -= Time.deltaTime;
        }
        else
        {
            _coyoteTimer = movementStats.cayoteTime;
            _fallingTimer = movementStats.fallModeRequiredTime;
        }

        // walljump buffer timer
        if (!ShouldApplyPostWallJumpBuffer())
        {
            _wallJumpPostBufferTimer -= Time.deltaTime;
        }

        // dash on ground timer
        if (_isGrounded)
        {
            _dashOnGroundTimer -= Time.deltaTime;
        }
    }
    #endregion

    #region Collision Check

    private void IsGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
        Vector2 boxCastSize = new Vector2(_feetCollider.bounds.size.x, movementStats.groundCheckDistance);

        _groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, movementStats.groundCheckDistance, movementStats.groundLayer);
        if (_groundHit.collider != null)
        {
            if (!_isGrounded) // if was not grounded and now is
            {
                GroundedChangedEvent?.Invoke(true);
            }
            _isGrounded = true;
        }
        else
        {
            if (_isGrounded) // if was grounded and now is not
            {
                GroundedChangedEvent?.Invoke(false);
            }
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

    private void IsHeadBumped()
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

    private void IsTouchingWall()
    {
        float originEndPoint = 0f;
        if (_isFacingRight)
        {
            originEndPoint = _bodyCollider.bounds.max.x;
        }
        else
        {
            originEndPoint = _bodyCollider.bounds.min.x;
        }

        float adjustedHeight = _bodyCollider.bounds.size.y * movementStats.wallCheckHeight;

        Vector2 boxCastOrigin = new Vector2(originEndPoint, _bodyCollider.bounds.center.y);
        Vector2 boxCastSize = new Vector2(movementStats.wallCheckDistance, adjustedHeight);

        _wallHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, transform.right, movementStats.wallCheckDistance, movementStats.groundLayer);

        if (_wallHit.collider != null)
        {
            _lastWallHit = _wallHit;
            _isTouchingWall = true;
        }
        else
        {
            _isTouchingWall = false;
        }

        #region Debug Visualization
        if (movementStats.showDebug)
        {
            Color rayColor = _isTouchingWall ? Color.green : Color.red;

            Vector2 boxBottomLeft = new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - boxCastSize.y / 2);
            Vector2 boxBottomRight = new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y - boxCastSize.y / 2);
            Vector2 boxTopLeft = new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y + boxCastSize.y / 2);
            Vector2 boxTopRight = new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y + boxCastSize.y / 2);

            Debug.DrawLine(boxBottomLeft, boxBottomRight, rayColor);
            Debug.DrawLine(boxBottomRight, boxTopRight, rayColor);
            Debug.DrawLine(boxTopRight, boxTopLeft, rayColor);
            Debug.DrawLine(boxTopLeft, boxBottomLeft, rayColor);

        }
        #endregion
    }

    private void CollisionChecks() // fixedupdate
    {
        IsGrounded();
        IsHeadBumped();
        IsTouchingWall();
    }

    #endregion
}
