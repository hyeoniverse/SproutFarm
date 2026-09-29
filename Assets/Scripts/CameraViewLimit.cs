using UnityEngine;
using UnityEngine.Rendering.Universal;

// Pixel Perfect Camera는 기준 해상도를 화면에 정수 배로만 키우므로, 세로로 긴 폰처럼 화면 비율이 기준과 많이 다르면
// 한쪽으로 보이는 땅이 크게 늘어난다. 보이는 범위가 무한 맵의 땅이 덮는 크기(Reposition.coveredView)를 넘으면
// 기준 해상도를 줄여 배율을 올린다. 넘지 않으면 원래 기준 해상도를 그대로 쓴다.
[RequireComponent(typeof(Camera), typeof(PixelPerfectCamera))]
public class CameraViewLimit : MonoBehaviour
{
    private Camera viewCamera;
    private PixelPerfectCamera pixelPerfect;
    private int baseRefX;
    private int baseRefY;
    private int lastWidth = -1;
    private int lastHeight = -1;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
        pixelPerfect = GetComponent<PixelPerfectCamera>();
        baseRefX = pixelPerfect.refResolutionX;
        baseRefY = pixelPerfect.refResolutionY;
    }

    private void Update()
    {
        int width = viewCamera.pixelWidth;
        int height = viewCamera.pixelHeight;
        if (width == lastWidth && height == lastHeight)
            return;

        lastWidth = width;
        lastHeight = height;

        // Pixel Perfect Camera가 원래 기준 해상도로 고를 배율과, 보이는 범위를 땅 안에 넣으려면 필요한 배율
        int zoom = Mathf.Max(1, Mathf.Min(width / baseRefX, height / baseRefY));
        float maxPixels = Reposition.coveredView * pixelPerfect.assetsPPU;
        int needed = Mathf.Max(zoom, Mathf.CeilToInt(width / maxPixels), Mathf.CeilToInt(height / maxPixels));

        if (needed == zoom)
        {
            pixelPerfect.refResolutionX = baseRefX;
            pixelPerfect.refResolutionY = baseRefY;
        }
        else
        {
            pixelPerfect.refResolutionX = width / needed;
            pixelPerfect.refResolutionY = height / needed;
        }
    }
}
