using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// VR에서는 메뉴 씬(Intro/Start/main)에서 설정 버튼을 없앤다. 메뉴용 설정창이 VR에서 제대로 보이지 않기 때문이다.
/// 설정은 플레이 씬에서만 연다. 데스크톱/폰에서는 아무것도 하지 않는다.
///
/// - 설정 버튼은 이름이 아니라 버튼이 부르는 함수(OnSettingsClicked / OnSettingsButtonClicked)로 찾는다.
///   씬 파일을 고치지 않아도 되고, 버튼 이름이 바뀌어도 동작한다.
/// - 같은 세로줄에 쌓인 버튼(시작 씬의 시작하기/설정/게임종료)에서 설정 버튼이 빠지면,
///   남은 버튼을 원래 묶음의 가운데 기준으로 같은 간격으로 다시 배치해 빈자리를 메운다.
/// - 한 번 처리한 뒤에는 숨겨진 버튼이 더는 보이지 않으므로 여러 번 불려도 결과가 같다.
/// </summary>
public static class VRMenuTrim
{
    private static readonly string[] SettingsMethods = { "OnSettingsClicked", "OnSettingsButtonClicked" };
    private const float SameColumnTolerance = 1f;

    public static void Apply()
    {
        if (!SceneIds.IsMenu(SceneManager.GetActiveScene().name)) return;

        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            if (!OpensSettings(button)) continue;
            HideAndCloseGap(button);
        }
    }

    private static bool OpensSettings(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            string method = button.onClick.GetPersistentMethodName(i);
            foreach (var name in SettingsMethods)
                if (method == name) return true;
        }
        return false;
    }

    private static void HideAndCloseGap(Button hidden)
    {
        var hiddenRt = hidden.transform as RectTransform;
        var column = new List<RectTransform>();
        if (hiddenRt != null && hiddenRt.parent != null)
        {
            foreach (var other in hiddenRt.parent.GetComponentsInChildren<Button>(false))
            {
                if (other == hidden) continue;
                var rt = other.transform as RectTransform;
                if (rt == null || rt.parent != hiddenRt.parent) continue;
                if (!SameColumn(rt, hiddenRt)) continue;
                column.Add(rt);
            }
        }

        Debug.Log($"[VRMenuTrim] 설정 버튼 '{hidden.name}' 숨김 (씬={SceneManager.GetActiveScene().name}, 같은 줄의 다른 버튼 {column.Count}개)");
        hidden.gameObject.SetActive(false);

        // 같은 줄에 남은 버튼이 없으면(main의 톱니 버튼처럼 혼자 있는 경우) 숨기기만 한다.
        if (column.Count == 0) return;

        var all = new List<RectTransform>(column) { hiddenRt };
        all.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y)); // 위에서 아래로

        float top = all[0].anchoredPosition.y;
        float bottom = all[all.Count - 1].anchoredPosition.y;
        float pitch = (top - bottom) / (all.Count - 1);
        float center = (top + bottom) * 0.5f;

        column.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
        for (int i = 0; i < column.Count; i++)
        {
            float y = center + ((column.Count - 1) * 0.5f - i) * pitch;
            var pos = column[i].anchoredPosition;
            column[i].anchoredPosition = new Vector2(pos.x, y);
        }
    }

    private static bool SameColumn(RectTransform a, RectTransform b)
    {
        return a.anchorMin == b.anchorMin && a.anchorMax == b.anchorMax &&
               Mathf.Abs(a.anchoredPosition.x - b.anchoredPosition.x) < SameColumnTolerance &&
               Mathf.Abs(a.sizeDelta.x - b.sizeDelta.x) < SameColumnTolerance;
    }
}
