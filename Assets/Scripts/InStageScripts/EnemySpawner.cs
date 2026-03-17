using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;
    public int spawnCount;
}

[System.Serializable]
public class WaveData
{
    public string waveName;
    public List<EnemySpawnInfo> enemyPool;
    public float spawnInterval;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Wave UI")]
    public TextMeshProUGUI waveInfoText; // "WAVE 1 / 3" 표시용
    public TextMeshProUGUI enemyCountText; // "ENEMIES: 10" 표시용

    [Header("Wave Settings")]
    [SerializeField] private List<WaveData> waves;
    private int _currentWaveIndex = 0;

    [Header("Spawn Area")]
    [SerializeField] private float minX;
    [SerializeField] private float maxX;
    [SerializeField] private float spawnY;

    [Header("Events")]
    public UnityEvent OnAllWavesCleared;

    private bool _isSpawnerStarted = false;

    private void Update()
    {
        // 5번 기능 핵심: 매 프레임마다 태그를 확인하여 UI에 생존 적 수 표시
        if (_isSpawnerStarted && enemyCountText != null)
        {
            int remainEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
            enemyCountText.text = $"남은 적: {remainEnemies}";
        }
    }

    private void UpdateWaveUI(int currentWave, int totalWaves)
    {
        if (waveInfoText != null)
            waveInfoText.text = $"WAVE {currentWave + 1} / {totalWaves}";
    }

    public void StartWaveSystem()
    {
        if (_isSpawnerStarted) return;

        if (waves.Count > 0 && _currentWaveIndex == 0)
        {
            _isSpawnerStarted = true;
            StartCoroutine(StartWave(_currentWaveIndex));
        }
    }

    private IEnumerator StartWave(int index)
    {
        UpdateWaveUI(index, waves.Count);

        WaveData currentWave = waves[index];

        // 1. 소환 리스트 생성
        List<GameObject> spawnList = new List<GameObject>();
        foreach (var info in currentWave.enemyPool)
        {
            for (int i = 0; i < info.spawnCount; i++)
            {
                spawnList.Add(info.enemyPrefab);
            }
        }

        // 2. Shuffle
        for (int i = 0; i < spawnList.Count; i++)
        {
            int randomIndex = Random.Range(i, spawnList.Count);
            GameObject temp = spawnList[i];
            spawnList[i] = spawnList[randomIndex];
            spawnList[randomIndex] = temp;
        }

        // 3. 순서대로 소환
        foreach (GameObject prefab in spawnList)
        {
            if (prefab != null)
            {
                SpawnEnemy(prefab);
            }
            yield return new WaitForSeconds(currentWave.spawnInterval);
        }

        // 4. 모든 적 처치 대기 (태그 "Enemy" 기준)
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);

        // 5. 다음 웨이브 이동
        _currentWaveIndex++;
        if (_currentWaveIndex < waves.Count)
        {
            yield return new WaitForSeconds(5f);
            StartCoroutine(StartWave(_currentWaveIndex));
        }
        else
        {
            // 모든 웨이브 클리어 시 UI 처리
            if (enemyCountText != null) enemyCountText.text = "CLEARED";
            _isSpawnerStarted = false;
            ResultUIController resultUI = FindAnyObjectByType<ResultUIController>();
            if (resultUI != null)
            {
                resultUI.ShowResult(true); // 코드로 직접 승리(true) 호출
            }

            OnAllWavesCleared?.Invoke();
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        float randomX = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(randomX, spawnY, 0);

        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.SpawnFromPool(prefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }
}