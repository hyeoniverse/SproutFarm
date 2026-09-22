using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerStatus : MonoBehaviour
{
    public float stamina = 100f;
    public TMP_Text staminaText; // 스테미나를 표시할 UI 텍스트
    public Image staminaIcon; // 스테미나 아이콘을 표시할 이미지
    public Sprite[] staminaIcons; // 각 스테미나 단계에 해당하는 아이콘 배열

    public float maxStamina = 100f;
    public float recoveryRate = 1f; // 스테미나 회복 속도 (초당 회복량)

    public AudioClip warningSound;
    private AudioSource warningSoundAudioSource;

    private float staminaChange = 0f; // 체력 변화량을 저장하는 변수

    private void Awake()
    {
        warningSoundAudioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        ApplyStaminaChange();
        UpdateStaminaDisplay();
        if (stamina == 0f)
        {
            ShowGameOver();
        }
    }

    public void DecreaseStamina(float amount)
    {
        staminaChange -= amount;
    }

    public void IncreaseStamina(float amount)
    {
        staminaChange += amount;
    }

    public void ApplyStaminaChange()
    {
        stamina = Mathf.Clamp(stamina + staminaChange, 0f, maxStamina);
        staminaChange = 0f;
    }

    private void UpdateStaminaDisplay()
    {
        staminaText.text = $"{stamina:F0}%";
        staminaIcon.sprite = staminaIcons[GetStaminaIconIndex()];
    }

    private int GetStaminaIconIndex()
    {
        if (stamina > 80f) return 0;
        if (stamina > 60f) return 1;
        if (stamina > 40f) return 2;
        if (stamina > 20f) return 3;
        if (stamina > 0f)
        {
            if (!warningSoundAudioSource.isPlaying)
            {
                warningSoundAudioSource.PlayOneShot(warningSound);
            }
            return 4;
        }

        return 5;
    }

    private void ShowGameOver()
    {
        GameResult.Finish(false); // 패배로 기록
        SceneManager.LoadScene("ClearScene", LoadSceneMode.Single);
    }
}
