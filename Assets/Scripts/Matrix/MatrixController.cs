using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NumSharp;
using System.Threading;
using System;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MatrixController : MonoBehaviour
{
    public static List<NDArray> _modelSet = new List<NDArray>();
    public static List<NDArray> _spaceSet = new List<NDArray>();
    public static List<NDArray> _offsetSet = new List<NDArray>();

    public int _tolerance = 1;
    public double _epsilon = 0.00005;
    [SerializeField] private bool _epsilonEffectsOnFileName = false;

    [SerializeField] private string _modelFileName = "model.json";
    [SerializeField] private string _spaceFileName = "space.json";
    [SerializeField] private string _offsetFileName = "offset.json";

    [SerializeField] private bool _isInitialized = false;
    [SerializeField] private bool _taskIsRunning = false;

    public Text statusText;
    private float _statusK;

    public Text offsetCounterText;

    private Coroutine _findOffsetCoroutine;

    public Action offsetSearchIsComplete;
    public Action<Action> offsetSearchIsStopped;
    private Action<float> updateProgress;
    private Action<int> updateOffsetCount;

    public int _initCount = 100;
    public PoolHandler<Model> _matrixModelPool;
    public Model _model;
    public Transform _modelGroupParent;
    public List<Model> _models;

    public float _scaleFactor = 2f;

    private void Awake() => _matrixModelPool = new PoolHandler<Model>(ref _models, true, _model, _initCount, _modelGroupParent);

    private void Start()
    {
        Init();
        StartOffsetCoroutine();
    }

    private void OnEnable()
    {
        offsetSearchIsComplete += CompleteOffsetCoroutine;
        offsetSearchIsStopped += StopOffsetCoroutine;
        updateProgress += DrawStatusText;
        updateOffsetCount += DrawOffsetText;
    }

    private void OnDisable()
    {
        offsetSearchIsComplete -= CompleteOffsetCoroutine;
        offsetSearchIsStopped -= StopOffsetCoroutine;
        updateProgress -= DrawStatusText;
        updateOffsetCount -= DrawOffsetText;
    }

    public void Init()
    {
        if (!_taskIsRunning)
        {
            _isInitialized = false;
            _taskIsRunning = false;
            DrawStatusText(0);
            DrawOffsetText(0);

            LoadInputData();
            _isInitialized = CheckInputData();
        }
        else
        {
            MyDebug.Log("����� ���������� ������ �� �� ����� ���������� ������.", "#FFD700");
        }
    }

    private void LoadInputData()
    {
        string modelSetPath = Application.dataPath + $"/JSON_Storage/Input/{_modelFileName}";
        string spaceSetPath = Application.dataPath + $"/JSON_Storage/Input/{_spaceFileName}";

        _modelSet = LoadMatrix(modelSetPath);
        _spaceSet = LoadMatrix(spaceSetPath);
    }

    private bool CheckInputData()
    {
        if (_modelSet.Count > 0 && _spaceSet.Count > 0)
        {
            _statusK = _spaceSet.Count / 100f;
            MyDebug.Log($"StatusKoef: {_statusK}");
            MyDebug.Log("������ ������� ��������� � ����������!", "#00FF00");
            return true;
        }
        else
        {
            MyDebug.Log("��������� ������������ ������!", "#00FF00");
            return false;
        }
    }

    private void DrawStatusText(float progress)
    {
        if (statusText)
            statusText.text = $"{progress}%";
    }

    private void DrawOffsetText(int count)
    {
        if (offsetCounterText)
            offsetCounterText.text = $"���������� ��������� ��������: {count}";
    }

    public void StartOffsetCoroutine()
    {
        if (_isInitialized)
        {
            if (!_taskIsRunning)
            {
                _taskIsRunning = true;
                MyDebug.Log("��������� ������!", "#00FF00");
                _findOffsetCoroutine = StartCoroutine(FindOffset(_modelSet, _spaceSet));
            }
            else
            {
                MyDebug.Log("������ ��� �����������!", "#FFD700");
            }
        }
        else
        {
            MyDebug.Log("��������� ��������� �������� ��-�� ������������ ������...", "#FFD700");
        }
    }

    public void CompleteOffsetCoroutine()
    {
        StopOffsetCoroutine(() => MyDebug.Log("������ ���������!", "#00FF00"));
        SaveOffsetMatrix();
    }

    public void StopOffsetCoroutine(Action action = null)
    {
        if (_taskIsRunning)
        {
            _taskIsRunning = false;
            action?.Invoke();
            DrawStatusText(0);

            if (_findOffsetCoroutine != null)
            {
                StopCoroutine(_findOffsetCoroutine);
                _findOffsetCoroutine = null;
            }
        }
        else
        {
            MyDebug.Log("������ ��� ���� �����������!", "#FFD700");
        }
    }

    public List<NDArray> LoadMatrix(string path)
    {
        JSON_Loader _jsonLoader = new JSON_Loader();

        List<MatrixElement_JSON> _loadedMatrixElements = _jsonLoader.LoadMatrixElement_JSON(path);

        return MatrixProcessor.ConvertMatrixElementsToNDArray(_loadedMatrixElements);
    }

    public void SaveOffsetMatrix()
    {
        JSON_Loader _jsonLoader = new JSON_Loader();

        string offsetSetPath = Application.dataPath + $"/JSON_Storage/Output/{_offsetFileName}";

        var newList = MatrixProcessor.ConvertNDArrayToJson(_offsetSet);

        _jsonLoader.SaveMatrixElement_JSON(newList, offsetSetPath);

        #if UNITY_EDITOR
        AssetDatabase.Refresh(); // ��������� �������� Unity, ����� ����� ���� ����� ������� ���� � ����� Output ��� ������ ������� Refresh (Ctr+R)
        #endif
    }

    public IEnumerator FindOffset(List<NDArray> model, List<NDArray> space) // offset = B*A(-1)
    {
        updateOffsetCount?.Invoke(0);
        _offsetSet = new List<NDArray>();

        int progress = 0;
        int i = 0;

        foreach (NDArray sMatrix in space)
        {
            progress++;

            if (progress % _statusK == 0)
            {
                updateProgress?.Invoke(progress / _statusK);
            }

            NDArray inversedMatrix = MatrixProcessor.GaussJordanInverse(model[0]); // �������� ������� A

            if (inversedMatrix == null)
            {
                offsetSearchIsStopped?.Invoke(() => 
                { 
                    MyDebug.Log("������ � ���������� �������� �������! ������������� ������.", "#FFD700"); 
                    CancelGroup(); 
                });
                yield break;
            }

            NDArray currentOffset = MatrixProcessor.MultiplyMatrix(sMatrix, inversedMatrix); // ��������� �������� B*A(-1)

            //offset = HandleNaN(offset);

            bool isValidOffset = true;

            int foundElements = 0;

            foreach (NDArray mMatrix in model)
            {
                NDArray offsettedMatrix = MatrixProcessor.MultiplyMatrix(currentOffset, mMatrix); // �������������� ���������

                yield return null;

                if (space.Any(matrix => MatrixProcessor.CompareMatrix(matrix, offsettedMatrix, _epsilon)))
                {
                    foundElements++;
                    DrawMatrix(foundElements, offsettedMatrix);
                }
                else // ���� ���� �� ���� ������� �� ���������
                {
                    isValidOffset = false; 
                    CancelGroup();
                    break;
                }
            }

            if (isValidOffset && !_offsetSet.Contains(currentOffset))
            {
                i++;
                updateOffsetCount?.Invoke(i);
                _offsetSet.Add(currentOffset);
                CancelGroup();
            }
        }

        offsetSearchIsComplete?.Invoke();
    }

    List<Model> foundList = new List<Model>();

    private void DrawMatrix(int index, NDArray offsettedMatrix)
    {
        Model model = _matrixModelPool.GetPoolObject(ref _models);
        model.Move(offsettedMatrix);
        model.SetScale(new Vector3(_scaleFactor, _scaleFactor, _scaleFactor));
        foundList.Add(model);

        if(foundList.Count - 2 >= 0)
        {
            foundList[foundList.Count - 2].ResetColor();
            foundList[foundList.Count - 2].SetScale(new Vector3(_scaleFactor/3f, _scaleFactor/3f, _scaleFactor/3f));
        }
    }

    private void CancelGroup()
    {
        foreach(var model in foundList)
        {
            model.DisableModel();
        }

        foundList.Clear();
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(MatrixController))]
public class MatrixControllerEditor : Editor
{
    MatrixController _matrixController;
    SerializedObject _sObject;

