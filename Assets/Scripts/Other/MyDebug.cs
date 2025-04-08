using UnityEngine;

public static class MyDebug
{
    public static void Log(string text)
    {
        Debug.Log(text);
    }

    public static void Log(string text, string color)
    {
    #if UNITY_EDITOR
        Debug.Log($"<color={color}>{text}</color>");
    #else
        Debug.Log(text);
    #endif
    }
}
