using UnityEngine;
using UnityEngine.Tilemaps;

// 울타리 칸에 꽃·새싹이 겹칠 때, 기둥 밑둥 쪽에서는 꽃·새싹이 울타리 앞에, 그 위(가로대·기둥 윗부분)에서는
// 울타리가 꽃·새싹 앞에 보이게 한다. 울타리 타일맵 자체는 풀 장식보다 아래에 그리고, 같은 울타리를 풀 장식 위에
// 한 번 더 그리되 칸마다 아래쪽 baseHeight만큼은 마스크로 가려 그 부분에서는 풀 장식이 앞에 보이게 한다.
[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
public class FenceOverlay : MonoBehaviour
{
    public TileBase fenceTile;                 // 이 타일이 놓인 칸만 덧그린다 (같은 타일맵의 벽 조각 등은 빼고)
    public int overlaySortingOrder = -5;       // 풀 장식 타일맵 바로 위
    public float baseHeight = 7f / 16f;        // 칸 아래에서 이 높이까지는 풀 장식이 울타리 앞에 보인다 (타일 16픽셀 기준 7픽셀)

    private void Start()
    {
        Tilemap fence = GetComponent<Tilemap>();
        TilemapRenderer fenceRenderer = GetComponent<TilemapRenderer>();

        GameObject overlayObject = new GameObject("FenceOverlay");
        overlayObject.layer = gameObject.layer;
        overlayObject.transform.SetParent(transform.parent, false);
        overlayObject.transform.localPosition = transform.localPosition;
        overlayObject.transform.localRotation = transform.localRotation;
        overlayObject.transform.localScale = transform.localScale;

        Tilemap overlay = overlayObject.AddComponent<Tilemap>();
        overlay.tileAnchor = fence.tileAnchor;
        TilemapRenderer overlayRenderer = overlayObject.AddComponent<TilemapRenderer>();
        overlayRenderer.sharedMaterial = fenceRenderer.sharedMaterial;
        overlayRenderer.sortingLayerID = fenceRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = overlaySortingOrder;
        overlayRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

        // 1x1 칸 크기, 아래쪽 가운데가 기준점인 흰 사각형 (마스크 모양으로만 쓰인다)
        Texture2D white = Texture2D.whiteTexture;
        Sprite maskSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0f), white.width);
        Vector3 cellSize = fence.layoutGrid.cellSize;

        foreach (Vector3Int cell in fence.cellBounds.allPositionsWithin)
        {
            if (fence.GetTile(cell) != fenceTile)
                continue;

            // 규칙 타일이 고른 모양 그대로 옮기려고 실제로 그려진 스프라이트·변환·색을 복사한다
            Tile copy = ScriptableObject.CreateInstance<Tile>();
            copy.sprite = fence.GetSprite(cell);
            copy.color = fence.GetColor(cell);
            copy.transform = fence.GetTransformMatrix(cell);
            copy.flags = TileFlags.LockAll;
            copy.colliderType = Tile.ColliderType.None;
            overlay.SetTile(cell, copy);

            GameObject maskObject = new GameObject("FenceBaseMask");
            maskObject.layer = gameObject.layer;
            maskObject.transform.SetParent(overlayObject.transform, false);
            maskObject.transform.position = fence.CellToWorld(cell) + new Vector3(cellSize.x / 2f, 0f, 0f);
            maskObject.transform.localScale = new Vector3(cellSize.x, cellSize.y * baseHeight, 1f);
            SpriteMask mask = maskObject.AddComponent<SpriteMask>();
            mask.sprite = maskSprite;
            // 덧그린 울타리만 가리고, 마스크를 쓰는 다른 렌더러(집 테두리 등)에는 영향을 주지 않는다
            mask.isCustomRangeActive = true;
            mask.frontSortingLayerID = overlayRenderer.sortingLayerID;
            mask.frontSortingOrder = overlaySortingOrder;
            mask.backSortingLayerID = overlayRenderer.sortingLayerID;
            mask.backSortingOrder = overlaySortingOrder - 1;
        }
    }
}
