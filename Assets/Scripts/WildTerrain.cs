using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 농장에서 멀어져도 같은 벌판만 이어지지 않게, 플레이어 둘레의 땅을 chunkSize 칸짜리 구역으로 나눠
// 구역마다 숲·바위밭·꽃밭·과수원·연못·빈터 중 하나를 깔아 둔다. 구역의 위치로 모양을 정하므로
// 같은 곳에 다시 가면 같은 풍경이 나오고, 멀리 갈수록 새로운 구역이 만들어진다.
// 농장 둘레(homeArea)는 원래 지형 그대로 두고, 나무·바위·물이 생기거나 사라지면 그 구역만 길찾기 그래프를 다시 읽는다.
public class WildTerrain : MonoBehaviour
{
    public Transform player;
    public int chunkSize = 16;
    public int viewChunks = 1;   // 플레이어가 있는 구역에서 이 칸 수만큼 둘레까지 만들어 둔다
    public int seed = 20260923;
    public RectInt homeArea = new RectInt(-22, -22, 44, 44); // 집·울타리·밭 둘레: 여기는 건드리지 않는다

    [Header("타일")]
    public TileBase[] trees;      // 키 큰 나무, 둥근 나무, 과일나무
    public TileBase[] mushrooms;
    public TileBase[] rocks;
    public TileBase[] stumps;     // 그루터기, 통나무
    public TileBase[] bushes;
    public TileBase[] flowers;    // 꽃, 새싹
    public TileBase sunflower;
    public TileBase[] baseGrass;  // 나무·해바라기 밑둥에 깔 작은 풀
    public Sprite[] pathSprites;  // Tilled_Dirt.png의 흙길 그림들 (Grass.png와 같은 순서)
    public TileBase pondWater;    // 물 (부딪히는 타일)
    public TileBase[] lilyPads;
    public Sprite[] shoreSprites; // Grass.png의 물가 그림들 (Grass_0부터 차례대로)
    public TilemapRenderer[] pathBlockers;  // 길을 깔면 안 되는 곳 (집·울타리·밭)
    public TilemapRenderer[] groundDecor; // 원래 깔려 있는 꽃·새싹 타일맵 (겹치지 않게 피하고, 물 자리에서는 치운다)

    [Header("그리는 순서")]
    public TilemapRenderer rendererTemplate; // 재질·정렬 레이어를 가져올 타일맵
    public int pathOrder = -9;               // 땅 위, 꽃 아래
    public int pondOrder = -9;               // 땅 위, 울타리 아래
    public int pondDecorOrder = -8;          // 물 위에 뜨는 수련잎
    public int flowerOrder = 3;              // 캐릭터와 같은 층 (y 위치로 앞뒤가 정해짐)
    public int objectOrder = 3;              // 나무·캐릭터와 같은 층 (y 위치로 앞뒤가 정해짐)
    public int baseGrassOrder = 3;           // 나무 밑둥에 깔리는 풀 (같은 층, 밑둥보다 아래라 앞에 보인다)
    public float baseOffset = 0.2f;

    // 물가 그림 고르기: [왼위, 위, 오른위, 왼, 오른, 왼아래, 아래, 오른아래]에 물이 보이면 w, 풀이면 G
    private static readonly Dictionary<string, int> ShoreLookup = new Dictionary<string, int>
    {
        { "wwwwGwGG", 0 }, { "wwwGGGGG", 1 }, { "wwwGwGGw", 2 }, { "wwwwwwGw", 3 }, { "wwwwGwGw", 4 },
        { "wwwGGGGw", 5 }, { "wwwGGwGG", 6 }, { "wwwGwwGw", 7 }, { "wwwGGwGw", 8 }, { "wGGGGGGw", 9 },
        { "wGGwGwGG", 10 }, { "GGGGGGGG", 11 }, { "GGwGwGGw", 12 }, { "wGwwwwGw", 13 }, { "wGGwGwGw", 14 },
        { "GGGGGGGw", 15 }, { "GGGGGwGG", 16 }, { "GGwGwwGw", 17 }, { "GGGGGwGw", 18 }, { "GGwGGwGG", 19 },
        { "wGGwGwww", 20 }, { "GGGGGwww", 21 }, { "GGwGwwww", 22 }, { "wGwwwwww", 23 }, { "wGwwGwGG", 24 },
        { "GGwGGGGG", 25 }, { "wGGGGGGG", 26 }, { "wGwGwGGw", 27 }, { "wGwGGGGG", 28 }, { "wGwGGwGG", 29 },
        { "wGwGGGGw", 30 }, { "wwwwGwww", 31 }, { "wwwGGwww", 32 }, { "wwwGwwww", 33 }, { "wwwwwwww", 34 },
        { "wGwwGwww", 35 }, { "GGwGGwww", 36 }, { "wGGGGwww", 37 }, { "wGwGwwww", 38 }, { "wGwGGwww", 39 },
        { "wGGGGwGw", 40 }, { "GGwGGwGw", 41 }, { "wGwwGwGw", 46 }, { "GGwGGGGw", 47 }, { "wGGGGwGG", 48 },
        { "wGwGwwGw", 49 }, { "wGwGGwGw", 50 },
    };

