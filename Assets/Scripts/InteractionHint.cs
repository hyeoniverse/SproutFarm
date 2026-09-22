using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 침대·집·울타리 근처에서 무엇을 할 수 있는지 화면 아래에 알려주는 안내 문구.
// Show()를 부른 프레임에만 보이므로, 조건이 맞는 동안 매 프레임 부르면 된다.
// 한 프레임에 여러 곳에서 부르면 priority가 높은 문구가 보인다.
public class InteractionHint : MonoBehaviour
{
    private static InteractionHint instance;

    private GameObject panel;
    private TMP_Text hintText;
    private string requestedMessage;
    private int requestedPriority;

    public static void Show(string message, int priority = 0)
    {
        if (instance == null)
        {
            instance = Create();
        }
        if (instance.requestedMessage == null || priority >= instance.requestedPriority)
        {
            instance.requestedMessage = message;
            instance.requestedPriority = priority;
        }
    }

    private void LateUpdate()
    {
        // 대화창이 떠 있을 때는 겹치지 않도록 숨긴다
        bool visible = requestedMessage != null && !GameManager.instance.dialoguePanel.activeSelf;
        panel.SetActive(visible);
        if (visible)
        {
            hintText.text = requestedMessage;
        }
        requestedMessage = null;
    }

    private static InteractionHint Create()
    {
        GameObject canvasObject = new GameObject("InteractionHint", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        // 다른 UI와 같은 기준 해상도로 맞춰 화면 크기에 따라 함께 커지고 작아지게 한다
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2778f, 1284f);
        scaler.matchWidthOrHeight = 0.5f;

        // 잔디 위에서도 잘 읽히도록 글자 크기에 맞춰 늘어나는 반투명 배경을 깐다
        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 140f);
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.14f, 0.12f, 0.12f, 0.75f);
        background.raycastTarget = false;
        HorizontalLayoutGroup layout = panel.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 20, 20);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = GameManager.instance.dialogueText.font;
        text.fontSize = 56f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = new Color32(255, 250, 235, 255);
        text.raycastTarget = false;
        panel.SetActive(false);

        InteractionHint hint = canvasObject.AddComponent<InteractionHint>();
        hint.panel = panel;
        hint.hintText = text;
        return hint;
    }
}
