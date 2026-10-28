using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 이동 규칙을 모아 둔 단 하나의 장소. 씬 이동은 SceneManager.LoadScene을 직접 부르지 말고 여기를 거친다.
///
/// 흐름
///   Intro → Start → main → (스테이지 N) Play / (튜토리얼) Stage_00
///   Play  → (AI 연습 제안) Practice → 클리어 후 "다음"을 누르면 원래 Play 스테이지로 복귀
///   Play  → 클리어 후 "다음"은 SelectedStage + 1 로 같은 씬을 다시 로드
///   Stage_00(튜토리얼) → 클리어 후 "다음"은 main
/// </summary>
public static class SceneFlow
{
    private const string KeySelectedStage = "SelectedStage";
    private const string KeyPracticeSkill = "PracticeSkill";

    // Practice에서 돌아갈 씬. 연습을 시작한 씬(Play 등)을 기억한다.
    private static string _practiceReturnScene;

    public static string Current => SceneManager.GetActiveScene().name;

    /// <summary>이름으로 씬을 연다. 시간 정지를 풀고 대소문자를 바로잡는다.</summary>
    public static void Load(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneIds.Canonical(sceneName));
    }

    public static void ToStart() => Load(SceneIds.Start);

    public static void ToMain() => Load(SceneIds.Main);

    public static void ToTutorial() => Load(SceneIds.Tutorial);

    public static void Restart() => Load(Current);

    /// <summary>스테이지 번호(0부터)를 저장하고 Play 씬으로 간다.</summary>
    public static void ToStage(int stageIndex)
    {
        PlayerPrefs.SetInt(KeySelectedStage, stageIndex);
        PlayerPrefs.Save();
        Load(SceneIds.Play);
    }

    /// <summary>부족한 개념(skill)을 저장하고 Practice 씬으로 간다. 끝나면 지금 씬으로 돌아올 수 있다.</summary>
    public static void ToPractice(string skill)
    {
        PlayerPrefs.SetString(KeyPracticeSkill, skill);
        PlayerPrefs.Save();

        string current = Current;
        if (current != SceneIds.Practice) _practiceReturnScene = current;
        Load(SceneIds.Practice);
    }

    /// <summary>클리어 패널의 "다음" 버튼. 지금 씬에 따라 갈 곳이 달라진다.</summary>
    public static void Next()
    {
        string current = Current;

        if (current == SceneIds.Practice)
        {
            // 진행도(SelectedStage)는 건드리지 않고 연습을 시작했던 씬으로 돌아간다.
            string back = string.IsNullOrEmpty(_practiceReturnScene) ? SceneIds.Play : _practiceReturnScene;
            _practiceReturnScene = null;
            Load(back);
        }
        else if (current == SceneIds.Tutorial)
        {
            ToMain();
        }
        else
        {
            int stage = PlayerPrefs.GetInt(KeySelectedStage, 0);
            PlayerPrefs.SetInt(KeySelectedStage, stage + 1);
            PlayerPrefs.Save();
            Load(current);
        }
    }
}
