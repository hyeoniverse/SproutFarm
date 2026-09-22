using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 침대·집·울타리 근처에서 무엇을 할 수 있는지 알려주는 안내 문구.
// 대화창 UI를 복제해 같은 모습으로 띄우되, 대화와 달리 시간을 멈추거나 Space 바를 가로채지 않고
// Show()를 부른 프레임에만 보인다. 한 프레임에 여러 곳에서 부르면 priority가 높은 문구가 보인다.
public class InteractionHint : MonoBehaviour
{
    public string portraitTrigger = "Blinking"; // 안내가 뜰 때 대화창 얼굴 표정

    private static InteractionHint instance;

    private GameObject panel;
    private TMP_Text hintText;
    private Animator portrait;
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
        // GameManager가 대화창을 들고 있으므로 준비된 뒤에 만든다
        BuildHintDialogue();
    }

    private void LateUpdate()
    {
        if (panel == null)
            return;

        // 진짜 대화가 떠 있을 때는 겹치지 않도록 숨긴다
        string message = Time.time < flashUntil ? flashMessage : requestedMessage;
        bool visible = message != null && !GameManager.instance.dialoguePanel.activeSelf;
        if (visible)
        {
            if (!panel.activeSelf)
            {
                panel.SetActive(true);
                if (portrait != null)
                {
                    portrait.SetTrigger(portraitTrigger);
                }
            }
            hintText.text = message;
        }
        else
        {
            panel.SetActive(false);
        }
        requestedMessage = null;
    }

    // 대화창을 복제하고, 타자 효과·타자 소리·넘기기 화살표·버튼(튜토리얼 건너뛰기)을 떼어 안내 전용으로 만든다
    private void BuildHintDialogue()
    {
        // 복제본의 Awake/OnEnable(소리 재생 등)이 돌기 전에 손보려고 비활성 부모 아래에 만든다
        GameObject holder = new GameObject("HintDialogueHolder");
        holder.SetActive(false);
        panel = Instantiate(GameManager.instance.dialoguePanel, holder.transform);
        panel.name = "HintDialogue";

        foreach (TypeEffect typeEffect in panel.GetComponentsInChildren<TypeEffect>(true))
        {
            DestroyImmediate(typeEffect);
        }
        foreach (AudioSource audioSource in panel.GetComponentsInChildren<AudioSource>(true))
        {
            DestroyImmediate(audioSource);
        }
        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            DestroyImmediate(button.gameObject);
        }
        Transform cursor = panel.transform.Find("Cusor");
        if (cursor != null)
        {
            cursor.gameObject.SetActive(false);
        }
        Transform emoji = panel.transform.Find("Emoji");
        portrait = emoji != null ? emoji.GetComponent<Animator>() : null;
        hintText = panel.GetComponentInChildren<TMP_Text>(true);

        panel.SetActive(false);
        panel.transform.SetParent(transform, false);
        Destroy(holder);
    }
}