    //Serialized Fields
    SerializedProperty _sModelFileName;
    SerializedProperty _sSpaceFileName;
    SerializedProperty _sOffsetFileName;

    SerializedProperty _sTaskIsRunning;

    SerializedProperty _sEpsilonEffectsOnFileName;

    private void OnEnable() //�����, ���������� ��� ��������� ���� ����������, ��������, ����� �� �������� ������
    {
        _matrixController = target as MatrixController;
        _sObject = new SerializedObject(_matrixController);
        _sModelFileName = _sObject.FindProperty("_modelFileName");
        _sSpaceFileName = _sObject.FindProperty("_spaceFileName");
        _sOffsetFileName = _sObject.FindProperty("_offsetFileName");

        _sTaskIsRunning = _sObject.FindProperty("_taskIsRunning");
        _sEpsilonEffectsOnFileName = _sObject.FindProperty("_epsilonEffectsOnFileName");
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI(); //��������� ������������ ����������
        _sObject.Update(); //��������� ������������� ���������������� ������� (������� Player). ��������� ��������� ������ � ���������� ��������

        DrawCustomInspector(); //����� � ���� ������ ����� ������������ ��������� ���������

        //// ������������, ����� GUI ��������� (�������� �������� � ����������, ��������)
        if (GUI.changed)
        {
            EditorUtility.SetDirty(_matrixController); //�������� ��������� �������, ��� ����� ���� Unity ������, ��� ��������� ��������� � ��������� �������� ����������� ��������� �� ����
            _sObject.ApplyModifiedProperties(); //��������� ��������� ��������������� ����������, ���� ������, ��� ��������� �������� ����������� ��������� �� ����
        }
    }

