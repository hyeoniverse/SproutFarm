using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 꽃·새싹이 앞쪽(아래쪽)에 있을 때만 다른 그림을 덮게 한다.
// - 캐릭터·동물: 하반신(발·다리)은 꽃·새싹이 덮고, 상반신은 꽃·새싹보다 앞에 보인다.
// - 나무·해바라기 같은 높은 타일: 밑둥 쪽은 꽃·새싹이 덮고, 그 위는 꽃·새싹보다 앞에 보인다.
// 꽃·새싹 타일맵은 이들보다 아래에 그리고, 같은 꽃·새싹을 이들 바로 위 순서로 한 번 더 그리되
// 하반신·밑둥을 덮는 마스크 안에서만 보이게 한다. 울타리 칸의 꽃·새싹은 울타리 뒤로 가려지는 부분이라 덧그리지 않는다.
public class GrassInFront : MonoBehaviour
{
    public TilemapRenderer[] grassRenderers;  // 꽃·새싹 타일맵들
    public Tilemap fence;                     // 이 타일맵에 타일이 있는 칸은 덧그리지 않는다

    [Header("캐릭터·동물")]
    public int characterOverlayOrder = 4;     // 캐릭터(3) 바로 위
    [Range(0f, 1f)] public float lowerBodyRatio = 0.4f; // 그림 아래에서 이 비율까지가 하반신

    [Header("나무·해바라기")]
    public TilemapRenderer[] tallRenderers;   // 나무·해바라기 타일맵들
    public int tallOverlayOrder = 8;          // 나무·해바라기(7) 바로 위
    public float tallBaseHeight = 6f / 16f;   // 그림 맨 아래에서 이 높이까지가 밑둥 (타일 16픽셀 기준 6픽셀)

    private struct Character
    {
        public SpriteRenderer renderer;
        public Transform mask;
    }

    private readonly List<Character> characters = new List<Character>();
    private readonly Dictionary<Sprite, Rect> spriteOutlines = new Dictionary<Sprite, Rect>();
    private Sprite maskSprite;

    private void Start()
    {
        // 1x1 크기, 아래쪽 가운데가 기준점인 흰 사각형 (마스크 모양으로만 쓰인다)
        Texture2D white = Texture2D.whiteTexture;
        maskSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0f), white.width);

        foreach (TilemapRenderer grassRenderer in grassRenderers)
        {
            CreateOverlay(grassRenderer, characterOverlayOrder);
            CreateOverlay(grassRenderer, tallOverlayOrder);
        }

        AddCharacter(FindAnyObjectByType<Player>().GetComponent<SpriteRenderer>());
        foreach (Animal animal in FindObjectsByType<Animal>(FindObjectsSortMode.None))
        {
            AddCharacter(animal.GetComponent<SpriteRenderer>());
        }

        foreach (TilemapRenderer tallRenderer in tallRenderers)
        {
            AddTallTileMasks(tallRenderer);
        }
    }

    // 꽃·새싹 타일맵을 sortingOrder 순서로 한 번 더 그리는 타일맵을 만든다 (그 순서용 마스크 안에서만 보임)
    private void CreateOverlay(TilemapRenderer grassRenderer, int sortingOrder)
    {
        Tilemap grass = grassRenderer.GetComponent<Tilemap>();
        GameObject overlayObject = new GameObject(grass.name + " Overlay " + sortingOrder);
        overlayObject.layer = grass.gameObject.layer;
        overlayObject.transform.SetParent(grass.transform.parent, false);
        overlayObject.transform.localPosition = grass.transform.localPosition;
        overlayObject.transform.localRotation = grass.transform.localRotation;
        overlayObject.transform.localScale = grass.transform.localScale;

        Tilemap overlay = overlayObject.AddComponent<Tilemap>();
        overlay.tileAnchor = grass.tileAnchor;
        TilemapRenderer overlayRenderer = overlayObject.AddComponent<TilemapRenderer>();
        overlayRenderer.sharedMaterial = grassRenderer.sharedMaterial;
        overlayRenderer.sortingLayerID = grassRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = sortingOrder;
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

    // sortingOrder 순서의 덧그린 꽃·새싹만 드러내는 마스크 (마스크를 쓰는 다른 렌더러에는 영향을 주지 않는다)
    private Transform CreateMask(Transform parent, int sortingLayerID, int sortingOrder)
    {
        GameObject maskObject = new GameObject("GrassInFrontMask");
        maskObject.transform.SetParent(parent, false);
        SpriteMask mask = maskObject.AddComponent<SpriteMask>();
        mask.sprite = maskSprite;
        mask.isCustomRangeActive = true;
        mask.frontSortingLayerID = sortingLayerID;
        mask.frontSortingOrder = sortingOrder;
        mask.backSortingLayerID = sortingLayerID;
        mask.backSortingOrder = sortingOrder - 1;
        return maskObject.transform;
    }

    private void AddCharacter(SpriteRenderer characterRenderer)
    {
        Transform mask = CreateMask(characterRenderer.transform, characterRenderer.sortingLayerID, characterOverlayOrder);
        characters.Add(new Character { renderer = characterRenderer, mask = mask });
    }

    // 나무·해바라기 타일마다 그림 맨 아래 밑둥에 마스크를 둔다 (움직이지 않으니 한 번만)
    private void AddTallTileMasks(TilemapRenderer tallRenderer)
    {
        Tilemap tall = tallRenderer.GetComponent<Tilemap>();
        foreach (Vector3Int cell in tall.cellBounds.allPositionsWithin)
        {
            Sprite sprite = tall.GetSprite(cell);
            if (sprite == null)
                continue;

            Rect outline = OutlineOf(sprite);
            Vector3 offset = tall.GetTransformMatrix(cell).GetColumn(3);
            Transform mask = CreateMask(transform, tallRenderer.sortingLayerID, tallOverlayOrder);
            mask.position = tall.GetCellCenterWorld(cell) + offset + new Vector3(outline.center.x, outline.yMin, 0f);
            mask.localScale = new Vector3(outline.width, tallBaseHeight, 1f);
        }
    }

    // 애니메이션으로 그림이 바뀌어도 마스크가 지금 그림의 하반신을 덮도록 맞춘다
    private void LateUpdate()
    {
        foreach (Character character in characters)
        {
            Sprite sprite = character.renderer.sprite;
            bool visible = sprite != null && character.renderer.enabled && character.renderer.gameObject.activeInHierarchy;
            if (character.mask.gameObject.activeSelf != visible)
            {
                character.mask.gameObject.SetActive(visible);
            }
            if (!visible)
                continue;

            Rect outline = OutlineOf(sprite);
            float centerX = outline.center.x * (character.renderer.flipX ? -1f : 1f);
            character.mask.localPosition = new Vector3(centerX, outline.yMin, 0f);
            character.mask.localScale = new Vector3(outline.width, outline.height * lowerBodyRatio, 1f);
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
