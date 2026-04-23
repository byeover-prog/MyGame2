#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class FontReplacer : EditorWindow
{
    private TMP_FontAsset targetFont;

    [MenuItem("Tools/Font Replacer")]
    public static void ShowWindow()
    {
        GetWindow<FontReplacer>("Font Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("씬 전체 TMP 폰트 교체", EditorStyles.boldLabel);
        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "교체할 폰트", targetFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("씬 전체 교체"))
        {
            if (targetFont == null)
            {
                EditorUtility.DisplayDialog("오류", "폰트를 선택해주세요!", "확인");
                return;
            }
            ReplaceAllFonts();
        }
    }

    private void ReplaceAllFonts()
    {
        var allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var tmp in allTexts)
        {
            Undo.RecordObject(tmp, "Font Replace");
            tmp.font = targetFont;
            tmp.fontSharedMaterial = targetFont.material; // Material도 같이 교체
            EditorUtility.SetDirty(tmp);
            count++;
        }

        EditorUtility.DisplayDialog("완료", $"{count}개 텍스트 교체 완료!", "확인");
    }
}
#endif