using UnityEngine;

public class PlayerParticleManager : MonoBehaviour
{
    [SerializeField] private GameObject _jumpParticlePrefab;
    [SerializeField] private GameObject _landParticlePrefab;
    [SerializeField] private GameObject _dashParticlePrefab;
    [SerializeField] private GameObject _doubleJumpParticlePrefab;

    private Vector3 _center = new Vector3(0, 0, 0);
    private Vector3 _feet = new Vector3(0, -1.5f, 0);

    private void OnEnable()
    {
        PlayerMovement.FirstJumpStartedEvent += PlayJumpParticle;
        //PlayerMovement.LandedEvent += PlayLandParticle;
        PlayerMovement.FellHighEvent += PlayLandParticle;
        PlayerMovement.DoubleJumpStartedEvent += PlayDoubleJumpParticle;
    }

    private void OnDisable()
    {
        PlayerMovement.FirstJumpStartedEvent -= PlayJumpParticle;
        //PlayerMovement.LandedEvent -= PlayLandParticle;
        PlayerMovement.FellHighEvent -= PlayLandParticle;
        PlayerMovement.DoubleJumpStartedEvent -= PlayDoubleJumpParticle;
    }
    public void PlayJumpParticle()
    {
        PlayParticle(_jumpParticlePrefab, _feet);
    }

    public void PlayLandParticle()
    {
        PlayParticle(_landParticlePrefab, _feet);
    }

    public void PlayDashParticle()
    {
        PlayParticle(_dashParticlePrefab);
    }

    public void PlayDoubleJumpParticle()
    {
        PlayParticle(_doubleJumpParticlePrefab, _center);
    }
    private void PlayParticle(GameObject particlePrefab, Vector3? offset = null)
    {
        if (particlePrefab == null) return;
        if (offset == null) offset = Vector3.zero;
        GameObject particleObject = Instantiate(particlePrefab, transform.position + (Vector3)offset, Quaternion.identity);

    }
}
