using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 나무·해바라기처럼 키 큰 타일은 캐릭터보다 위에 그려지는데, 밑둥 쪽에 겹친 꽃·새싹까지 가려 버린다.
// 그래서 꽃·새싹 타일맵을 나무 위 순서로 한 번 더 그리고, 키 큰 타일의 밑둥을 덮는 마스크 안에서만 보이게 한다.
// (캐릭터와 꽃·새싹은 같은 정렬 순서에서 y 위치로 앞뒤가 정해지므로 따로 덧그리지 않는다.)
public class GrassInFront : MonoBehaviour
{
    public TilemapRenderer[] grassRenderers;  // 꽃·새싹 타일맵들
    public Tilemap fence;                     // 이 타일맵에 타일이 있는 칸은 덧그리지 않는다 (울타리 뒤로 가려지는 부분)
    public TilemapRenderer[] tallRenderers;   // 나무·해바라기 타일맵들
    public int overlaySortingOrder = 8;       // 나무·해바라기(7) 바로 위
    public float baseHeight = 6f / 16f;       // 그림 맨 아래에서 이 높이까지가 밑둥 (타일 16픽셀 기준 6픽셀)

    private readonly Dictionary<Sprite, Rect> spriteOutlines = new Dictionary<Sprite, Rect>();
    private Sprite maskSprite;

    private void Start()
    {
        // 1x1 크기, 아래쪽 가운데가 기준점인 흰 사각형 (마스크 모양으로만 쓰인다)
        Texture2D white = Texture2D.whiteTexture;
        maskSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0f), white.width);

        foreach (TilemapRenderer grassRenderer in grassRenderers)
        {
            CreateOverlay(grassRenderer);
        }

        foreach (TilemapRenderer tallRenderer in tallRenderers)
        {
            AddBaseMasks(tallRenderer);
        }
    }

    // 꽃·새싹 타일맵을 나무 위 순서로 한 번 더 그리는 타일맵 (밑둥 마스크 안에서만 보임)
    private void CreateOverlay(TilemapRenderer grassRenderer)
    {
        Tilemap grass = grassRenderer.GetComponent<Tilemap>();
        GameObject overlayObject = new GameObject(grass.name + " Over Tall Tiles");
        overlayObject.layer = grass.gameObject.layer;
        overlayObject.transform.SetParent(grass.transform, false);

        Tilemap overlay = overlayObject.AddComponent<Tilemap>();
        overlay.tileAnchor = grass.tileAnchor;
        TilemapRenderer overlayRenderer = overlayObject.AddComponent<TilemapRenderer>();
        overlayRenderer.sharedMaterial = grassRenderer.sharedMaterial;
        overlayRenderer.sortingLayerID = grassRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = overlaySortingOrder;
        overlayRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        // 규칙 타일이 고른 꽃·새싹 그림을 그대로 옮긴다 (같은 그림은 타일 하나를 같이 쓴다)
        Dictionary<Sprite, Tile> tiles = new Dictionary<Sprite, Tile>();
        foreach (Vector3Int cell in grass.cellBounds.allPositionsWithin)
        {
            Sprite sprite = grass.GetSprite(cell);
            if (sprite == null)
                continue;
            if (fence != null && fence.HasTile(fence.WorldToCell(grass.GetCellCenterWorld(cell))))
                continue;

            if (!tiles.TryGetValue(sprite, out Tile tile))
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.flags = TileFlags.LockAll;
                tile.colliderType = Tile.ColliderType.None;
                tiles[sprite] = tile;
            }
            overlay.SetTile(cell, tile);
        }
    }

    // 나무·해바라기 타일마다 그림 맨 아래 밑둥에 마스크를 둔다 (움직이지 않으니 한 번만)
    private void AddBaseMasks(TilemapRenderer tallRenderer)
    {
        Tilemap tall = tallRenderer.GetComponent<Tilemap>();
        foreach (Vector3Int cell in tall.cellBounds.allPositionsWithin)
        {
            Sprite sprite = tall.GetSprite(cell);
            if (sprite == null)
                continue;

            Rect outline = OutlineOf(sprite);
            Vector3 offset = tall.GetTransformMatrix(cell).GetColumn(3);
            GameObject maskObject = new GameObject("TallTileBaseMask");
            maskObject.transform.SetParent(transform, false);
            maskObject.transform.position = tall.GetCellCenterWorld(cell) + offset + new Vector3(outline.center.x, outline.yMin, 0f);
            maskObject.transform.localScale = new Vector3(outline.width, baseHeight, 1f);
            SpriteMask mask = maskObject.AddComponent<SpriteMask>();
            mask.sprite = maskSprite;
            // 덧그린 꽃·새싹만 드러내고, 마스크를 쓰는 다른 렌더러(울타리 칸의 꽃·새싹, 집 테두리 등)에는 영향을 주지 않는다
            mask.isCustomRangeActive = true;
            mask.frontSortingLayerID = tallRenderer.sortingLayerID;
            mask.frontSortingOrder = overlaySortingOrder;
            mask.backSortingLayerID = tallRenderer.sortingLayerID;
            mask.backSortingOrder = overlaySortingOrder - 1;
        }
    }

    // 그림에서 실제로 칠해진 부분의 범위 (스프라이트 기준점 기준, 유닛 단위)
    private Rect OutlineOf(Sprite sprite)
    {
        if (spriteOutlines.TryGetValue(sprite, out Rect outline))
            return outline;

        Vector2[] vertices = sprite.vertices;
        Vector2 min = vertices[0];
        Vector2 max = vertices[0];
        foreach (Vector2 vertex in vertices)
        {
            min = Vector2.Min(min, vertex);
            max = Vector2.Max(max, vertex);
        }
        outline = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        spriteOutlines[sprite] = outline;
        return outline;
    }
}