    private void DrawCustomInspector()
    {
        DrawHead();

        DrawControlls();

        DrawTolerance();

        DrawUI();

        DrawIO();

        DrawVisualisation();
    }

    private void DrawHead()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("��������� ���������� 4�4", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
        EditorGUILayout.LabelField("����� ������ ��������", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });
        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("������, ���������� � ������ ��������� �������. " +
            "��������� ������� �� ���������� ���������, ���������, ����������� � ����������� ������ ��������. " +
            "��������� ��� ����� model.json � space.json, � ������� ���������� ��������� ������ ������� 4�4. " +
            "�� ������ ������ ���� offset.json � ���������� ��������� ��������.", MessageType.None);
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    private void DrawIO()
    {
        EditorGUILayout.LabelField("������� ������", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });

        _sModelFileName.stringValue = EditorGUILayout.TextField(new GUIContent("Model File Name", "����, ���������� ��������� ������ (model)."), _sModelFileName.stringValue);
        _sSpaceFileName.stringValue = EditorGUILayout.TextField(new GUIContent("Space File Name", "����, ���������� ��������� ������ ������������ (space)."), _sSpaceFileName.stringValue);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("������ ������", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });
        EditorGUILayout.LabelField("Offset File Name", _sOffsetFileName.stringValue);

        EditorGUILayout.Space();

        if (GUILayout.Button("��������� ������� ������ (Input)", new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fixedHeight = 30 }))
        {
            _matrixController.Init();
        }
        if (GUILayout.Button("��������� ���������� ������ (Output)", new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fixedHeight = 30 }))
        {
            _matrixController.SaveOffsetMatrix();
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    private void DrawControlls()
    {
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("����������", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("������ ��������:", _sTaskIsRunning.boolValue.ToString());
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("��������� ����� offset", new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fixedHeight = 30 }))
            {
                _matrixController.Init();
                _matrixController.StartOffsetCoroutine();
            }
            if (GUILayout.Button("���������� �����", new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fixedHeight = 30 }))
            {
                _matrixController.StopOffsetCoroutine(() => MyDebug.Log("������ ���� ����������� ����� ���������", "#FFD700"));
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
    }

    private void DrawTolerance()
    {
        EditorGUILayout.LabelField("��������", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });
        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(_sTaskIsRunning.boolValue);
        _matrixController._tolerance = EditorGUILayout.IntSlider(new GUIContent("Tolerance", "�������� ����������� ����������� ��� ��������� ������ ����� �������."), _matrixController._tolerance, 1, 5);
        EditorGUI.EndDisabledGroup();

        _matrixController._epsilon = _matrixController._tolerance switch
        {
            1 => 0.05,
            2 => 0.005,
            3 => 0.0005,
            4 => 0.00005,
            5 => 0.000005,
            _ => 0.000005,
        };

        EditorGUILayout.LabelField("Epsilon:", _matrixController._epsilon.ToString());

        _sEpsilonEffectsOnFileName.boolValue = EditorGUILayout.Toggle(new GUIContent("Effects on FileName", "�������� �� ����������� � �������� ��������� �����?"), _sEpsilonEffectsOnFileName.boolValue);

        if (_sEpsilonEffectsOnFileName.boolValue)
            _sOffsetFileName.stringValue = $"offset_{_matrixController._epsilon}.json";
        else
            _sOffsetFileName.stringValue = $"offset.json";
    }

    private void DrawUI()
    {
        EditorGUILayout.LabelField("UI", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });
        EditorGUILayout.Space();
        _matrixController.statusText = (Text)EditorGUILayout.ObjectField(new GUIContent("Status Text", "����� ���������� �� ����������� ������� �������� � ���������."), _matrixController.statusText, typeof(Text), true);
        _matrixController.offsetCounterText = (Text)EditorGUILayout.ObjectField(new GUIContent("Offset Count Text", "����� ���������� �� ����������� ���������� ��������� ��������."), _matrixController.offsetCounterText, typeof(Text), true);
        EditorGUILayout.Space();
    }

    private void DrawVisualisation()
    {
        EditorGUILayout.LabelField("������������", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });
        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            _matrixController._initCount = EditorGUILayout.IntField(new GUIContent("Spawn Count", "���������� ����������������� ������� �� ������."), _matrixController._initCount);

            _matrixController._model = (Model)EditorGUILayout.ObjectField(new GUIContent("Model Prefab", "������ ������� ������."), _matrixController._model, typeof(Model), false);

            _matrixController._modelGroupParent = (Transform)EditorGUILayout.ObjectField(new GUIContent("Model Group Object", "������������ ������ Model."), _matrixController._modelGroupParent, typeof(Transform), true);
        }

        _matrixController._scaleFactor = EditorGUILayout.Slider(new GUIContent("Scale Factor", "�������� ������� ���������."), _matrixController._scaleFactor, 1f, 5f);
    }
}
#endif
