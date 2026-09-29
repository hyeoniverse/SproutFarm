using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 터치 기기에서 화면 왼쪽에 이동 조이스틱, 오른쪽에 확인(Space 바)·달리기(Shift 키) 버튼을 띄운다.
// 씬에 따로 배치하지 않도록 GameManager가 붙이면 캔버스와 그림을 스스로 만들고, 누른 값은 MobileInput에 넘긴다.
[DefaultExecutionOrder(-100)] // 확인 버튼 누름을 다른 스크립트의 Update보다 먼저 이번 프레임 입력으로 옮겨 둔다
public class MobileControls : MonoBehaviour
{
    public float stickRadius = 80f;   // 손잡이가 움직일 수 있는 거리 (캔버스 단위, 1280x720 기준)
    public float stickDeadZone = 0.3f; // 이만큼(반지름 비율)도 안 밀면 멈춰 있는 것으로 본다
    public float margin = 40f;        // 화면 가장자리에서 떨어진 거리
    public float interactSize = 150f;
    public float runSize = 120f;
    public float opacity = 0.8f;

    // 대화창과 같은 갈색 테두리·크림색 바탕
    private static readonly Color OutlineColor = new Color32(0x64, 0x3F, 0x40, 0xFF);
    private static readonly Color FillColor = new Color32(0xF3, 0xE3, 0xC3, 0xFF);
    private static readonly Color PressedTint = new Color(0.8f, 0.8f, 0.8f, 1f);
    private const int NoPointer = int.MinValue;

    private RectTransform canvasRect;
    private RectTransform stickBase;
    private RectTransform knob;
    private int stickPointer = NoPointer;
    private bool interactQueued;

    private void Start()
    {
        // 이 씬에는 EventSystem이 없어서 버튼을 누르려면 만들어야 한다 (DialogueSkipButton과 같은 방식)
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject canvasObject = new GameObject("MobileControls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // 대화창보다 위, 잠잘 때 화면을 덮는 검은 막(100)보다 아래
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 1f; // 가로가 긴 휴대폰에서도 손가락 크기에 맞게 높이 기준으로 맞춘다
        canvasRect = (RectTransform)canvasObject.transform;

        Sprite ring = CreateCircleSprite(false);
        Sprite disc = CreateCircleSprite(true);

        BuildStick(ring, disc);
        BuildButton("확인", disc, new Vector2(-margin, margin), interactSize, 40f,
            pressed => { if (pressed) interactQueued = true; });
        BuildButton("달리기", disc, new Vector2(-margin - interactSize - 20f, margin), runSize, 30f,
            pressed => MobileInput.RunButton = pressed);
    }

    private void Update()
    {
        MobileInput.InteractButton = interactQueued;
        interactQueued = false;
    }

    private void OnDestroy()
    {
        MobileInput.Clear();
    }

    // 왼쪽 아래 절반을 누르면 그 자리에 조이스틱이 나타나고, 떼면 원래 자리로 돌아간다
    private void BuildStick(Sprite ring, Sprite disc)
    {
        RectTransform zone = CreateRect("StickZone", canvasRect);
        zone.anchorMin = Vector2.zero;
        zone.anchorMax = new Vector2(0.45f, 0.7f);
        zone.offsetMin = Vector2.zero;
        zone.offsetMax = Vector2.zero;
        Image zoneImage = zone.gameObject.AddComponent<Image>();
        zoneImage.color = Color.clear; // 보이지 않지만 누르기는 받는다

        stickBase = CreateImage("StickBase", zone, ring, stickRadius * 2f + 40f).rectTransform;
        knob = CreateImage("Knob", stickBase, disc, stickRadius).rectTransform;
        knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f); // 손잡이는 받침 가운데 기준으로 움직인다
        ResetStick();

        PointerRelay relay = zone.gameObject.AddComponent<PointerRelay>();
        relay.down = eventData =>
        {
            if (stickPointer != NoPointer)
                return;
            stickPointer = eventData.pointerId;
            stickBase.anchoredPosition = ToLocal(zone, eventData.position);
            MoveKnob(zone, eventData.position);
        };
        relay.drag = eventData =>
        {
            if (eventData.pointerId == stickPointer)
            {
                MoveKnob(zone, eventData.position);
            }
        };
        relay.up = eventData =>
        {
            if (eventData.pointerId == stickPointer)
            {
                ResetStick();
            }
        };
    }