    [Header("길")]
    public int pathSpacing = 24;    // 길 사이 간격
    public float pathWidth = 1.8f;  // 길 반폭 (네 칸쯤)
    public float pathWander = 2.5f; // 길이 휘어지는 폭
    public float pathCurve = 0.055f; // 길이 휘어지는 빠르기
    public Vector2Int homePathFrom = new Vector2Int(-5, -4); // 집 앞에서 시작해 남쪽 큰길까지 이어지는 진입로
    public float homePathWidth = 1.2f;

    private enum Theme { Woods, Rocky, Meadow, Orchard, Pond, Clearing }

    // 구역마다 뽑는 풍경. 연못과 숲·꽃밭을 자주 만나도록 여러 번 넣어 뒀다.
    private static readonly Theme[] ThemeBag =
    {
        Theme.Woods, Theme.Woods, Theme.Woods,
        Theme.Rocky, Theme.Rocky,
        Theme.Meadow, Theme.Meadow, Theme.Meadow,
        Theme.Orchard, Theme.Orchard,
        Theme.Pond, Theme.Pond, Theme.Pond,
        Theme.Clearing,
    };

    // 물을 판 자리에 원래 있던 꽃·새싹. 물 위에 뜬 것처럼 보이므로 치워 뒀다가, 구역을 지울 때 되돌려 놓는다.
    private struct RemovedTile
    {
        public Tilemap map;
        public Vector3Int cell;
        public TileBase tile;
        public Matrix4x4 transform;
    }

    private Tilemap pathMap;
    private Tilemap pondMap;
    private Tilemap pondDecorMap;
    private Tilemap flowerMap;
    private Tilemap objectMap;
    private Tilemap baseGrassMap;
    private Tilemap[] groundDecorMaps;
    private Vector3[] decorPositions;
    private readonly List<RemovedTile> removedDecor = new List<RemovedTile>();
    private readonly Dictionary<Vector2Int, List<Vector2Int>> hiddenByChunk = new Dictionary<Vector2Int, List<Vector2Int>>();
    private readonly HashSet<Vector2Int> hiddenCells = new HashSet<Vector2Int>();
    private readonly Dictionary<int, Tile> shoreTiles = new Dictionary<int, Tile>();
    private readonly Dictionary<int, Tile> pathTiles = new Dictionary<int, Tile>();
    private readonly HashSet<Vector2Int> builtChunks = new HashSet<Vector2Int>();
    private readonly Queue<Vector2Int> toBuild = new Queue<Vector2Int>();
    private readonly Queue<Vector2Int> toClear = new Queue<Vector2Int>();
    private Vector2Int center = new Vector2Int(int.MinValue, int.MinValue);
    private static WildTerrain current;

    private void Awake()
    {
        current = this;
    }

