using System.Collections;
using UnityEngine;

public class TestMapLoader : MonoBehaviour
{
    public MapGenerator mapGenerator;
    public CameraSwitcher cameraSwitcher;

    IEnumerator Start()
    {
        yield return null; // CameraSwitcher 초기화 대기

        // PlayerPrefs에서 선택된 스테이지 번호 읽기 (기본값: 1)
        int stageNumber = PlayerPrefs.GetInt("SelectedStage", 1);

        if (mapGenerator != null)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Practice")
            {
                // Practice 씬일 경우 CSV 문자열 생성 및 로드
                string missingSkill = PlayerPrefs.GetString("PracticeSkill", "Spatial");
                string practiceCsv = PracticeMapGenerator.Instance.GeneratePracticeMapCSV(missingSkill);
                mapGenerator.LoadStageFromString(practiceCsv);
            }
            else
            {
                // 정규 스테이지 로드
                mapGenerator.LoadStage(stageNumber);
            }
        }
        else
        {
            Debug.LogError("[TestMapLoader] MapGenerator가 연결되어 있지 않습니다!");
        }
    }

    void Update()
    {
        // 스페이스바를 누를 때마다 카메라 시점이 순차적으로 바뀜
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (cameraSwitcher != null)
            {
                cameraSwitcher.SwitchCamera();
            }
        }
    }
}
