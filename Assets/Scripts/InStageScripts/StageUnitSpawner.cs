using UnityEngine;
using System.Collections;

public class StageUnitSpawner : MonoBehaviour
{
    [Header("프리팹 설정")]
    public GameObject unitBasePrefab; // 아군 껍데기 프리팹
    public Transform baseTransform;   // 스폰 중심점 (DefenseBase 등)

    [Header("스폰 설정")]
    public float spawnRadius = 2.0f;

    private IEnumerator Start()
    {
        // 씬 로드 직후 매니저가 정리될 시간을 줍니다.
        yield return new WaitForSeconds(0.1f);

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[Spawner] InventoryManager 인스턴스를 찾을 수 없습니다! DontDestroyOnLoad 확인 필요.");
            yield break;
        }

        SpawnFormation();
    }

    private void SpawnFormation()
    {
        // InventoryManager에서 편성 데이터를 가져옴
        UnitData[] formation = InventoryManager.Instance.formationSlots;

        int spawnCount = 0;

        for (int i = 0; i < formation.Length; i++)
        {
            // 데이터가 있는지 검사
            if (formation[i] == null)
            {
                Debug.Log($"[Spawner] {i}번 슬롯이 비어있어 건너뜁니다.");
                continue;
            }

            // 1. 위치 계산
            Vector3 spawnPos = baseTransform != null ? baseTransform.position : Vector3.zero;
            spawnPos += new Vector3(Random.Range(-spawnRadius, spawnRadius), Random.Range(-spawnRadius, spawnRadius), 0);

            // 2. 프리팹 생성
            GameObject unitGo = Instantiate(unitBasePrefab, spawnPos, Quaternion.identity);

            // 3. 데이터 주입
            UnitStat unitStat = unitGo.GetComponent<UnitStat>();
            if (unitStat != null)
            {
                // 뽑기 시 저장했던 UnitData 전달
                unitStat.data = formation[i];

                // 장비 정보 주입 (UnitData에 장착 정보가 포함되어 있음)
                unitStat.currentWeapon = formation[i].equippedWeapon;
                unitStat.currentHelm = formation[i].equippedHelm;
                unitStat.currentChest = formation[i].equippedChest;

                // 스탯 갱신
                unitStat.RefreshStats();

                unitGo.name = $"Ally_{formation[i].unitName}_{i}";
                spawnCount++;
            }
        }

        Debug.Log($"[Spawner] 총 {spawnCount}명의 유닛이 소환되었습니다.");
    }
}