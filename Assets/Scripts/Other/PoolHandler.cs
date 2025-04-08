using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolHandler<T> where T : MonoBehaviour
{
    public bool autoExpand { get; set; } //should pool expand automaticaly

    public T _modelPrefab { get; } //prefab of bullet
    public Transform _modelGroup { get; } //parent gameObject on the scene with bullets objects in it as a child

    private List<T> modelPool;

    public PoolHandler(ref List<T> poolType, bool expand, T prefab, int size, Transform parentContainer) //констуктор 1, poolType - bulletPool => pool for bullets
    {
        _modelPrefab = prefab;
        _modelGroup = parentContainer;
        autoExpand = expand;

        CreatePool(ref poolType, size);
    }

    private void CreatePool(ref List<T> newPool, int size) //Creating pool, newPool - poolType - bulletPool => pool for bullets
    {
        newPool = new List<T>();

        for (int i = 0; i < size; i++)
            CreateObject(ref newPool);
    }

    private T CreateObject(ref List<T> pool, bool isActive = false) //isActive - true; creates new enabled object; isActive - false; creates unenabled object
    {
        var newObject = UnityEngine.Object.Instantiate(this._modelPrefab, this._modelGroup);

        newObject.gameObject.SetActive(isActive);
        pool.Add(newObject);

        return newObject;
    }

    public bool CheckAvailableObject(ref List<T> pool, out T availableObject)
    {
        foreach (var item in pool)
        {
            if(!item.gameObject.activeInHierarchy)
            {
                availableObject = item;
                availableObject.gameObject.SetActive(true);
                return true;
            }
        }

        availableObject = null;
        return false;
    }

    public T GetPoolObject(ref List<T> pool)
    {
        if (CheckAvailableObject(ref pool, out var availableObject))
            return availableObject;

        if (autoExpand)
            return CreateObject(ref pool, true);

        throw new Exception($"There is no more AvailableObjects in {nameof(pool)}. AutoExpand is not allowed!");
    }
}
