using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 왼쪽 아래에 지금 끝나면 받을 환산 점수(잡은 동물·열매·남은 시간·남은 체력)를 별 + "점수 N"으로 실시간 보여준다.
// 오른쪽 아래 동물 수 표시의 아이콘·숫자를 복제해 같은 모양으로 만든다. 클리어 보너스는 클리어할 때 더해진다.
public class ScoreDisplay : MonoBehaviour
{
    public Image iconTemplate;       // 동물 수 옆 아이콘 (CowImage)
    public TMP_Text valueTemplate;   // 동물 수 숫자 (CowValue)
    public Sprite scoreIcon;         // 별
    public Vector2 iconPosition = new Vector2(70f, 65f);  // 화면 왼쪽 아래 모서리 기준
    public float iconSize = 70f;
    public Vector2 textPosition = new Vector2(115f, 60f); // 글자의 왼쪽 끝

    private Animal[] animals;
    private DayNightCycle dayNightCycle;
    private PlayerStatus playerStatus;
    private TMP_Text scoreText;
    private int shownScore = -1;

    private void Start()
    {
        animals = FindObjectsByType<Animal>(FindObjectsSortMode.None);
        dayNightCycle = FindAnyObjectByType<DayNightCycle>();
        playerStatus = FindAnyObjectByType<PlayerStatus>();
        Vector2 bottomLeft = Vector2.zero;

        Image icon = Instantiate(iconTemplate, iconTemplate.transform.parent);
        icon.name = "ScoreImage";
        icon.sprite = scoreIcon;
        SpriteRenderer strayRenderer = icon.GetComponent<SpriteRenderer>();
        if (strayRenderer != null)
        {
            Destroy(strayRenderer);
        }
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = bottomLeft;
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.anchoredPosition = iconPosition;

        scoreText = Instantiate(valueTemplate, valueTemplate.transform.parent);
        scoreText.name = "ScoreValue";
        scoreText.horizontalAlignment = HorizontalAlignmentOptions.Left;
        RectTransform valueRect = scoreText.rectTransform;
        valueRect.anchorMin = valueRect.anchorMax = bottomLeft;
        valueRect.pivot = new Vector2(0f, 0.5f);
        valueRect.sizeDelta = new Vector2(400f, valueRect.sizeDelta.y);
        valueRect.anchoredPosition = textPosition;
    }

    private void Update()
    {
        int score = GameResult.CurrentScore(animals, dayNightCycle, playerStatus);
        if (score == shownScore)
            return;

        shownScore = score;
        scoreText.text = "점수 " + score.ToString("N0");
    }
}
