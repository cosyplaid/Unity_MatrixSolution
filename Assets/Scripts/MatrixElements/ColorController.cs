using UnityEngine;

public class ColorController : MonoBehaviour
{
    private MaterialPropertyBlock _pBlock;
    private Renderer _renderer;

    public Color defaultColor;
    public Color highlightColor;
    public Color foundColor;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _pBlock = new MaterialPropertyBlock();
    }

    public void ResetColor()
    {
        _pBlock.SetColor("_Color", defaultColor);
        _renderer.SetPropertyBlock(_pBlock);
    }

    public void SetHighlightColor()
    {
        _pBlock.SetColor("_Color", highlightColor);
        _renderer.SetPropertyBlock(_pBlock);
    }

    public void SetFoundColor()
    {
        _pBlock.SetColor("_Color", foundColor);
        _renderer.SetPropertyBlock(_pBlock);
    }
}
