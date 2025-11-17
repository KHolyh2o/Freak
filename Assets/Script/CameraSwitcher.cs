using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    // 인스펙터에서 3개의 카메라를 연결할 배열
    public Camera[] cameras;

    private int currentCameraIndex = 0;

    void Start()
    {
        // 씬에 카메라가 연결되어 있는지 확인
        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogError("CameraSwitcher에 카메라가 연결되지 않았습니다!");
            return;
        }

        // 1. 모든 카메라를 비활성화
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(false);
        }

        // 2. 첫 번째 카메라(인덱스 0)만 활성화
        cameras[0].gameObject.SetActive(true);
        currentCameraIndex = 0;
    }

    /// <summary>
    /// 이 함수를 버튼이 호출합니다.
    /// </summary>
    public void SwitchCamera()
    {
        if (cameras.Length < 2) return; // 카메라가 2대 미만이면 순환할 필요 없음

        // 1. 현재 켜져있는 카메라를 끈다
        cameras[currentCameraIndex].gameObject.SetActive(false);

        // 2. 다음 카메라 인덱스로 이동
        currentCameraIndex++;

        // 3. 만약 인덱스가 배열의 끝을 넘어가면, 다시 0번(처음)으로
        if (currentCameraIndex >= cameras.Length)
        {
            currentCameraIndex = 0;
        }

        // 4. 새로 선택된 카메라를 켠다
        cameras[currentCameraIndex].gameObject.SetActive(true);
    }
}