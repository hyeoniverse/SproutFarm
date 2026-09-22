using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 침대·집·울타리 근처에서 무엇을 할 수 있는지 화면 아래에 알려주는 안내 문구.
// Show()를 부른 프레임에만 보이므로, 조건이 맞는 동안 매 프레임 부르면 된다.
// 한 프레임에 여러 곳에서 부르면 priority가 높은 문구가 보인다.
public class InteractionHint : MonoBehaviour
{
    public Sprite backgroundSprite; // 대화창 글상자와 같은 크림색 상자 (9분할)
    public Color textColor = new Color(0.392f, 0.25f, 0.25f, 1f); // HUD 글자와 같은 갈색
    public float fontSize = 48f;

    private static InteractionHint instance;

    private GameObject panel;
    private TMP_Text hintText;
    private string requestedMessage;
    private int requestedPriority;
    private string flashMessage;
    private float flashUntil;

    public static void Show(string message, int priority = 0)
    {
        if (instance == null)
            return;

        if (instance.requestedMessage == null || priority >= instance.requestedPriority)
        {
            instance.requestedMessage = message;
            instance.requestedPriority = priority;
        }
    }

    // 잠깐(seconds 동안) 보여주는 알림. 그동안은 다른 안내보다 먼저 보인다.
    public static void Flash(string message, float seconds)
    {
        if (instance == null)
            return;

        instance.flashMessage = message;
        instance.flashUntil = Time.time + seconds;
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // 대화창 글꼴을 쓰므로 GameManager가 준비된 뒤에 만든다
        BuildUI();
    }

    private void LateUpdate()
    {
        if (panel == null)
            return;

        // 대화창이 떠 있을 때는 겹치지 않도록 숨긴다
        string message = Time.time < flashUntil ? flashMessage : requestedMessage;
        bool visible = message != null && !GameManager.instance.dialoguePanel.activeSelf;
        panel.SetActive(visible);
        if (visible)
        {
            hintText.text = message;
        }
        requestedMessage = null;
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        // 다른 UI와 같은 기준 해상도로 맞춰 화면 크기에 따라 함께 커지고 작아지게 한다
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2778f, 1284f);
        scaler.matchWidthOrHeight = 0.5f;

        // HUD 상자들과 같은 비율(스프라이트 1px = 6.25)로 9분할해 높이 300, 너비는 글자에 맞춘다
        panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 160f); // 오른쪽 아래 동물 수 표시보다 위
        panelRect.sizeDelta = new Vector2(0f, 300f);
        Image background = panel.GetComponent<Image>();
        background.sprite = backgroundSprite;
        background.type = Image.Type.Sliced;
        background.raycastTarget = false;
        HorizontalLayoutGroup layout = panel.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(110, 90, 75, 69); // 상자 테두리(10/6/12/11px) 안쪽에 글자가 오도록
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        hintText = textObject.GetComponent<TextMeshProUGUI>();
        hintText.font = GameManager.instance.dialogueText.font;
        hintText.fontSize = fontSize;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.textWrappingMode = TextWrappingModes.NoWrap;
        hintText.color = textColor;
        hintText.raycastTarget = false;
        panel.SetActive(false);
    }
}
