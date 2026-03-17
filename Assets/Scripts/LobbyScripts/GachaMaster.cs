using UnityEngine;
using UnityEngine.UI;

public class GachaMaster : MonoBehaviour
{
    public static GachaMaster Instance;

    [Header("Managers")]
    public UnitGachaManager unitManager;
    public EquipmentGachaManager equipManager;

    [Header("Common UI Components")]
    public Image commonResultImage;    // 유닛/장비 공용 이미지
    public Button unitDrawButton;      // 유닛 뽑기 버튼
    public Button equipDrawButton;     // 장비 뽑기 버튼
    public Button rerollButton;        // 유닛 전용 리롤 버튼
    public Button sharedConfirmButton; // 통합 확정 버튼
    public Sprite defaultSprite;

    [Header("UI Panels (Result Groups)")]
    public GameObject unitResultGroup;   // 유닛 스탯 TMP들이 담긴 부모 오브젝트
    public GameObject equipResultGroup;  // 장비 스탯 TMP들이 담긴 부모 오브젝트

    private bool _isUnitActive = false;

    private void Awake()
    {
        Instance = this;
        HideAllGroups();
        // 초기 상태: 유닛/장비 뽑기 버튼만 켜둠
        SetAllButtonStates(true, true, false, false);
    }

    private void HideAllGroups()
    {
        if (unitResultGroup) unitResultGroup.SetActive(false);
        if (equipResultGroup) equipResultGroup.SetActive(false);
    }

    // --- 버튼 상태 일괄 제어 함수 ---
    public void SetAllButtonStates(bool unitDraw, bool equipDraw, bool reroll, bool confirm)
    {
        if (unitDrawButton) unitDrawButton.interactable = unitDraw;
        if (equipDrawButton) equipDrawButton.interactable = equipDraw;
        if (rerollButton) rerollButton.interactable = reroll;
        if (sharedConfirmButton) sharedConfirmButton.interactable = confirm;
    }

    // 1. 유닛 뽑기 클릭 시
    public void OnClickUnitDraw()
    {
        _isUnitActive = true;

        if (unitResultGroup) unitResultGroup.SetActive(true);
        if (equipResultGroup) equipResultGroup.SetActive(false);

        unitManager.OnClickDraw();

        // 결과 나왔으니 드로우 버튼들은 끄고, 리롤과 컨펌을 켬
        SetAllButtonStates(false, false, true, true);
    }

    // 2. 장비 뽑기 클릭 시
    public void OnClickEquipDraw()
    {
        _isUnitActive = false;
        if (equipResultGroup) equipResultGroup.SetActive(true);
        if (unitResultGroup) unitResultGroup.SetActive(false);

        equipManager.OnClickEquipmentGacha();

        // 장비는 리롤이 없으므로 컨펌만 켬
        SetAllButtonStates(false, false, false, true);
    }

    // 3. 리롤 클릭 시 (유닛 매니저 연결)
    public void OnClickReroll()
    {
        if (unitResultGroup) unitResultGroup.SetActive(true);
        if (equipResultGroup) equipResultGroup.SetActive(false);

        unitManager.OnClickReroll();

        // 리롤은 1회 한정이므로 리롤 버튼 끔
        SetAllButtonStates(false, false, false, true);
    }

    // 4. 통합 확정 클릭 시
    public void OnClickConfirmAll()
    {
        if (_isUnitActive)
            unitManager.OnClickConfirm(); // 내부에서 gachaUI.SetButtonState(true, false) 호출 중일 것
        else
            equipManager.OnClickConfirm();

        if (commonResultImage != null && defaultSprite != null)
        {
            commonResultImage.sprite = defaultSprite;
        }

        HideAllGroups();
        // 여기서 다시 한번 확실하게 버튼들을 깨워줘야 합니다.
        SetAllButtonStates(true, true, false, false);
    }

    public void UpdateResultImage(Sprite s) => commonResultImage.sprite = s;
}