using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveData
{
    public string waveName;
    public GameObject enemyPrefab; // 소환할 적 프리팹
    public int enemyCount;         // 이 웨이브에 나올 총 적 수
    public float spawnInterval;    // 적 생성 간격
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private List<WaveData> waves; // 3개의 웨이브 설정
    private int _currentWaveIndex = 0;

    [Header("Spawn Area")]
    [SerializeField] private float minX; // 스폰 가능 최소 X
    [SerializeField] private float maxX; // 스폰 가능 최대 X
    [SerializeField] private float spawnY; // 스폰 Y 좌표 (고정)

    private int _remainingEnemiesInWave;
    private bool _isSpawnerStarted = false; // 웨이브 시작 여부

    /// <summary>
    /// 웨이브 시작을 외부에서 호출하기 위한 메서드
    /// </summary>
    public void StartWaveSystem()
    {
        if (_isSpawnerStarted) return; // 이미 시작되었다면 리턴

        if (waves.Count > 0 && _currentWaveIndex == 0)
        {
            _isSpawnerStarted = true;
            StartCoroutine(StartWave(_currentWaveIndex));
        }
    }

    /// <summary>
    /// 웨이브 로직 코루틴
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private IEnumerator StartWave(int index)
    {
        Debug.Log($"{waves[index].waveName} 시작!");
        WaveData currentWave = waves[index];
        _remainingEnemiesInWave = currentWave.enemyCount;

        for (int i = 0; i < currentWave.enemyCount; i++)
        {
            SpawnEnemy(currentWave.enemyPrefab);
            yield return new WaitForSeconds(currentWave.spawnInterval);
        }

        // 모든 적이 소환된 후, 필드에 적이 0명이 될 때까지 대기
        // 일단 다음 웨이브로 넘어가기 위한 임시 로직
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);

        _currentWaveIndex++;
        if (_currentWaveIndex < waves.Count)
        {
            yield return new WaitForSeconds(5f); // 웨이브 사이 정비 시간
            StartCoroutine(StartWave(_currentWaveIndex));
        }
        else
        {
            Debug.Log("모든 웨이브 클리어!");
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        float randomX = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(randomX, spawnY, 0);

        // 오브젝트 풀에서 적을 가져옴
        SimpleObjectPool.Instance.SpawnFromPool(prefab, spawnPos, Quaternion.identity);
    }
}