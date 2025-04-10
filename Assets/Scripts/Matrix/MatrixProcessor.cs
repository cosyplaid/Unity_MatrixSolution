using NumSharp;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#nullable enable

public static class MatrixProcessor
{
    public static List<NDArray> ConvertMatrixElementsToNDArray(in List<MatrixElement_JSON> matrixElements)
    {
        List<NDArray> array = new List<NDArray>();

        if (matrixElements.Count > 0)
        {
            MyDebug.Log($"Total Count: {matrixElements.Count}", "#FFD700");

            foreach (var element in matrixElements)
            {
                var _matrix = BuildNDArray(element);
                array.Add(_matrix);
            }
        }
        else
        {
            MyDebug.Log("Matrix Elements Set is empty!", "#8B0000");
        }

        return array;
    }

    public static List<MatrixElement_JSON> ConvertNDArrayToJson(in List<NDArray> matrixElements)
    {
        List<MatrixElement_JSON> array = new List<MatrixElement_JSON>();

        if (matrixElements.Count > 0)
        {
            MyDebug.Log($"Total Count: {matrixElements.Count}", "#FFD700");

            foreach (var element in matrixElements)
            {
                var _matrix = BuildMatrixElement_JSON(element);
                array.Add(_matrix);
            }
        }
        else
        {
            MyDebug.Log("Matrix Elements Set is empty!", "#8B0000");
        }

        return array;
    }

    public static NDArray BuildNDArray(in MatrixElement_JSON element) // ������������ MatrixElement_JSON ��� ������� NDArray
    {
        var matrix = np.array(new[,]
        {
                { (double)element.m00, (double)element.m01, (double)element.m02, (double)element.m03},
                { (double)element.m10, (double)element.m11, (double)element.m12, (double)element.m13},
                { (double)element.m20, (double)element.m21, (double)element.m22, (double)element.m23},
                { (double)element.m30, (double)element.m31, (double)element.m32, (double)element.m33},
            });

        return matrix;
    }

    public static MatrixElement_JSON BuildMatrixElement_JSON(in NDArray matrix) // ������������ ������� NDArray ��� MatrixElement_JSON
    {
        MatrixElement_JSON element = new MatrixElement_JSON();

        element.m00 = matrix[0, 0];
        element.m01 = matrix[0, 1];
        element.m02 = matrix[0, 2];
        element.m03 = matrix[0, 3];

        element.m10 = matrix[1, 0];
        element.m11 = matrix[1, 1];
        element.m12 = matrix[1, 2];
        element.m13 = matrix[1, 3];

        element.m20 = matrix[2, 0];
        element.m21 = matrix[2, 1];
        element.m22 = matrix[2, 2];
        element.m23 = matrix[2, 3];

        element.m30 = matrix[3, 0];
        element.m31 = matrix[3, 1];
        element.m32 = matrix[3, 2];
        element.m33 = matrix[3, 3];

        return element;
    }

    public static NDArray MultiplyMatrix(in NDArray matrixA, in NDArray matrixB) => np.dot(matrixA, matrixB);

    public static bool CompareMatrix(in NDArray matrixA, in NDArray matrixB) => np.all(matrixA == matrixB); //matrixA == matrixB ������� NDArray ��� �� �������� ����������� ��������� - true; np.all() �������� ��� �� �������� = true.

