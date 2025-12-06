using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera[] cameras;
    private int currentCameraIndex = 0;

    void Start()
    {
        if (cameras == null || cameras.Length == 0) return;

        // 초기화: 0번 카메라만 켜기
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(false);
        }
        cameras[0].gameObject.SetActive(true);
        currentCameraIndex = 0;
    }

    // (기존) 버튼 누를 때마다 순서대로 변경
    public void SwitchCamera()
    {
        if (cameras.Length < 2) return;
        cameras[currentCameraIndex].gameObject.SetActive(false);
        currentCameraIndex++;
        if (currentCameraIndex >= cameras.Length) currentCameraIndex = 0;
        cameras[currentCameraIndex].gameObject.SetActive(true);
    }

    // (★ 추가된 함수) 특정 번호의 카메라로 즉시 변경
    public void SetSpecificCamera(int index)
    {
        if (index < 0 || index >= cameras.Length) return;

        // 현재 켜진 거 끄고
        cameras[currentCameraIndex].gameObject.SetActive(false);

        // 원하는 거 켜고
        currentCameraIndex = index;
        cameras[currentCameraIndex].gameObject.SetActive(true);
    }
}