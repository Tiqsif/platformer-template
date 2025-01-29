using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerMovementStats movementStats;
    [SerializeField] private Collider2D _feetCollider;
    [SerializeField] private Collider2D _bodyCollider;

    private Rigidbody2D _rb;

    private Vector2 _moveVelocity;
    private bool isFacingRight = true;

    private RaycastHit2D _groundHit;
    private RaycastHit2D _headHit;
    private bool _isGrounded;
    private bool _isHeadBumped;

    private void Awake()
    {
        isFacingRight = true;
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        CollisionChecks();
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

    private void CollisionChecks()
    {
        isGrounded();
    }

    #endregion
}
