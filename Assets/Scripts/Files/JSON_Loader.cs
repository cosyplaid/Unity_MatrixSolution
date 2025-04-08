using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;
using NumSharp;

public class JSON_Loader
{
    public List<MatrixElement_JSON> LoadMatrixElement_JSON(string path)
    {
        List<MatrixElement_JSON> matrixElements = new List<MatrixElement_JSON>();
        string jsonString;

        // ������ �����
        try
        {
            jsonString = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            MyDebug.Log($"��������� ������ ��� ������ �����: {ex.Message}", "#8B0000");
            return matrixElements;
        }

        Debug.Log(jsonString);

        // ��������������
        try
        {
            matrixElements = JsonConvert.DeserializeObject<List<MatrixElement_JSON>>(jsonString);
        }
        catch (JsonSerializationException ex)
        {
            MyDebug.Log($"������ ��� �������������� JSON: {ex.Message}", "#8B0000");
            return matrixElements;
        }
        catch (JsonReaderException ex)
        {
            MyDebug.Log($"������ ������ JSON: {ex.Message}", "#8B0000");
            return matrixElements;
        }
        catch (Exception ex)
        {
            MyDebug.Log($"����� ������: {ex.Message}", "#8B0000");
            return matrixElements;
        }

        return matrixElements;
    }

    public void SaveMatrixElement_JSON(in List<MatrixElement_JSON> matrixElements, string path)
    {
        string json = JsonConvert.SerializeObject(matrixElements, Formatting.Indented);

        File.WriteAllText(path, json);

        MyDebug.Log($"������ ���������! �����: {path}", "#FFD700");
        MyDebug.Log($"���������� ������: {matrixElements.Count}", "#00FF00");
    }
}
