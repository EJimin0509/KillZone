using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("보유 목록 (실제 게임용 ScriptableObject)")]
    public List<UnitData> myUnits = new List<UnitData>();
    public List<EquipmentData> myEquipments = new List<EquipmentData>();

    [Header("스테이지 편성")]
    public UnitData[] formationSlots = new UnitData[10];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    // 게임 시작 시 혹은 데이터가 로드된 후 한 번 호출해서 ScriptableObject 리스트를 만듭니다.
    public void RefreshInventoryFromSaveData()
    {
        if (GameManager.Instance == null) return;

        // 1. 기존 리스트 초기화
        myUnits.Clear();
        myEquipments.Clear();

        // 2. 유닛 복구 루프
        foreach (var u in GameManager.Instance.ownedUnits)
        {
            UnitData unit = ScriptableObject.CreateInstance<UnitData>();
            unit.unitName = u.unitName;
            unit.spriteKey = u.spriteKey; // 문자열 키 복사
            unit.hp = u.hp;
            unit.melee = u.melee;
            unit.range = u.range;
            unit.repair = u.repair;
            unit.medic = u.medic;
            unit.will = u.will;
            unit.faith = u.faith;

            // 스프라이트 로드
            if (!string.IsNullOrEmpty(u.spriteKey))
            {
                // 1. 먼저 단일 파일로 로드 시도
                unit.unitSprite = Resources.Load<Sprite>($"Sprites/{u.spriteKey}");

                // 2. 만약 실패했다면 (Multiple Sprite인 경우)
                if (unit.unitSprite == null && u.spriteKey.Contains("_"))
                {
                    // "Hero_01_0"에서 "_"를 기준으로 파일명("Hero_01")만 추출
                    string fileName = u.spriteKey.Substring(0, u.spriteKey.LastIndexOf('_'));

                    // 해당 파일 내의 모든 스프라이트 조각들을 가져옴
                    Sprite[] allSprites = Resources.LoadAll<Sprite>($"Sprites/{fileName}");

                    // 그중 이름이 spriteKey와 일치하는 조각을 찾음
                    foreach (var s in allSprites)
                    {
                        if (s.name == u.spriteKey)
                        {
                            unit.unitSprite = s;
                            break;
                        }
                    }
                }

                // 최종 확인
                //if (unit.unitSprite == null)
                //{
                //    Debug.LogError($"[최종 로드 실패] {u.spriteKey}를 찾을 수 없습니다.");
                //}
            }

            myUnits.Add(unit);
        }
        Debug.Log($"유닛 SO 생성 완료: {myUnits.Count}개");

        // 3. 장비 복구 루프 (여기 신경 써서 확인하세요)
        foreach (var e in GameManager.Instance.ownedEquips)
        {
            EquipmentData equip = ScriptableObject.CreateInstance<EquipmentData>();
            equip.equipName = e.equipName;
            equip.spriteKey = e.spriteKey; // [핵심] 장비용 spriteKey도 반드시 할당
            equip.type = e.type;
            equip.attackRange = e.attackRange;
            equip.defense = e.defense;

            // 보너스 리스트 복제
            equip.additionalStatBonuses = new List<StatBonus>(e.bonuses);

            // 장비 스프라이트 로드
            if (!string.IsNullOrEmpty(e.spriteKey))
            {
                // 1. 단일 파일로 먼저 시도
                equip.equipSprite = Resources.Load<Sprite>($"Sprites/{e.spriteKey}");

                // 2. 실패 시 Multiple Sprite(조각) 로드 시도
                if (equip.equipSprite == null && e.spriteKey.Contains("_"))
                {
                    // "EquipSheet_0" -> "EquipSheet" 추출
                    int lastUnderscoreIndex = e.spriteKey.LastIndexOf('_');
                    string fileName = e.spriteKey.Substring(0, lastUnderscoreIndex);

                    Sprite[] allSprites = Resources.LoadAll<Sprite>($"Sprites/{fileName}");
                    foreach (var s in allSprites)
                    {
                        if (s.name == e.spriteKey)
                        {
                            equip.equipSprite = s;
                            break;
                        }
                    }
                }
            }

                myEquipments.Add(equip);
        }

        Debug.Log($"장비 SO 생성 완료: {myEquipments.Count}개");
        Debug.Log("인벤토리 전체 동기화 성공!");

        foreach (var unit in myUnits)
        {
            if (unit.equippedHelm != null) unit.equippedHelm.ownerUnit = unit;
            if (unit.equippedChest != null) unit.equippedChest.ownerUnit = unit;
            if (unit.equippedWeapon != null) unit.equippedWeapon.ownerUnit = unit;
        }
    }

    // 가챠 성공 시 호출
    public void AddUnit(UnitData unit)
    {
        if (unit == null) return;
        myUnits.Add(unit); // 현재 리스트에 추가
        GameManager.Instance.AddUnit(unit); // 실제 저장용 리스트에 추가 (GameManager가 저장함)
    }

    public void AddEquipment(EquipmentData equip)
    {
        if (equip == null) return;
        myEquipments.Add(equip);
        GameManager.Instance.AddEquipment(equip);
    }
}