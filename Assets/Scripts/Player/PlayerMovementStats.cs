using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementStats", menuName = "Player/Player Movement Stats")]
public class PlayerMovementStats : ScriptableObject
{
    public bool showDebug;
    [Space]
    [Header("Walk")]
    [Range(0f, 1f)] public float moveThreshold = 0.25f;
    [Range(1f, 100f)] public float maxWalkSpeed = 15f;
    [Range(0.25f, 50f)] public float groundAcceleration = 5f;
    [Range(0.25f, 50f)] public float groundDeceleration = 5f;
    [Range(0.25f, 50f)] public float airAcceleration = 5f;
    [Range(0.25f, 50f)] public float airDeceleration = 5f;
    [Range(0.25f, 50f)] public float wallJumpMoveAcceleration = 5f;
    [Range(0.25f, 50f)] public float wallJumpMoveDeceleration = 5f;

    [Header("Collision Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.02f;
    public float headCheckDistance = 0.02f;
    [Range(0f,1f)] public float headWidth = 1f;
    public float wallCheckDistance = 0.125f;
    [Range(0.01f, 2f)] public float wallCheckHeight = 0.8f;

    [Header("Jump")]
    public float jumpHeight = 6f;
    [Range(1f,1.1f)] public float jumpHeightCompensation = 1.054f; // to adjust the jump height
    public float timeToJumpApex = 0.4f;
    [Range(0.01f, 5f)] public float gravityOnReleaseMultiplier = 2f;
    public float maxFallSpeed = 25f;
    [Range(1,5)] public int maxJumpCount = 2;

    [Header("ResetJumpOptions")]
    public bool resetJumpOnWallSlide = true;

    [Header("JumpCancel")]
    [Range(0.02f,0.3f)] public float timeForUpwardsCancel = 0.05f;

    [Header("JumpApex")]
    [Range(0.5f, 1f)] public float apexThreshold = 0.9f;
    [Range(0.01f,1f)] public float apexHangTime = 0.07f;

    [Header("JumpBuffer")]
    [Range(0.0f, 1f)] public float jumpBufferTime = 0.1f;

    [Header("CayoteTime")]
    [Range(0.0f, 1f)] public float cayoteTime = 0.1f;

    [Header("WallSlide")]
    [Min(0.01f)] public float wallSlideSpeed = 5f;
    [Range(0.25f, 50f)] public float wallSlideDecelerationSpeed = 50f;

    [Header("WallJump")]
    public Vector2 wallJumpDirection = new Vector2(-20f, 6.5f);
    [Range(0f, 1f)] public float wallJumpPostBufferTime = 0.125f;
    [Range(0.01f, 5f)] public float wallJumpGravityOnReleaseMultiplier = 1f;

    [Header("Dash")]
    public bool enableDash = true;
    [Range(0f, 1f)] public float dashTime = 0.11f;
    [Range(1f, 200f)] public float dashSpeed = 40f;
    [Range(0f, 1f)] public float timeBtwDashesOnGround = 0.225f; 
    public bool resetDashOnWallSlide = true; 
    [Range(0, 5)] public int numberOfDashes = 2;
    [Range(0f, 0.5f)] public float dashDiagonallyBias = 0.4f;
    
    [Header("Dash Cancel Time")]
    [Range(0.01f, 5f)] public float dashGravityOnReleaseMultiplier = 1f;
    [Range(0.02f, 0.3f)] public float dashTimeForUpwardsCancel = 0.027f;

    [Header("FallMode")]
    [Tooltip("FallMode changes the camera to show below the player")] public bool enableFallMode = true;
    [Range(0.1f, 20f)] public float fallModeRequiredTime = 2f;


    public readonly Vector2[] DashDirections = new Vector2[]
    {
        new Vector2(0,0), // no
        new Vector2(1,0), // right
        new Vector2(1,1).normalized, // top right
        new Vector2(0,1), // up
        new Vector2(-1,1).normalized, // top left
        new Vector2(-1,0), // left
        new Vector2(-1,-1).normalized, // bottom left
        new Vector2(0,-1), // down
        new Vector2(1,-1).normalized // bottom right
    };
    

    // for jump calculations
    public float Gravity { get; private set; }
    public float InitialJumpVelocity { get; private set; }
    public float AdjustedJumpHeight { get; private set; }

    // for wall jump calculations
    public float WallJumpGravity { get; private set; }
    public float InitialWallJumpVelocity { get; private set; }
    public float AdjustedWallJumpHeight { get; private set; }


    private void OnValidate()
    {
        CalculateValues();
    }

    private void OnEnable()
    {
        CalculateValues();
    }

    private void CalculateValues()
    {
        AdjustedJumpHeight = jumpHeight * jumpHeightCompensation;
        Gravity = -(2f * AdjustedJumpHeight) / Mathf.Pow(timeToJumpApex, 2f);
        InitialJumpVelocity = Mathf.Abs(Gravity) * timeToJumpApex;

        AdjustedWallJumpHeight = wallJumpDirection.y * jumpHeightCompensation;
        WallJumpGravity = -(2f * AdjustedWallJumpHeight) / Mathf.Pow(timeToJumpApex, 2f);
        InitialWallJumpVelocity = Mathf.Abs(WallJumpGravity) * timeToJumpApex;
    }

}
