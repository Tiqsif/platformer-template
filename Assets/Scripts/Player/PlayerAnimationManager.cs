using UnityEngine;

public class PlayerAnimationManager : MonoBehaviour
{
    [SerializeField] private Transform _playerBody;
    [SerializeField] private AudioClip _playerFootsteps;

    [SerializeField] [Range(1f,10f)] private float _bobbingSpeed = 2f;
    [SerializeField] private AnimationCurve _bobbingCurve; // Define the curve in the Inspector
    [SerializeField] private float _bobbingAmplitude = 0.1f;
    private float _bobbingTime; // Keeps track of time

    public float maxTiltAngle = 10f;

    public void Tick(float velX, float velXMax, bool isGrounded)
    {
        // calculate tilt angle by lerping tilt angle from 0 to max tilt angle
        float tiltAngle = Mathf.Lerp(0, maxTiltAngle, Mathf.Abs(velX / velXMax));
        _playerBody.localRotation = Quaternion.Euler(0, 0, tiltAngle);

        if (Mathf.Abs(velX/velXMax) >= 0.99f && isGrounded)
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
                SFXManager.Instance.KillAndPlaySFX(_playerFootsteps, 0.5f);
            }
        }
        

        if (Mathf.Abs(velX / velXMax) < 0.1f)
        {
            _playerBody.localPosition = Vector3.zero;
        }



    }
}
