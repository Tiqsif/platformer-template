using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private CinemachineCamera[] _allCameras;

    [Header("Player Jump/Fall Y Lerping Values")]
    [SerializeField] private float _fallPanAmount = 0.25f;
    [SerializeField] private float _fallYPanTime = 0.35f;
    public float fallSpeedYDampingChangeThreshold = -15f;

    public bool isLerpingYDamping { get; private set; }
    public bool lerpedFromPlayerFall { get; set; }

    private Coroutine _lerpYPanCoroutine;
    private CinemachineCamera _activeCamera;
    private CinemachinePositionComposer _positionComposer;

    private float _normYDampingAmount;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        foreach (var cam in _allCameras)
        {
            if (cam.enabled)
            {
                _activeCamera = cam;
                _positionComposer = _activeCamera.GetComponent<CinemachinePositionComposer>();
                if (_positionComposer != null)
                {
                    _normYDampingAmount = _positionComposer.Damping.y;
                }
                break;
            }
        }


    }

    #region Lerp Y Damping

    public void LerpYDamping(bool isPlayerFalling)
    {

        //if (_lerpYPanCoroutine != null) {StopCoroutine(_lerpYPanCoroutine);}

        _lerpYPanCoroutine = StartCoroutine(LerpYRoutine(isPlayerFalling));
    }

    private IEnumerator LerpYRoutine(bool isPlayerFalling)
    {
        isLerpingYDamping = true;

        float startDampAmount  = _positionComposer.Damping.y;
        float endDampAmount;

        if (isPlayerFalling)
        {
            endDampAmount = _fallPanAmount;
            lerpedFromPlayerFall = true;
            //Debug.Log("Lerping from Player Fall: " + endDampAmount);
        }
        else
        {
            endDampAmount = _normYDampingAmount;
            //Debug.Log("Lerping from Player Jump");
        }

        float elapsedTime = 0f;
        while (elapsedTime < _fallYPanTime)
        {
            elapsedTime += Time.deltaTime;
            float lerpedPanAmount = Mathf.Lerp(startDampAmount, endDampAmount, elapsedTime / _fallYPanTime);
            _positionComposer.Damping.y = lerpedPanAmount;
            yield return null;
        }

        //Debug.Log("Lerping Done, Y: " + _positionComposer.Damping.y);

        isLerpingYDamping = false;
    }
    #endregion
}
