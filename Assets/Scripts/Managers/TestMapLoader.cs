using System.Collections;
using UnityEngine;

public class TestMapLoader : MonoBehaviour
{
    [Header("Testing Components")]
    public MapGenerator mapGenerator;
    public CameraSwitcher cameraSwitcher;
    
    [Header("Test Settings")]
    public int testStageNumber = 1;

    IEnumerator Start()
    {
        // 카메라 스위처가 초기화를 마칠 때까지 1프레임 대기
        yield return null;

        if (mapGenerator != null)
        {
            Debug.Log($"[TestMapLoader] 게임 시작! {testStageNumber}번 스테이지 맵 생성 테스트를 시작합니다.");
            mapGenerator.LoadStage(testStageNumber);
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
                Debug.Log("[TestMapLoader] 스페이스바 입력! 카메라 전환 테스트");
                cameraSwitcher.SwitchCamera();
            }
            else
            {
                Debug.LogWarning("[TestMapLoader] CameraSwitcher가 연결되어 있지 않습니다!");
            }
        }
    }
}
