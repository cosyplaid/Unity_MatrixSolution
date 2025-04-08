using UnityEngine;

public class ColorController : MonoBehaviour
{
    private MaterialPropertyBlock _pBlock;
    private Renderer _renderer;

    public Color defaultColor;
    public Color color;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _pBlock = new MaterialPropertyBlock();
    }

    public void ChangeColor()
    {
        _pBlock.SetColor("_Color", color);
        _renderer.SetPropertyBlock(_pBlock);
    }

    public void ResetColor()
    {
        _pBlock.SetColor("_Color", defaultColor);
        _renderer.SetPropertyBlock(_pBlock);
    }
}
