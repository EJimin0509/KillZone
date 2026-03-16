using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    void LateUpdate()
    {
        // 카메라의 방향과 일치시켜 UI가 회전하지 않고 정면을 보게 함
        transform.forward = Camera.main.transform.forward;
    }
}