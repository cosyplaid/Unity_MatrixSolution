using System.Collections;
using System.Collections.Generic;
using NumSharp;
using UnityEngine;

public class Model : MonoBehaviour
{
    public List<Transform> _elements;

    public void Move(in NDArray input)
    {
        ChangeColor();

        for (byte i = 0; i < input.shape[0]; i++)
        {
            var row = input[i];

            double x = row[0] * row[3];
            double y = row[1] * row[3];
            double z = row[2] * row[3];

            Vector3 vector = new Vector3((float)x, (float)y, (float)z);

            if (_elements[i] != null)
                _elements[i].position = vector;
            else
                MyDebug.Log($"Элемента под индексом {i} не существует в списке!", "#FFD700");
        }
    }

    public void SetScale(Vector3 newScale)
    {
        foreach (var e in _elements)
        {
            e.localScale = newScale;
        }
    }

    //public void EnableElement(int index) => _elements[index].gameObject.SetActive(true);

    public void DisableModel() => gameObject.SetActive(false);

    public void ChangeColor()
    {
        foreach (var e in _elements)
        {
            if (e.TryGetComponent<ColorController>(out ColorController colorController))
                colorController.ChangeColor();
        }
    }

    public void ResetColor()
    {
        foreach (var e in _elements)
        {
            if(e.TryGetComponent<ColorController>(out ColorController colorController))
                colorController.ResetColor();
        }
    }
}