    private void Start()
    {
        if (player == null)
        {
            player = GameManager.instance.player.transform;
        }
        pathMap = CreateTilemap("Wild Path", pathOrder, TilemapRenderer.Mode.Chunk, false);
        pondMap = CreateTilemap("Wild Pond", pondOrder, TilemapRenderer.Mode.Chunk, true);
        pondDecorMap = CreateTilemap("Wild Pond Decor", pondDecorOrder, TilemapRenderer.Mode.Chunk, false);
        flowerMap = CreateTilemap("Wild Flowers", flowerOrder, TilemapRenderer.Mode.Individual, false);
        objectMap = CreateTilemap("Wild Objects", objectOrder, TilemapRenderer.Mode.Individual, true);
        baseGrassMap = CreateTilemap("Wild Base Grass", baseGrassOrder, TilemapRenderer.Mode.Individual, false);

        groundDecorMaps = new Tilemap[groundDecor.Length];
        decorPositions = new Vector3[groundDecor.Length];
        for (int i = 0; i < groundDecor.Length; i++)
        {
            groundDecorMaps[i] = groundDecor[i].GetComponent<Tilemap>();
            decorPositions[i] = groundDecorMaps[i].transform.position;
        }
    }

    private void Update()
    {
        // 꽃·새싹 타일맵은 무한 맵을 따라 옮겨 다니므로, 움직였으면 연못 자리를 다시 비운다
        if (DecorMoved() && hiddenCells.Count > 0)
        {
            RestoreDecor();
            HideDecor();
        }

        // 한 프레임에 구역 하나씩만 만들거나 지워서 끊기지 않게 한다
        if (toClear.Count > 0)
        {
            Clear(toClear.Dequeue());
            return;
        }
        if (toBuild.Count > 0)
        {
            Build(toBuild.Dequeue());
            return;
        }

        Vector2Int playerChunk = ChunkOf(player.position);
        if (playerChunk == center)
            return;

        center = playerChunk;
        // 멀어진 구역은 지우고, 가까워진 구역은 새로 만든다
        var keep = new HashSet<Vector2Int>();
        for (int x = -viewChunks; x <= viewChunks; x++)
        {
            for (int y = -viewChunks; y <= viewChunks; y++)
            {
                keep.Add(new Vector2Int(playerChunk.x + x, playerChunk.y + y));
            }
        }

        var gone = new List<Vector2Int>();
        foreach (Vector2Int chunk in builtChunks)
        {
            if (!keep.Contains(chunk))
                gone.Add(chunk);
        }
        foreach (Vector2Int chunk in gone)
        {
            toClear.Enqueue(chunk);
            builtChunks.Remove(chunk);
        }
        foreach (Vector2Int chunk in keep)
        {
            if (builtChunks.Add(chunk))
            {
                toBuild.Enqueue(chunk);
            }
        }
    }

