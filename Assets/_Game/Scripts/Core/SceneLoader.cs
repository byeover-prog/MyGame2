using UnityEngine.SceneManagement;

/// <summary>
/// 씬 로드 유틸
/// - string을 직접 치지 말고 여기만 통해서 로드한다.
/// </summary>
public static class SceneLoader
{
    public static void Load(string scene_name)
    {
        // 간단 방어: 빈 문자열이면 아무것도 하지 않음
        if (string.IsNullOrEmpty(scene_name))
            return;

        SceneManager.LoadScene(scene_name);
    }
}
