using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 오른쪽 아래 동물 수 옆에 지금까지의 점수(별 + 숫자)를 실시간으로 보여준다.
// 동물 수 표시의 아이콘·숫자를 복제해 같은 모양으로 만든다.
// 클리어 보너스·남은 시간·남은 체력 점수는 게임이 끝날 때 더해진다.
public class ScoreDisplay : MonoBehaviour
{
    public Image iconTemplate;       // 동물 수 옆 아이콘 (CowImage)
    public TMP_Text valueTemplate;   // 동물 수 숫자 (CowValue)
    public Sprite scoreIcon;         // 별
    public float valueRightX = -330f; // 점수 숫자의 오른쪽 끝 (소 아이콘 바로 왼쪽)
    public float valueY = 60f;
    public float iconSize = 70f;
    public float iconGap = 10f;

    private Animal[] animals;
    private TMP_Text scoreText;
    private RectTransform iconRect;
    private int shownScore = -1;

    private void Start()
    {
        animals = FindObjectsByType<Animal>(FindObjectsSortMode.None);

        Image icon = Instantiate(iconTemplate, iconTemplate.transform.parent);
        icon.name = "ScoreImage";
        icon.sprite = scoreIcon;
        SpriteRenderer strayRenderer = icon.GetComponent<SpriteRenderer>();
        if (strayRenderer != null)
        {
            Destroy(strayRenderer);
        }
        iconRect = icon.rectTransform;
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);

        scoreText = Instantiate(valueTemplate, valueTemplate.transform.parent);
        scoreText.name = "ScoreValue";
        scoreText.horizontalAlignment = HorizontalAlignmentOptions.Right;
        RectTransform valueRect = scoreText.rectTransform;
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(valueRightX, valueY);
        valueRect.sizeDelta = new Vector2(240f, valueRect.sizeDelta.y);
    }

    private void Update()
    {
        // 튜토리얼 동안에는 동물이 아직 도망치기 전이라 잡은 것으로 치지 않는다
        int captured = 0;
        if (!GameManager.instance.IsInitialDialogue())
        {
            foreach (Animal animal in animals)
            {
                if (animal.isCaptured)
                    captured++;
            }
        }

        int score = GameResult.CurrentScore(captured);
        if (score == shownScore)
            return;

        shownScore = score;
        scoreText.text = score.ToString("N0");
        // 별은 숫자 길이에 맞춰 숫자 바로 왼쪽에 붙인다
        float numberLeft = valueRightX - scoreText.preferredWidth;
        iconRect.anchoredPosition = new Vector2(numberLeft - iconGap - iconSize / 2f, valueY + 5f);
    }
}
