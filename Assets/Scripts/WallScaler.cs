#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]

[ExecuteInEditMode]
public class WallScaler : MonoBehaviour
{
    [Header("Values")]
    [Range(1,100)] public int sizeX = 1;
    [Range(1,100)] public int sizeY = 1;

    [Header("Components")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BoxCollider2D _boxCollider;
    void OnValidate()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (_boxCollider == null)
        {
            _boxCollider = GetComponent<BoxCollider2D>();
        }
#if UNITY_EDITOR
        EditorApplication.delayCall += ApplySizes;
#endif
    }

    private void ApplySizes()
    {
        _spriteRenderer.size = new Vector2(sizeX, sizeY);
        _boxCollider.size = new Vector2(sizeX, sizeY);

    }
}
