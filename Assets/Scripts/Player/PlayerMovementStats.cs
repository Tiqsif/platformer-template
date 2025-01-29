using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementStats", menuName = "Player/Player Movement Stats")]
public class PlayerMovementStats : ScriptableObject
{
    public bool showDebug;
    [Space]
    [Header("Walk")]
    [Range(1f, 100f)] public float maxWalkSpeed = 15f;
    [Range(0.25f, 50f)] public float groundAcceleration = 5f;
    [Range(0.25f, 50f)] public float groundDeceleration = 5f;
    [Range(0.25f, 50f)] public float airAcceleration = 5f;
    [Range(0.25f, 50f)] public float airDeceleration = 5f;

    [Header("Collision Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.02f;
    public float headCheckDistance = 0.02f;
    [Range(0f,1f)] public float headWidth = 1f;

    [Header("Jump")]
    public float jumpHeight = 6f;
    [Range(1f,1.1f)] public float jumpHeightCompensation = 1.054f;
    public float timeToJumpApex = 0.4f;
    [Range(0.01f, 5f)] public float gravityOnReleaseMultiplier = 2f;
    public float maxFallSpeed = 25f;
    [Range(1,5)] public int maxJumpCount = 2;

    [Header("JumpCancel")]
    [Range(0.02f,0.3f)] public float jumpCancelWindow = 0.05f;

    [Header("JumpApex")]
    [Range(0.5f, 1f)] public float apexThreshold = 0.9f;
    [Range(0.01f,1f)] public float apexHangTime = 0.07f;

    [Header("JumpBuffer")]
    [Range(0.0f, 1f)] public float jumpBufferTime = 0.1f;

    [Header("CayoteTime")]
    [Range(0.0f, 1f)] public float cayoteTime = 0.1f;

    [Header("JumpVisualization")]
    public bool showArc;
    public bool stopOnCollision;
    public bool drawRight;
    [Range(5,100)] public int arcResolution = 20;
    [Range(0,500)] public int visualizationSteps = 90;

    public float Gravity { get; private set; }
    public float InitialJumpVelocity { get; private set; }
    public float AdjustedJumpHeight { get; private set; }


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
    }

}
