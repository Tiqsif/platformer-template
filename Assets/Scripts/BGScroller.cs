using UnityEngine;
using UnityEngine.UI;

public class BGScroller : MonoBehaviour
{
    [SerializeField] private RawImage _image;
    [SerializeField] private Vector2 _scrollSpeed;
    void Update()
    {
        _image.uvRect = new Rect(_image.uvRect.position + _scrollSpeed * Time.deltaTime, _image.uvRect.size);
    }
}
