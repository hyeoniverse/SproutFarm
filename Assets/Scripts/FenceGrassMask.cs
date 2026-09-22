using UnityEngine;
using UnityEngine.Tilemaps;

// 꽃·새싹은 캐릭터와 울타리보다 위에 그리는데, 울타리 칸에 겹칠 때는 기둥 밑둥 쪽에서만 울타리 앞에 보이고
// 그 위(가로대·기둥 윗부분)에서는 울타리 뒤로 가려져야 자연스럽다. 그래서 울타리 칸마다 아래쪽 baseHeight를 뺀
// 윗부분에 마스크를 두고, 꽃·새싹 타일맵(Mask Interaction: Visible Outside Mask)이 그 안에서는 그려지지 않게 한다.
[RequireComponent(typeof(Tilemap))]
public class FenceGrassMask : MonoBehaviour
{
    public TileBase fenceTile;             // 이 타일이 놓인 칸에만 마스크를 둔다 (같은 타일맵의 벽 조각 등은 빼고)
    public TilemapRenderer grassRenderer;  // 가릴 꽃·새싹 타일맵 (정렬 순서가 같은 다른 꽃·새싹 타일맵도 함께 가려진다)
    public float baseHeight = 7f / 16f;    // 칸 아래에서 이 높이까지는 꽃·새싹이 울타리 앞에 보인다 (타일 16픽셀 기준 7픽셀)

    private void Start()
    {
        Tilemap fence = GetComponent<Tilemap>();

        // 1x1 칸 크기, 아래쪽 가운데가 기준점인 흰 사각형 (마스크 모양으로만 쓰인다)
        Texture2D white = Texture2D.whiteTexture;
        Sprite maskSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0f), white.width);
        Vector3 cellSize = fence.layoutGrid.cellSize;

        foreach (Vector3Int cell in fence.cellBounds.allPositionsWithin)
        {
            if (fence.GetTile(cell) != fenceTile)
                continue;

            GameObject maskObject = new GameObject("FenceGrassMask");
            maskObject.layer = gameObject.layer;
            maskObject.transform.SetParent(transform, false);
            maskObject.transform.position = fence.CellToWorld(cell) + new Vector3(cellSize.x / 2f, cellSize.y * baseHeight, 0f);
            maskObject.transform.localScale = new Vector3(cellSize.x, cellSize.y * (1f - baseHeight), 1f);
            SpriteMask mask = maskObject.AddComponent<SpriteMask>();
            mask.sprite = maskSprite;
            // 꽃·새싹 타일맵만 가리고, 마스크를 쓰는 다른 렌더러(집 테두리 등)에는 영향을 주지 않는다
            mask.isCustomRangeActive = true;
            mask.frontSortingLayerID = grassRenderer.sortingLayerID;
            mask.frontSortingOrder = grassRenderer.sortingOrder;
            mask.backSortingLayerID = grassRenderer.sortingLayerID;
            mask.backSortingOrder = grassRenderer.sortingOrder - 1;
        }
    }
}
