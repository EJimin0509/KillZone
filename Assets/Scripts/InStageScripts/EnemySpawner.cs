using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // 리스트 처리를 위해 추가

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab; // 적 프리팹
    public int spawnCount;         // 이 종류의 적을 몇 마리 소환할 것인가?
}

[System.Serializable]
public class WaveData
{
    public string waveName;
    public List<EnemySpawnInfo> enemyPool; // 소환할 적 구성 정보
    public float spawnInterval;            // 적 생성 간격
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private List<WaveData> waves; // 웨이브 설정 리스트
    private int _currentWaveIndex = 0;

    [Header("Spawn Area")]
    [SerializeField] private float minX;   // 스폰 가능 최소 X
    [SerializeField] private float maxX;   // 스폰 가능 최대 X
    [SerializeField] private float spawnY; // 스폰 Y 좌표 (고정)

    private bool _isSpawnerStarted = false; // 웨이브 시작 여부

    /// <summary>
    /// 외부(UI 등)에서 웨이브를 시작하기 위해 호출하는 메서드
    /// </summary>
    public void StartWaveSystem()
    {
        if (_isSpawnerStarted) return;

        if (waves.Count > 0 && _currentWaveIndex == 0)
        {
            _isSpawnerStarted = true;
            StartCoroutine(StartWave(_currentWaveIndex));
        }
    }

    /// <summary>
    /// 웨이브 핵심 로직
    /// </summary>
    private IEnumerator StartWave(int index)
    {
        WaveData currentWave = waves[index];
        Debug.Log($"<color=cyan>{currentWave.waveName} 시작!</color>");

        // 1. 이번 웨이브에서 소환할 적들을 모두 리스트에 담기 (수량 정확히 보장)
        List<GameObject> spawnList = new List<GameObject>();
        foreach (var info in currentWave.enemyPool)
        {
            for (int i = 0; i < info.spawnCount; i++)
            {
                spawnList.Add(info.enemyPrefab);
            }
        }

        // 2. 소환 순서를 무작위로 섞기 (Shuffle 로직)
        // 리스트 내용은 그대로 유지하면서 순서만 섞어 다양한 재미를 줌
        for (int i = 0; i < spawnList.Count; i++)
        {
            int randomIndex = Random.Range(i, spawnList.Count);
            GameObject temp = spawnList[i];
            spawnList[i] = spawnList[randomIndex];
            spawnList[randomIndex] = temp;
        }

        // 3. 섞인 리스트 순서대로 소환 실행
        foreach (GameObject prefab in spawnList)
        {
            if (prefab != null)
            {
                SpawnEnemy(prefab);
            }
            yield return new WaitForSeconds(currentWave.spawnInterval);
        }

        // 4. 필드 내 적이 모두 죽을 때까지 대기
        // (참고: 적이 많을 경우 성능을 위해 별도의 카운트 변수 방식을 권장하지만, 일단 기존 로직 유지)
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);

        Debug.Log($"<color=yellow>{currentWave.waveName} 클리어!</color>");

        // 5. 다음 웨이브로 이동
        _currentWaveIndex++;
        if (_currentWaveIndex < waves.Count)
        {
            yield return new WaitForSeconds(5f); // 웨이브 사이 정비 시간
            StartCoroutine(StartWave(_currentWaveIndex));
        }
        else
        {
            Debug.Log("<color=green>축하합니다! 모든 웨이브를 클리어했습니다!</color>");
            _isSpawnerStarted = false; // 종료 후 필요시 재시작 가능하도록 플래그 초기화
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        float randomX = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(randomX, spawnY, 0);

        // SimpleObjectPool을 통해 정확히 해당 프리팹을 소환
        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.SpawnFromPool(prefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // 오브젝트 풀이 없을 경우를 대비한 백업 (테스트용)
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }
}