    public static bool CompareMatrix(in NDArray matrixA, in NDArray matrixB, double tolerance) // ��������� ������ � ������ �����������
    {
        if (matrixA.Shape[0] != matrixB.Shape[0] && matrixA.Shape[1] != matrixB.Shape[1])
            return false;

        for (byte i = 0; i < matrixA.shape[0]; i++)
        {
            for (byte k = 0; k < matrixA.shape[1]; k++)
            {
                if (Math.Abs((double)matrixA[i, k] - (double)matrixB[i, k]) > tolerance)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static NDArray GaussJordanInverse(in NDArray matrix) //�������� ��������� �������� ������� ������� ������ ��������
    {
        int n = matrix.shape[0]; //shape[0] - ������; shape[1] - �������

        if (n != matrix.shape[1]) 
            throw new ArgumentException("������� ������ ���� ����������.");

        // ������� ����������� ������� [A | I]
        var augmentedMatrix = np.hstack(new NDArray[] { matrix, np.eye(n) });

        // ��������� ����� ������-�������
        for (byte i = 0; i < n; i++)
        {
            double pivot = augmentedMatrix[i, i];

            if (pivot == 0)
            {
                MyDebug.Log("������� ��������� � �� ����� ��������. ������ ��������...", "#8B0000");
                return matrix; //
            }

            for (byte j = 0; j < augmentedMatrix.shape[1]; j++) //8 <= 4(A) + 4(I)
                augmentedMatrix[i, j] /= pivot;

            for (byte k = 0; k < n; k++)
            {
                if (k != i)
                {
                    double factor = augmentedMatrix[k, i];

                    for (byte j = 0; j < augmentedMatrix.shape[1]; j++)
                    {
                        augmentedMatrix[k, j] -= factor * augmentedMatrix[i, j];
                    }
                }
            }
        }

        // ��������� � ���������� ������ ����� � �������� �������� �������
        return augmentedMatrix[$":, {n}:"];
    }

    public static Vector3 GetPosition(in NDArray matrix)
    {
        double x = matrix[0][3];
        double y = matrix[1][3];
        double z = matrix[2][3];

        Vector3 vector = new Vector3((float)x, (float)y, (float)z);

        //Debug.Log($"Vector3: {vector}");

        return vector;
    }

    public static Quaternion GetRotation(in NDArray matrix)
    {
        Quaternion rotation = Quaternion.identity;

        Vector3 xDirection = GetAxisDirection(matrix[0][0], matrix[1][0], matrix[2][0]).normalized;

        Vector3 yDirection = GetAxisDirection(matrix[0][1], matrix[1][1], matrix[2][1]).normalized;

        //Vector3 zDirection = GetAxisDirection(matrix[0][2], matrix[1][2],matrix[2][2]);
        //Debug.Log($"ZDir: {zDirection}");

        if (CheckPerpendicularity(xDirection, yDirection))
        {
            //MyDebug.Log($"������� {a} � {b} ���������������.", "#00FF00");
            rotation = Quaternion.LookRotation(xDirection, yDirection);
        }
        else
        {
            MyDebug.Log($"������� {xDirection} � {yDirection} �� ���������������.", "#FFD700");
        }

        return rotation;
    }

    private static Vector3 GetAxisDirection(double xVal, double yVal, double zVal)
    {
        double x = xVal;
        double y = yVal;
        double z = zVal;

        return new Vector3(
            (float)x,
            (float)y,
            (float)z
        );
    }

    private static bool CheckPerpendicularity(Vector3 a, Vector3 b)
    {
        float dotProduct = Vector3.Dot(a, b);

        if (Mathf.Abs(dotProduct) < 0.0000005f)
            return true;
        else
            return false;
    }


    //public static NDArray HandleNaN(in NDArray matrix) //�������� NaN �� 0
    //{
    //    NDArray newMatrix = matrix;

    //    for (byte i = 0; i < matrix.shape[0]; i++)
    //    {
    //        for (byte k = 0; k < matrix.shape[1]; k++)
    //        {
    //            if (double.IsNaN((double)matrix[i, k]))
    //                newMatrix[i, k] = 0.0;
    //        }
    //    }

    //    return newMatrix;
    //}

    //static NDArray RoundMatrix(in NDArray matrix, int roundValue = 10)
    //{
    //    NDArray roundedMatrix = matrix;

    //    for (byte i = 0; i < matrix.shape[0]; i++)
    //    {
    //        for (byte k = 0; k < matrix.shape[1]; k++)
    //        {
    //            roundedMatrix[i, k] = Math.Round((double)matrix[i, k], roundValue);
    //        }
    //    }
    //    return roundedMatrix;
    //}
}
