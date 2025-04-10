using UnityEngine;

[RequireComponent(typeof(ColorController))]
public class Model : MonoBehaviour
{
    private Transform _transform;

    private ColorController _colorController;

    private Vector3 _startPositon = new Vector3(0,0,0);
    private Quaternion _startRotation = Quaternion.identity;

    private void Awake()
    {
        _transform = transform;
        _colorController = GetComponent<ColorController>();
    }

    public void Init(Vector3 position, Quaternion rotation)
    {
        _startPositon = position;
        _startRotation = rotation;

        Reset();
    }

    public void Reset()
    {
        Move(_startPositon);
        Rotate(_startRotation);
        SetScale(new Vector3(1, 1, 1));
        ResetColor();

        DisableModel();
    }
    
    public void Move(in Vector3 input) => _transform.position = input;

    public void Rotate(in Quaternion rotation) => _transform.rotation = rotation;

    public void SetScale(Vector3 newScale) => _transform.localScale = newScale;

    public void ChangeColor(bool found = false)
    {
        if (found)
            _colorController.SetFoundColor();
        else
            _colorController.SetHighlightColor();
    }

    public void ResetColor() => _colorController.ResetColor();

    public void DisableModel() => gameObject.SetActive(false);
}
