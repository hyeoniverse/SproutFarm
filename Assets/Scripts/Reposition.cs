using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 땅(Ground·Grass)은 20칸짜리 조각을 가로·세로 3장씩 깔고, 카메라에서 반 판(30칸)보다 멀어진 조각을
// 반대편 끝으로 옮겨 끝없이 이어지게 한다. 씬에는 2×2장만 두고, 나머지 5장은 시작할 때 복사해 채운다.
// 3×3장이면 카메라 둘레 어느 쪽으로든 20칸 이상 땅이 깔려 있으므로, 화면이 40칸까지 커져도 바탕이 보이지 않는다.
public class Reposition : MonoBehaviour
{
    public const float pieceSize = 20f;
    private const float span = pieceSize * 3;           // 3장을 이은 길이: 조각이 옮겨 가는 거리
    public const float coveredView = span - pieceSize;   // 땅이 빈틈없이 덮는 화면 크기 (40칸)

    private static bool copying;
    private static readonly Dictionary<TilemapRenderer, List<TilemapRenderer>> copies = new Dictionary<TilemapRenderer, List<TilemapRenderer>>();

    private void Awake()
    {
        if (copying || !CompareTag("Ground"))
            return;

        // 씬의 2×2장 중 왼쪽·아래쪽 조각을 오른쪽·위쪽 끝에 한 장씩 더 놓아 3×3장을 만든다
        bool left = transform.localPosition.x < 0;
        bool bottom = transform.localPosition.y < 0;
        if (left)
            Copy(new Vector3(pieceSize * 2, 0f));
        if (bottom)
            Copy(new Vector3(0f, pieceSize * 2));
        if (left && bottom)
            Copy(new Vector3(pieceSize * 2, pieceSize * 2));
    }

    private void OnDestroy()
    {
        TilemapRenderer tilemapRenderer = GetComponent<TilemapRenderer>();
        if (tilemapRenderer != null)
            copies.Remove(tilemapRenderer);
    }

    private void Copy(Vector3 offset)
    {
        copying = true;
        GameObject copy = Instantiate(gameObject, transform.position + offset, transform.rotation, transform.parent);
        copying = false;
        copy.name = $"{name} copy";

        TilemapRenderer original = GetComponent<TilemapRenderer>();
        if (!copies.TryGetValue(original, out List<TilemapRenderer> list))
        {
            list = new List<TilemapRenderer>();
            copies[original] = list;
        }
        list.Add(copy.GetComponent<TilemapRenderer>());
    }

    // 원래 타일맵들에 시작할 때 만든 복사본을 더한 목록 (꽃·새싹 타일맵을 다루는 스크립트가 복사본도 함께 다루도록)
    public static TilemapRenderer[] WithCopies(TilemapRenderer[] renderers)
    {
        var all = new List<TilemapRenderer>(renderers);
        foreach (TilemapRenderer tilemapRenderer in renderers)
        {
            if (tilemapRenderer != null && copies.TryGetValue(tilemapRenderer, out List<TilemapRenderer> list))
                all.AddRange(list);
        }
        return all.ToArray();
    }

    private void LateUpdate()
    {
        if (!CompareTag("Ground"))
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Vector3 offset = transform.position - mainCamera.transform.position;
        float shiftX = Mathf.Round(offset.x / span) * span;
        float shiftY = Mathf.Round(offset.y / span) * span;
        if (shiftX != 0f || shiftY != 0f)
            transform.position -= new Vector3(shiftX, shiftY, 0f);
    }
}
