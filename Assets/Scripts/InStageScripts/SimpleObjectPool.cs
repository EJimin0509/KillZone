using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범용 풀링 스크립트
/// 적을 생성하고 삭제할 때 부하를 줄이기 위함
/// </summary>
public class SimpleObjectPool : MonoBehaviour
{
    public static SimpleObjectPool Instance;

    private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        Instance = this;
    }

    // 풀에서 꺼내기
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;

        if (!_poolDictionary.ContainsKey(key))
            _poolDictionary.Add(key, new Queue<GameObject>());

        if (_poolDictionary[key].Count > 0)
        {
            GameObject obj = _poolDictionary[key].Dequeue();
            obj.SetActive(true);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }
        else
        {
            return Instantiate(prefab, position, rotation);
        }
    }

    // 풀에 반납하기 (Destroy 대신 사용)
    public void ReturnToPool(GameObject obj)
    {
        string key = obj.name.Replace("(Clone)", "");
        obj.SetActive(false);
        _poolDictionary[key].Enqueue(obj);
    }
}