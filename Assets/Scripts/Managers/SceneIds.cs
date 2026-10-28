using System;

public enum SceneKind
{
    Menu,     // 인트로/시작/메인 등 퍼즐을 푸는 화면이 아닌 곳
    Gameplay, // 스테이지/연습 등 퍼즐을 푸는 곳
    Editor    // 맵 만들기
}

/// <summary>
/// 씬 이름과 종류를 정의하는 단 하나의 장소.
/// 씬이 새로 생기면 상수를 추가하고 KindOf에 분류만 해주면 된다.
/// 목록에 없는 이름은 Gameplay로 취급하므로, 새 스테이지 씬은 따로 등록하지 않아도 게임 씬처럼 동작한다.
/// </summary>
public static class SceneIds
{
    public const string Intro = "Intro";
    public const string Start = "Start";
    public const string Main = "main";
    public const string Play = "Play";
    public const string Practice = "Practice";
    public const string Tutorial = "Stage_00";
    public const string Creative = "CreativeMode";

    private static readonly string[] Known =
    {
        Intro, Start, Main, Play, Practice, Tutorial, Creative
    };

    public static SceneKind KindOf(string sceneName)
    {
        switch (Canonical(sceneName))
        {
            case Intro:
            case Start:
            case Main:
            case "MuseumHub": // 더 이상 쓰지 않는 씬이지만 남아 있어도 메뉴로 취급
                return SceneKind.Menu;
            case Creative:
                return SceneKind.Editor;
            default:
                return SceneKind.Gameplay;
        }
    }

    public static bool IsMenu(string sceneName) => KindOf(sceneName) == SceneKind.Menu;

    /// <summary>퍼즐/에디터 화면(ESC로 일시정지, 인게임 UI 사용). 메뉴가 아닌 모든 씬.</summary>
    public static bool IsPlayScene(string sceneName) => !IsMenu(sceneName);

    /// <summary>"Main"처럼 대소문자가 다르게 적힌 이름을 실제 씬 이름으로 바로잡는다.</summary>
    public static string Canonical(string sceneName)
    {
        foreach (var known in Known)
            if (string.Equals(known, sceneName, StringComparison.OrdinalIgnoreCase)) return known;
        return sceneName;
    }
}
