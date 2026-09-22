using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 처음 튜토리얼 대화 중에만 대화 상자 오른쪽 위에 "건너뛰기" 버튼을 띄운다.
// 누르면 남은 튜토리얼을 건너뛰고(동물들이 아직 도망치기 전이면 바로 풀어 준 뒤) 게임을 시작한다.
public class DialogueSkipButton : MonoBehaviour
{
    public RectTransform dialogueBox; // 대화창에서 실제로 보이는 상자
    public Sprite normalSprite;       // 글자 없는 넓은 버튼
    public Sprite pressedSprite;      // 같은 버튼이 눌린 모양
    public string label = "건너뛰기";
    public Vector2 size = new Vector2(240f, 80f);
    public float fontSize = 34f;
    public Vector2 offset = new Vector2(-30f, 10f); // 상자 오른쪽 위 모서리에서 버튼 오른쪽 아래 모서리까지

    private GameObject button;

    private void Start()
    {
        GameManager gameManager = GameManager.instance;

        button = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = (RectTransform)button.transform;
        buttonRect.SetParent(dialogueBox, false);
        buttonRect.anchorMin = Vector2.one;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = offset;
        buttonRect.sizeDelta = size;
        button.GetComponent<Image>().sprite = normalSprite;

        Button skip = button.GetComponent<Button>();
        skip.transition = Selectable.Transition.SpriteSwap;
        skip.spriteState = new SpriteState { pressedSprite = pressedSprite };
        skip.onClick.AddListener(gameManager.SkipInitialDialogue);

        TextMeshProUGUI text = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(buttonRect, false);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.font = gameManager.dialogueText.font;
        text.fontSize = fontSize;
        text.color = gameManager.dialogueText.color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = label;

        // 이 씬에는 EventSystem이 없어서 버튼 클릭을 받으려면 만들어야 한다
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void LateUpdate()
    {
        bool visible = GameManager.instance.IsInitialDialogue();
        if (button.activeSelf != visible)
        {
            button.SetActive(visible);
        }

        // 버튼이 선택된 채로 남으면 대화를 넘기는 Space 바가 버튼 누르기로도 쓰이므로 선택을 풀어 둔다
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == button)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
