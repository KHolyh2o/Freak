using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CustomLevelSelect : MonoBehaviour
{
    [Header("커스텀 맵 버튼들")]
    public Button[] slotButtons; // 슬롯 1, 2, 3 버튼 연결
    public TextMeshProUGUI[] slotTexts; // 버튼 안의 텍스트 연결

    void Start()
    {
        // 각 슬롯별로 저장된 파일이 있는지 확인
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i + 1; // 1번부터 시작
            string path = Path.Combine(Application.persistentDataPath, $"MapSlot_{slotIndex}.json");

            if (File.Exists(path))
            {
                // 파일이 있으면 버튼 활성화
                slotButtons[i].interactable = true;
                slotTexts[i].text = $"내 맵 {slotIndex} 플레이";

                // 버튼 클릭 시 실행할 함수 연결
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => LoadCustomMap(slotIndex));
            }
            else
            {
                // 파일 없으면 비활성화
                slotButtons[i].interactable = false;
                slotTexts[i].text = "비어 있음";
            }
        }
    }

    void LoadCustomMap(int slotIndex)
    {
        // 1. "몇 번 슬롯을 로드해라"라는 쪽지를 남김 (PlayerPrefs)
        PlayerPrefs.SetInt("AutoLoadSlot", slotIndex);

        // 2. 맵 에디터 씬으로 이동 (거기서 플레이할 거니까)
        SceneManager.LoadScene("CreativeMode");
    }
}