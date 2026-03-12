using UnityEngine;
using UnityEditor;
using TMPro;

public class TMPFontChanger : EditorWindow
{
    public TMP_FontAsset targetFont;

    [MenuItem("Tools/TMP Font Changer")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontChanger>("Font Changer");
    }

    void OnGUI()
    {
        GUILayout.Label("모든 TMP 폰트 일괄 변경", EditorStyles.boldLabel);
        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("대상 폰트", targetFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("현재 씬 전체 변경"))
        {
            if (targetFont == null) return;

            // 씬에 있는 모든 TMP_Text 컴포넌트 찾기 (비활성화 된 것 포함)
            TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();

            int count = 0;
            foreach (TMP_Text text in allTexts)
            {
                // 프리팹 원본이 아닌 씬 내 오브젝트인지 확인
                if (!EditorUtility.IsPersistent(text.transform.gameObject))
                {
                    Undo.RecordObject(text, "Change TMP Font");
                    text.font = targetFont;
                    EditorUtility.SetDirty(text);
                    count++;
                }
            }
            Debug.Log($"{count}개의 텍스트 폰트를 변경했습니다.");
        }
    }
}