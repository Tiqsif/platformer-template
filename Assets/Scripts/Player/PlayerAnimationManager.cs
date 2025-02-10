using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private Transform _playerBody;
    [SerializeField] private AudioClip _playerFootsteps;
    [SerializeField] private AudioClip _blinkClip;
    [SerializeField] private Transform _frontEye;
    [SerializeField] private Transform _backEye;


    [Header("Bobbing Values")]
    [SerializeField] [Range(1f,10f)] private float _bobbingSpeed = 2f;
    [SerializeField] private AnimationCurve _bobbingCurve; // Define the curve in the Inspector
    [SerializeField] private float _bobbingAmplitude = 0.1f;
    private float _bobbingTime; // Keeps track of time

    public float maxTiltAngle = 10f;


    // values for eye animations
    private EyeState _eyeState = EyeState.Static;
    private Coroutine _eyeCoroutine;
    private bool _isEyeAnimating = false;

    private Vector3 _frontEyeStartPos;
    private Quaternion _frontEyeStartRotation;
    private Vector3 _frontEyeStartScale;

    private Vector3 _backEyeStartPos;
    private Quaternion _backEyeStartRotation;
    private Vector3 _backEyeStartScale;

    [Header("Eye Animation Values")]
    [SerializeField] private Vector2 _BlinkIntervalMinMax = new Vector2(1.5f, 3.5f); 
    [SerializeField] private float _squintThreshold = 1.5f;  // Time before squinting

    private float _BlinkTimer;
    private float _NextBlinkTime;
    private float _moveTimer;
    private bool _isGrounded;
    private bool _isHeadBumped;


    // body squash and stretch values local!
    private Vector3 _bodyStartScale;
    private Vector3 _bodyStartPos;
    [SerializeField] private float _bodyDeformTime = 0.4f;
    [SerializeField] private float _bodyDeformPercentage = 0.2f;


    private void OnEnable()
    {
        PlayerMovement.GroundedChangedEvent += OnGroundedChanged;
        PlayerMovement.HeadBumpedEvent += OnHeadBumped;
        PlayerMovement.JumpStartedEvent += JumpAnimation;
        PlayerMovement.WallJumpStartedEvent += JumpAnimation;
        PlayerMovement.FellHighEvent += LandAnimation;
    }

    private void OnDisable()
    {
        PlayerMovement.GroundedChangedEvent -= OnGroundedChanged;
        PlayerMovement.HeadBumpedEvent -= OnHeadBumped;
        PlayerMovement.JumpStartedEvent -= JumpAnimation;
        PlayerMovement.WallJumpStartedEvent -= JumpAnimation;
        PlayerMovement.FellHighEvent -= LandAnimation;
    }
    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();


        _frontEyeStartPos = _frontEye.localPosition;
        _frontEyeStartRotation = _frontEye.localRotation;
        _frontEyeStartScale = _frontEye.localScale;

        _backEyeStartPos = _backEye.localPosition;
        _backEyeStartRotation = _backEye.localRotation;
        _backEyeStartScale = _backEye.localScale;

        _bodyStartScale = _playerBody.localScale;
    }

    public void Tick(float velX, float velXMax, bool isGrounded) // called from PlayerMovement
    {
        // calculate tilt angle by lerping tilt angle from 0 to max tilt angle
        float tiltAngle = Mathf.Lerp(0, maxTiltAngle, Mathf.Abs(velX / velXMax));
        _playerBody.localRotation = Quaternion.Euler(0, 0, tiltAngle);

        if (Mathf.Abs(velX/velXMax) >= 0.5f && isGrounded)
        {
            float tweenSpeed = Mathf.Pow(2, Mathf.Abs(velX / velXMax)) * _bobbingSpeed;
            _bobbingTime += Time.deltaTime * tweenSpeed;
            float loopedTime = Mathf.Repeat(_bobbingTime, 1f);

            // Evaluate the animation curve
            float bobbingVal = _bobbingCurve.Evaluate(loopedTime);

            // Apply to position
            _playerBody.localPosition = new Vector3(0, bobbingVal * _bobbingAmplitude, 0);

            // Play footsteps sound when reaching the bottom
            if (bobbingVal <= 0.1f)
            {
                SFXManager.Instance.KillAndPlaySFX(_playerFootsteps, volume:0.15f);
            }
        }
        

        if (Mathf.Abs(velX / velXMax) < 0.1f)
        {
            _playerBody.localPosition = Vector3.zero;
        }



    }

    private void Update()
    {
        EyeAnimationsTick();
    }

    #region EyeAnimations
    private enum EyeState
    {
        Blink,
        Static,
        Squint,
        Wide
    }
    private void EyeAnimationsTick()
    {
        bool isHorizontalMoving = Mathf.Abs(_playerMovement.HorizontalVelocity) > 0.1f;
        bool isVerticalMax = Mathf.Abs(_playerMovement.VerticalVelocity) >= _playerMovement.movementStats.maxFallSpeed * 0.5f;

        if (_isHeadBumped)
        {
            SetEyeState(EyeState.Blink);
            return;
        }

        if (isVerticalMax)
        {
            _moveTimer = 0f; // Reset movement timer when falling
            _BlinkTimer = 0f;  // Reset Blink timer
            SetEyeState(EyeState.Wide);
            return;
        }
        

        if (isHorizontalMoving)
        {
            _BlinkTimer = 0f; // Reset Blink timer
            _moveTimer += Time.deltaTime;

            if (_moveTimer >= _squintThreshold)
            {
                SetEyeState(EyeState.Squint);
                //_eyeAnimator.Play("SquintEyes");
            }
            else
            {
                SetEyeState(EyeState.Static);
                //StartCoroutine(ResetEyes());
            }
        }
        else
        {
            _moveTimer = 0f; // Reset movement timer
            _BlinkTimer += Time.deltaTime;

            if (_BlinkTimer >= _NextBlinkTime)
            {
                SFXManager.Instance.KillAndPlaySFX(_blinkClip, volume: 0.15f);
                SetEyeState(EyeState.Blink);

                //StartCoroutine(ResetEyes());
                //_eyeAnimator.Play("Blink");
                
            }
            else 
            { 
                if (_eyeState != EyeState.Blink || !_isEyeAnimating) // TODO: Check if this makes sense
                {
                    SetEyeState(EyeState.Static);

                }
            }
        }
    }

    private void SetEyeState(EyeState state)
    {
        bool isStateChanged = _eyeState != state;
        if (isStateChanged)
        {
            //Debug.Log("Eye state changed from: " + _eyeState +" to: " + state);
            switch (state)
            {
                case EyeState.Blink:
                    StopEyeCoroutines();
                    _eyeCoroutine = StartCoroutine(BlinkEyes());
                    break;
                case EyeState.Static:
                    StopEyeCoroutines();
                    _eyeCoroutine = StartCoroutine(ResetEyes());
                    break;
                case EyeState.Squint:
                    StopEyeCoroutines();
                    _eyeCoroutine = StartCoroutine(SquintEyes());
                    break;
                case EyeState.Wide:
                    StopEyeCoroutines();
                    _eyeCoroutine = StartCoroutine(WidenEyes());
                    break;
            }
            
        }
        _eyeState = state;
    }

    private IEnumerator ResetEyes()
    {
        _isEyeAnimating = true;
        float resetSpeed = 5f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * resetSpeed;
            _frontEye.localPosition = Vector3.Lerp(_frontEye.localPosition, _frontEyeStartPos, t);
            _frontEye.localRotation = Quaternion.Lerp(_frontEye.localRotation, _frontEyeStartRotation, t);
            _frontEye.localScale = Vector3.Lerp(_frontEye.localScale, _frontEyeStartScale, t);

            _backEye.localPosition = Vector3.Lerp(_backEye.localPosition, _backEyeStartPos, t);
            _backEye.localRotation = Quaternion.Lerp(_backEye.localRotation, _backEyeStartRotation, t);
            _backEye.localScale = Vector3.Lerp(_backEye.localScale, _backEyeStartScale, t);
            yield return null;
        }
        _frontEye.localPosition = _frontEyeStartPos;
        _frontEye.localRotation = _frontEyeStartRotation;
        _frontEye.localScale = _frontEyeStartScale;

        _backEye.localPosition = _backEyeStartPos;
        _backEye.localRotation = _backEyeStartRotation;
        _backEye.localScale = _backEyeStartScale;
        _isEyeAnimating = false;
    
    }
    private IEnumerator WidenEyes()
    {
        _isEyeAnimating = true;
        _frontEye.localScale = _frontEyeStartScale;
        _backEye.localScale = _backEyeStartScale;
        float widen = 1.65f;
        Vector3 frontEyeTargetScale = new Vector3(_frontEyeStartScale.x, _frontEyeStartScale.y * widen, _frontEyeStartScale.z);
        Vector3 backEyeTargetScale = new Vector3(_backEyeStartScale.x, _backEyeStartScale.y * widen, _backEyeStartScale.z);
        float widenTime = widen;
        float t = 0f;
        while (t < widenTime)
        {
            t += Time.deltaTime;
            _frontEye.localScale = Vector3.Lerp(_frontEyeStartScale, frontEyeTargetScale, t / widenTime);
            _backEye.localScale = Vector3.Lerp(_backEyeStartScale, backEyeTargetScale, t / widenTime);
            //Debug.Log(_frontEye.localScale);
            yield return null;
            
        }
        _frontEye.localScale = frontEyeTargetScale;
        _backEye.localScale = backEyeTargetScale;
        //Debug.Log(_frontEye.localScale+ "done");
        _isEyeAnimating = false;

    }

    private IEnumerator SquintEyes()
    {
        _isEyeAnimating = true;
        Vector3 frontEyeTargetScale = new Vector3(_frontEyeStartScale.x, _frontEyeStartScale.y * 0.5f, _frontEyeStartScale.z);
        Vector3 backEyeTargetScale = new Vector3(_backEyeStartScale.x, _backEyeStartScale.y * 0.5f, _backEyeStartScale.z);

        float squintTime = 0.5f;
        float t = 0f;
        while (t < squintTime)
        {
            t += Time.deltaTime;
            _frontEye.localScale = Vector3.Lerp(_frontEyeStartScale, frontEyeTargetScale, t / squintTime);
            _backEye.localScale = Vector3.Lerp(_backEyeStartScale, backEyeTargetScale, t / squintTime);
            yield return null;
        }
        _frontEye.localScale = frontEyeTargetScale;
        _backEye.localScale = backEyeTargetScale;
        _isEyeAnimating = false;

    }

    private IEnumerator BlinkEyes()
    {
        _isEyeAnimating = true;
        Vector3 frontEyeTargetScale = new Vector3(_frontEyeStartScale.x, 0.01f, _frontEyeStartScale.z);
        Vector3 backEyeTargetScale = new Vector3(_backEyeStartScale.x, 0.01f, _backEyeStartScale.z);
        float squintTime = 0.1f;
        if (!_isHeadBumped)
        {
            //SFXManager.Instance.KillAndPlaySFX(_blinkClip, volume:0.15f);
        }
        float t = 0f;
        while (t < squintTime)
        {
            t += Time.deltaTime;
            _frontEye.localScale = Vector3.Lerp(_frontEyeStartScale, frontEyeTargetScale, t / squintTime);
            _backEye.localScale = Vector3.Lerp(_backEyeStartScale, backEyeTargetScale, t / squintTime);
            yield return null;
        }
        _frontEye.localScale = frontEyeTargetScale;
        _backEye.localScale = backEyeTargetScale;
        yield return null;
        t = 0f;
        while (t < squintTime)
        {
            t += Time.deltaTime;
            _frontEye.localScale = Vector3.Lerp(frontEyeTargetScale, _frontEyeStartScale, t / squintTime);
            _backEye.localScale = Vector3.Lerp(backEyeTargetScale, _backEyeStartScale, t / squintTime);
            yield return null;
        }
        SetNewBlinkInterval();
        _isHeadBumped = false; // Reset head bumped so it doesnt have to wait touching the ground
        _isEyeAnimating = false;
    
    }
    private void SetNewBlinkInterval()
    {
        _BlinkTimer = 0f; // Reset timer
        _NextBlinkTime = Random.Range(_BlinkIntervalMinMax.x, _BlinkIntervalMinMax.y);
    }

    private void StopEyeCoroutines()
    {
        //Debug.Log("Stopping Eye Coroutines");
        if (_eyeCoroutine != null)
        {
            StopCoroutine(_eyeCoroutine);
            _eyeCoroutine = null;
        }

    }

    #endregion

    #region SquashAndStretch

    private void JumpAnimation()
    {
        //Debug.Log("Jump Animation");
        ResetBodyScale();
        StartCoroutine(JumpAnimationRoutine());
    }

    private IEnumerator JumpAnimationRoutine()
    {
        Vector3 startScale = _bodyStartScale;
        Vector3 targetScale = new Vector3(startScale.x * (1 - _bodyDeformPercentage), startScale.y * (1 + _bodyDeformPercentage), startScale.z);
        float jumpTime = _bodyDeformTime/2;
        float t = 0f;
        while (t < jumpTime)
        {
            t += Time.deltaTime;
            _playerBody.localScale = Vector3.Lerp(startScale, targetScale, t / jumpTime);
            yield return null;
        }
        _playerBody.localScale = targetScale;

        t = 0f;
        while (t < jumpTime)
        {
            t += Time.deltaTime;
            _playerBody.localScale = Vector3.Lerp(targetScale, startScale, t / jumpTime);
            yield return null;
        }
        _playerBody.localScale = startScale;
    }

    private void LandAnimation()
    {
        //Debug.Log("Land Animation");
        ResetBodyScale();
        SetEyeState(EyeState.Blink);
        StartCoroutine(LandAnimationRoutine());
    }

    private IEnumerator LandAnimationRoutine()
    {
        Vector3 startScale = _bodyStartScale;
        Vector3 targetScale = new Vector3(startScale.x * (1 + _bodyDeformPercentage), startScale.y * (1 - _bodyDeformPercentage), startScale.z);

        Vector3 startPosition  = _bodyStartPos;
        Vector3 targetPosition = new Vector3(startPosition.x, startPosition.y - (startScale.y * _bodyDeformPercentage), startPosition.z);
        float landTime = _bodyDeformTime/2;
        float t = 0f;
        while (t < landTime)
        {
            t += Time.deltaTime;
            _playerBody.localScale = Vector3.Lerp(startScale, targetScale, t / landTime);
            _playerBody.localPosition = Vector3.Lerp(startPosition, targetPosition, t / landTime);
            yield return null;
        }
        _playerBody.localScale = targetScale;

        t = 0f;
        while (t < landTime)
        {
            t += Time.deltaTime;
            _playerBody.localScale = Vector3.Lerp(targetScale, startScale, t / landTime);
            _playerBody.localPosition = Vector3.Lerp(targetPosition, startPosition, t / landTime);
            yield return null;
        }
        _playerBody.localScale = startScale;
    }

    private void ResetBodyScale()
    {
        _playerBody.localScale = _bodyStartScale;
    }

    /*
    private IEnumerator ResetBodyAnimationRoutine()
    {
        Vector3 startScale = _playerBody.localScale;
        Vector3 targetScale = _bodyStartScale;
        float resetTime = 0.1f;
        float t = 0f;
        while (t < resetTime)
        {
            t += Time.deltaTime;
            _playerBody.localScale = Vector3.Lerp(startScale, targetScale, t / resetTime);
            yield return null;
        }
        _playerBody.localScale = targetScale;
    }
    */

    #endregion
    private void OnGroundedChanged(bool isGrounded)
    {
        _isGrounded = isGrounded;
        if (_isGrounded)
        {
            _isHeadBumped = false;
        }
    }

    private void OnHeadBumped(bool isHeadBumped)
    {
        _isHeadBumped = isHeadBumped;
        if (_isHeadBumped)
        {
            SetEyeState(EyeState.Blink);
            LandAnimation();
        }
    }
}