    private void MoveKnob(RectTransform zone, Vector2 screenPosition)
    {
        Vector2 offset = Vector2.ClampMagnitude(ToLocal(zone, screenPosition) - stickBase.anchoredPosition, stickRadius);
        knob.anchoredPosition = offset;
        MobileInput.Move = SnapToEightWays(offset / stickRadius);
    }

    private void ResetStick()
    {
        stickPointer = NoPointer;
        stickBase.anchoredPosition = Vector2.one * (margin + stickRadius + 20f);
        knob.anchoredPosition = Vector2.zero;
        MobileInput.Move = Vector2.zero;
    }

    // 방향키를 누른 것과 같은 값으로 맞춘다 (상하좌우는 ±1, 대각선은 길이 1).
    // Player는 방향이 정확히 ±1일 때 바라보는 쪽을 정하므로 아날로그 값을 그대로 넘기면 안 된다.
    private Vector2 SnapToEightWays(Vector2 stick)
    {
        if (stick.magnitude < stickDeadZone)
            return Vector2.zero;

        float step = Mathf.PI / 4f;
        float angle = Mathf.Round(Mathf.Atan2(stick.y, stick.x) / step) * step;
        return new Vector2(Mathf.Round(Mathf.Cos(angle)), Mathf.Round(Mathf.Sin(angle))).normalized;
    }

    // 오른쪽 아래 버튼. 누르면 onPress(true), 떼면 onPress(false)
    private void BuildButton(string label, Sprite disc, Vector2 position, float size, float fontSize, System.Action<bool> onPress)
    {
        Image image = CreateImage(label, canvasRect, disc, size);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = position;

        TextMeshProUGUI text = CreateRect("Label", rect).gameObject.AddComponent<TextMeshProUGUI>();
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        TMP_Text dialogueText = GameManager.instance.dialogueText;
        text.font = dialogueText.font;
        text.fontSharedMaterial = dialogueText.fontSharedMaterial;
        text.fontSize = fontSize;
        text.color = OutlineColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = label;

        Color normal = image.color;
        PointerRelay relay = image.gameObject.AddComponent<PointerRelay>();
        relay.down = eventData =>
        {
            image.color = normal * PressedTint;
            onPress(true);
        };
        relay.up = eventData =>
        {
            image.color = normal;
            onPress(false);
        };
    }

    private Image CreateImage(string objectName, RectTransform parent, Sprite sprite, float size)
    {
        RectTransform rect = CreateRect(objectName, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.sizeDelta = Vector2.one * size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = new Color(1f, 1f, 1f, opacity);
        image.raycastTarget = parent == canvasRect; // 조이스틱 그림은 누르기를 뒤의 StickZone에 넘긴다
        return image;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    // 화면 좌표를 rect의 왼쪽 아래 기준 좌표로 바꾼다 (자식들이 왼쪽 아래에 앵커되어 있으므로)
    private static Vector2 ToLocal(RectTransform rect, Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, null, out Vector2 local);
        return local - rect.rect.min;
    }

    // 게임 그림과 어울리도록 도트로 그린 원. filled면 크림색으로 채우고, 아니면 테두리만 그린 반투명 고리
    private static Sprite CreateCircleSprite(bool filled)
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) / 2f;
        float outer = size / 2f;
        Color inside = filled ? FillColor : new Color(FillColor.r, FillColor.g, FillColor.b, 0.35f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                Color color = Color.clear;
                if (distance < outer - 2f)
                {
                    color = inside;
                }
                else if (distance < outer)
                {
                    color = OutlineColor;
                }
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