    private Vector2Int ChunkOf(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.y / chunkSize));
    }

    private Tilemap CreateTilemap(string name, int sortingOrder, TilemapRenderer.Mode mode, bool solid)
    {
        var tilemapObject = new GameObject(name);
        tilemapObject.layer = solid ? LayerMask.NameToLayer("Object") : gameObject.layer;
        tilemapObject.transform.SetParent(transform, false);
        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        TilemapRenderer tilemapRenderer = tilemapObject.AddComponent<TilemapRenderer>();
        tilemapRenderer.sharedMaterial = rendererTemplate.sharedMaterial;
        tilemapRenderer.sortingLayerID = rendererTemplate.sortingLayerID;
        tilemapRenderer.sortingOrder = sortingOrder;
        tilemapRenderer.mode = mode;
        if (solid)
        {
            tilemapObject.AddComponent<TilemapCollider2D>();
        }
        return tilemap;
    }

    private void Clear(Vector2Int chunk)
    {
        foreach (Vector3Int cell in CellsOf(chunk))
        {
            pathMap.SetTile(cell, null);
            pondMap.SetTile(cell, null);
            pondDecorMap.SetTile(cell, null);
            flowerMap.SetTile(cell, null);
            objectMap.SetTile(cell, null);
            baseGrassMap.SetTile(cell, null);
        }
        if (hiddenByChunk.TryGetValue(chunk, out List<Vector2Int> cells))
        {
            RestoreDecor();
            foreach (Vector2Int cell in cells)
            {
                hiddenCells.Remove(cell);
            }
            hiddenByChunk.Remove(chunk);
            HideDecor();
        }
        RescanPaths(chunk);
    }

    private void Build(Vector2Int chunk)
    {
        var random = new System.Random(seed ^ (chunk.x * 73856093) ^ (chunk.y * 19349663));
        Theme theme = ThemeBag[random.Next(ThemeBag.Length)];
        var taken = new HashSet<Vector3Int>();
        var water = new HashSet<Vector2Int>();
        var path = new HashSet<Vector2Int>();

        // 서로 spacing칸 안에는 겹쳐 두지 않고, 물가에는 두지 않는다
        void Put(Tilemap tilemap, Vector3Int cell, TileBase tile, int spacing)
        {
            for (int x = -spacing; x <= spacing; x++)
            {
                for (int y = -spacing; y <= spacing; y++)
                {
                    if (taken.Contains(new Vector3Int(cell.x + x, cell.y + y, 0)))
                        return;
                }
            }
            if (NearWater(water, cell))
                return;
            // 원래 깔려 있던 꽃·새싹 위에 또 놓으면 겹쳐 보인다
            if (tilemap == flowerMap && GroundDecorAt(cell) != null)
                return;

            tilemap.SetTile(cell, tile);
            taken.Add(cell);
            // 나무·해바라기는 밑둥 자리에 작은 풀을 깔아, 밑둥 쪽만 풀이 앞에 보이게 한다
            if (tilemap == objectMap && baseGrass.Length > 0 && random.NextDouble() < 0.5)
            {
                BaseGrass.PlaceAtBase(baseGrassMap, objectMap, cell, baseGrass[random.Next(baseGrass.Length)], baseOffset);
            }
        }

        TileBase Pick(TileBase[] tiles) => tiles[random.Next(tiles.Length)];

        if (theme == Theme.Pond)
        {
            DigPond(chunk, random, water);
        }
        DrawPath(chunk, water, path);

        // 물과 길 위에 놓여 있던 꽃·새싹은 치워 둔다 (물에 뜨거나 길을 덮어 보이지 않게)
        var hidden = new List<Vector2Int>(water);
        hidden.AddRange(path);
        if (hidden.Count > 0)
        {
            hiddenByChunk[chunk] = hidden;
            hiddenCells.UnionWith(hidden);
            HideDecor();
        }

        foreach (Vector3Int cell in CellsOf(chunk))
        {
            var spot = new Vector2Int(cell.x, cell.y);
            if (homeArea.Contains(spot) || water.Contains(spot) || path.Contains(spot))
                continue;

            double roll = random.NextDouble();
            switch (theme)
            {
                case Theme.Woods:
                    // 나무가 많은 숲: 가로지르는 빈 줄을 남겨 지나갈 수 있게 한다
                    if ((cell.x + cell.y) % 7 == 0 || (cell.x - cell.y) % 9 == 0)
                        break;
                    if (roll < 0.18)
                        Put(objectMap, cell, trees[0], 1);
                    else if (roll < 0.24)
                        Put(objectMap, cell, Pick(trees), 2);
                    else if (roll < 0.28)
                        Put(flowerMap, cell, Pick(mushrooms), 1);
                    else if (roll < 0.3)
                        Put(objectMap, cell, Pick(stumps), 1);
                    break;

                case Theme.Rocky:
                    if (roll < 0.08)
                        Put(objectMap, cell, Pick(rocks), 1);
                    else if (roll < 0.11)
                        Put(objectMap, cell, Pick(stumps), 1);
                    else if (roll < 0.14)
                        Put(objectMap, cell, Pick(bushes), 2);
                    break;

                case Theme.Meadow:
                    if (roll < 0.2)
                        Put(flowerMap, cell, Pick(flowers), 1);
                    else if (roll < 0.215)
                        Put(objectMap, cell, sunflower, 2);
                    break;

                case Theme.Orchard:
                    // 과일나무를 네 칸 간격으로 줄 맞춰 심는다
                    if (cell.x % 4 == 0 && cell.y % 4 == 0)
                        Put(objectMap, cell, trees[trees.Length - 1], 3);
                    else if (roll < 0.1)
                        Put(flowerMap, cell, Pick(flowers), 1);
                    break;

                case Theme.Pond:
                    if (roll < 0.12)
                        Put(flowerMap, cell, Pick(flowers), 1);
                    else if (roll < 0.15)
                        Put(objectMap, cell, Pick(bushes), 2);
                    else if (roll < 0.17)
                        Put(objectMap, cell, trees[0], 2);
                    break;

                case Theme.Clearing:
                    if (roll < 0.09)
                        Put(flowerMap, cell, Pick(flowers), 1);
                    else if (roll < 0.11)
                        Put(objectMap, cell, Pick(bushes), 2);
                    break;
            }
        }
        RescanPaths(chunk);
    }

    // 구불구불한 흙길을 깐다. 길 자리는 세계 좌표만으로 정해지므로 구역이 바뀌어도 끊기지 않고 이어진다.
    private void DrawPath(Vector2Int chunk, HashSet<Vector2Int> water, HashSet<Vector2Int> path)
    {
        if (pathSprites == null || pathSprites.Length == 0)
            return;

        foreach (Vector3Int cell in CellsOf(chunk))
        {
            if (PathCell(cell.x, cell.y, water))
            {
                path.Add(new Vector2Int(cell.x, cell.y));
            }
        }

        // 가장자리가 둥글게 보이도록, 옆 칸도 길인지에 따라 흙 그림을 고른다
        foreach (Vector2Int cell in path)
        {
            bool Dirt(int dx, int dy) => PathCell(cell.x + dx, cell.y + dy, water);

            if (BlobLookup(Dirt, out int sprite) && sprite < pathSprites.Length)
            {
                pathMap.SetTile(new Vector3Int(cell.x, cell.y, 0), BlobTile(pathTiles, pathSprites, sprite));
            }
        }
    }

    // 집·울타리·밭 위에는 길을 깔지 않는다
    private bool Blocked(Vector3Int cell)
    {
        foreach (TilemapRenderer blocker in pathBlockers)
        {
            Tilemap map = blocker.GetComponent<Tilemap>();
            if (map.GetTile(map.WorldToCell(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f))) != null)
                return true;
        }
        return false;
    }

    // 여덟 이웃이 같은 바닥인지 보고 47조각 타일셋에서 맞는 그림을 고른다
    private static bool BlobLookup(System.Func<int, int, bool> same, out int sprite)
    {
        bool top = same(0, 1);
        bool bottom = same(0, -1);
        bool left = same(-1, 0);
        bool right = same(1, 0);
        char Side(bool isSame) => isSame ? 'G' : 'w';
        string look = new string(new[]
        {
            Side(top && left && same(-1, 1)),
            Side(top),
            Side(top && right && same(1, 1)),
            Side(left),
            Side(right),
            Side(bottom && left && same(-1, -1)),
            Side(bottom),
            Side(bottom && right && same(1, -1)),
        });
        return ShoreLookup.TryGetValue(look, out sprite);
    }

    private static Tile BlobTile(Dictionary<int, Tile> cache, Sprite[] sprites, int sprite)
    {
        if (cache.TryGetValue(sprite, out Tile tile))
            return tile;

        tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprites[sprite];
        tile.flags = TileFlags.LockAll;
        tile.colliderType = Tile.ColliderType.None;
        cache[sprite] = tile;
        return tile;
    }

    // 비스듬히 꺾이는 곳에서 폭이 좁아지면 길이 끊겨 보이므로, 양옆이 길이면 가운데도 길로 친다
    private bool OnPath(int x, int y)
    {
        return OnHomePath(x, y)
            || OnPathBand(x, y)
            || (OnPathBand(x - 1, y) && OnPathBand(x + 1, y))
            || (OnPathBand(x, y - 1) && OnPathBand(x, y + 1));
    }

    // pathSpacing칸마다 가로·세로로 길이 지나가고, 사인 곡선만큼 완만하게 휘어진다
    private bool OnPathBand(int x, int y)
    {
        int row = Mathf.RoundToInt((float)y / pathSpacing);
        for (int k = row - 1; k <= row + 1; k++)
        {
            float center = k * pathSpacing + pathWander * Mathf.Sin(x * pathCurve + k * 2.3f);
            if (Mathf.Abs(y - center) <= pathWidth)
                return true;
        }

        int column = Mathf.RoundToInt((float)x / pathSpacing);
        for (int k = column - 1; k <= column + 1; k++)
        {
            float center = k * pathSpacing + pathWander * Mathf.Sin(y * pathCurve + k * 1.7f);
            if (Mathf.Abs(x - center) <= pathWidth)
                return true;
        }
        return false;
    }

    // 이 칸에 길을 까는지 (구역 밖 이웃도 같은 기준으로 볼 수 있게 순수 함수로 둔다)
    private bool PathCell(int x, int y, HashSet<Vector2Int> water)
    {
        var here = new Vector2Int(x, y);
        if (water.Contains(here) || !OnPath(x, y))
            return false;
        if (!homeArea.Contains(here))
            return true;

        return OnHomePath(x, y) && !Blocked(new Vector3Int(x, y, 0));
    }

    // 집 앞에서 남쪽 큰길까지 이어지는 진입로
    private bool OnHomePath(int x, int y)
    {
        return Mathf.Abs(x - homePathFrom.x) <= homePathWidth
            && y <= homePathFrom.y
            && y >= -(pathSpacing + 6);
    }

    // 길 위에 서 있는지 (길 위에서는 빨리 걷고 체력이 천천히 닳는다)
    public static bool OnPathAt(Vector3 position)
    {
        if (current == null || current.pathMap == null)
            return false;

        return current.pathMap.GetTile(current.pathMap.WorldToCell(position)) != null;
    }

    // 구역 안쪽에 둥근 물웅덩이를 파고, 물에 닿는 땅에는 물가 그림을 깐다
    private void DigPond(Vector2Int chunk, System.Random random, HashSet<Vector2Int> water)
    {
        const int margin = 5;
        // 농장 둘레에는 연못을 파지 않는다
        if (homeArea.Overlaps(new RectInt(chunk.x * chunkSize, chunk.y * chunkSize, chunkSize, chunkSize)))
            return;

        var pondCenter = new Vector2(
            chunk.x * chunkSize + margin + (float)random.NextDouble() * (chunkSize - margin * 2),
            chunk.y * chunkSize + margin + (float)random.NextDouble() * (chunkSize - margin * 2));

        var circles = new List<Vector3>();
        int count = 2 + random.Next(2);
        for (int i = 0; i < count; i++)
        {
            circles.Add(new Vector3(
                pondCenter.x + (float)(random.NextDouble() - 0.5) * 3f,
                pondCenter.y + (float)(random.NextDouble() - 0.5) * 3f,
                1.8f + (float)random.NextDouble() * 1.4f));
        }

        foreach (Vector3Int cell in CellsOf(chunk))
        {
            var here = new Vector2Int(cell.x, cell.y);
            var point = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
            foreach (Vector3 circle in circles)
            {
                if (Vector2.Distance(point, new Vector2(circle.x, circle.y)) < circle.z)
                {
                    water.Add(here);
                    break;
                }
            }
        }

        foreach (Vector2Int cell in water)
        {
            pondMap.SetTile(new Vector3Int(cell.x, cell.y, 0), pondWater);
            // 사방이 물인 곳에는 수련잎을 띄운다
            if (lilyPads.Length > 0 && random.NextDouble() < 0.18 && Neighbors4(cell).TrueForAll(water.Contains))
            {
                pondDecorMap.SetTile(new Vector3Int(cell.x, cell.y, 0), lilyPads[random.Next(lilyPads.Length)]);
            }
        }

        // 물에 닿는 땅: 어느 쪽에 물이 있는지로 물가 그림을 고른다
        var shore = new HashSet<Vector2Int>();
        foreach (Vector2Int cell in water)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var neighbour = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (!water.Contains(neighbour))
                        shore.Add(neighbour);
                }
            }
        }

        foreach (Vector2Int cell in shore)
        {
            bool top = water.Contains(cell + Vector2Int.up);
            bool bottom = water.Contains(cell + Vector2Int.down);
            bool left = water.Contains(cell + Vector2Int.left);
            bool right = water.Contains(cell + Vector2Int.right);
            char Side(bool isWater) => isWater ? 'w' : 'G';
            string look = new string(new[]
            {
                Side(top || left || water.Contains(cell + new Vector2Int(-1, 1))),
                Side(top),
                Side(top || right || water.Contains(cell + new Vector2Int(1, 1))),
                Side(left),
                Side(right),
                Side(bottom || left || water.Contains(cell + new Vector2Int(-1, -1))),
                Side(bottom),
                Side(bottom || right || water.Contains(cell + new Vector2Int(1, -1))),
            });
            if (ShoreLookup.TryGetValue(look, out int sprite))
            {
                pondMap.SetTile(new Vector3Int(cell.x, cell.y, 0), ShoreTile(sprite));
            }
        }
    }

    private Tile ShoreTile(int sprite)
    {
        if (shoreTiles.TryGetValue(sprite, out Tile tile))
            return tile;

        tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = shoreSprites[sprite];
        tile.flags = TileFlags.LockAll;
        tile.colliderType = Tile.ColliderType.None; // 물가는 지나다닐 수 있고, 막히는 건 물뿐이다
        shoreTiles[sprite] = tile;
        return tile;
    }

    // 이 자리에 원래 깔려 있는 꽃·새싹 타일맵 (없으면 null)
    private Tilemap GroundDecorAt(Vector3Int cell)
    {
        foreach (Tilemap decor in groundDecorMaps)
        {
            foreach (Vector3Int decorCell in DecorCells(decor, cell))
            {
                if (decor.GetTile(decorCell) != null)
                    return decor;
            }
        }
        return null;
    }

    // 꽃·새싹 타일맵은 무한 맵을 따라 움직이므로, 세계 좌표로 칸을 찾는다
    private static IEnumerable<Vector3Int> DecorCells(Tilemap decor, Vector3Int cell)
    {
        yield return decor.WorldToCell(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f));
    }

    // 물이 들어찬 자리의 꽃·새싹을 치운다 (물 위에 꽃이 떠 있는 것처럼 보이지 않게)
    private void HideDecor()
    {
        foreach (Vector2Int cell in hiddenCells)
        {
            var here = new Vector3Int(cell.x, cell.y, 0);
            foreach (Tilemap decor in groundDecorMaps)
            {
                foreach (Vector3Int decorCell in DecorCells(decor, here))
                {
                    TileBase tile = decor.GetTile(decorCell);
                    if (tile == null)
                        continue;

                    removedDecor.Add(new RemovedTile { map = decor, cell = decorCell, tile = tile, transform = decor.GetTransformMatrix(decorCell) });
                    decor.SetTile(decorCell, null);
                }
            }
        }
    }

    // 치워 둔 꽃·새싹을 원래 칸에 돌려놓는다
    private void RestoreDecor()
    {
        foreach (RemovedTile item in removedDecor)
        {
            item.map.SetTile(item.cell, item.tile);
            item.map.SetTransformMatrix(item.cell, item.transform);
        }
        removedDecor.Clear();
    }

    // 꽃·새싹 타일맵이 무한 맵을 따라 옮겨졌는지 보고, 지금 자리를 적어 둔다
    private bool DecorMoved()
    {
        bool moved = false;
        for (int i = 0; i < groundDecorMaps.Length; i++)
        {
            Vector3 position = groundDecorMaps[i].transform.position;
            if (position != decorPositions[i])
            {
                decorPositions[i] = position;
                moved = true;
            }
        }
        return moved;
    }

    private static bool NearWater(HashSet<Vector2Int> water, Vector3Int cell)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (water.Contains(new Vector2Int(cell.x + dx, cell.y + dy)))
                    return true;
            }
        }
        return false;
    }

    private static List<Vector2Int> Neighbors4(Vector2Int cell)
    {
        return new List<Vector2Int> { cell + Vector2Int.up, cell + Vector2Int.down, cell + Vector2Int.left, cell + Vector2Int.right };
    }

    // 이 구역에 나무·바위·물이 생기거나 사라졌으니 길찾기 그래프에서 이 부분만 다시 읽는다
    private void RescanPaths(Vector2Int chunk)
    {
        if (AstarPath.active == null)
            return;

        var center3 = new Vector3((chunk.x + 0.5f) * chunkSize, (chunk.y + 0.5f) * chunkSize, 0f);
        AstarPath.active.UpdateGraphs(new Bounds(center3, new Vector3(chunkSize + 2, chunkSize + 2, 10f)));
    }

    private IEnumerable<Vector3Int> CellsOf(Vector2Int chunk)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                yield return new Vector3Int(chunk.x * chunkSize + x, chunk.y * chunkSize + y, 0);
            }
        }
    }
}
