using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DayNightCycle : MonoBehaviour
{
    public int hoursPerDay = 24;
    public int minutesPerHour = 60;
    public float realSecondsPerGameMinute = 1f;
    public float hoursToExhaustion = 4f; // 쉬지 않고 걸으면 이 게임 시간이 지나 체력이 바닥난다
    public float idleDrainRatio = 0.5f;  // 가만히 있을 때는 걸을 때의 이 비율만큼만 닳는다

    public TMP_Text timeText;
    public TMP_Text dayText;
    public Image screenOverlay;

    private int currentDay = 1;
    private int currentHour = 9;
    private int currentMinute = 0;
    private float timer = 0f;
    private Gradient overlayGradient;
    private PlayerStatus playerStatus;

    private float playerSpeed = 0f;
    private float continuousMovementTime = 0f;

    private void Awake()
    {
        playerStatus = FindObjectOfType<PlayerStatus>();
    }

    private void Start()
    {
        InitializeGradient();
        DisplayTime();
        UpdateLighting();
    }

    private void Update()
    {
        if (GameManager.instance.dialoguePanel.activeSelf)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= realSecondsPerGameMinute)
        {
            timer = 0f;
            UpdateGameTime();
        }

        DisplayTime();
        UpdateLighting();
    }

    private void InitializeGradient()
    {
        GradientColorKey[] colorKey = {
            new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 0.0f),
            new GradientColorKey(new Color(0.1f, 0.1f, 0.25f), 4.0f / 24.0f),
            new GradientColorKey(new Color(0.1f, 0.2f, 0.5f), 6.0f / 24.0f),
            new GradientColorKey(new Color(0.9f, 0.8f, 0.9f), 10.0f / 24.0f),
            new GradientColorKey(new Color(0.9f, 0.9f, 1.0f), 13.0f / 24.0f),
            new GradientColorKey(new Color(0.9f, 0.5f, 0.4f), 16.0f / 24.0f),
            new GradientColorKey(new Color(0.5f, 0.2f, 0.3f), 18.0f / 24.0f),
            new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 20.0f / 24.0f)
        };

        GradientAlphaKey[] alphaKey = {
            new GradientAlphaKey(0.8f, 0.0f),
            new GradientAlphaKey(0.7f, 4.0f / 24.0f),
            new GradientAlphaKey(0.4f, 6.0f / 24.0f),
            new GradientAlphaKey(0.0f, 10.0f / 24.0f),
            new GradientAlphaKey(0.0f, 13.0f / 24.0f),
            new GradientAlphaKey(0.2f, 16.0f / 24.0f),
            new GradientAlphaKey(0.5f, 18.0f / 24.0f),
            new GradientAlphaKey(0.8f, 20.0f / 24.0f)
        };

        overlayGradient = new Gradient();
        overlayGradient.SetKeys(colorKey, alphaKey);
    }

    private void UpdateGameTime()
    {
        currentMinute++;
        if (currentMinute >= minutesPerHour)
        {
            currentMinute = 0;
            currentHour++;
            if (currentHour >= hoursPerDay)
            {
                currentHour = 0;
                currentDay++;

                GameResult.Finish(AllAnimalsCaptured());
                SceneManager.LoadScene("ClearScene", LoadSceneMode.Single);
            }
        }

        DecreasePlayerStamina();
    }

    private void DisplayTime()
    {
        string period = currentHour < 12 ? "오전" : "오후";
        int displayHour = currentHour % 12 == 0 ? 12 : currentHour % 12;
        timeText.text = $"{period} {displayHour:D2}시";
        dayText.text = $"Day {currentDay}";
    }

    private void UpdateLighting()
    {
        float timeNormalized = (currentHour * minutesPerHour + currentMinute) / (float)(hoursPerDay * minutesPerHour);
        screenOverlay.color = overlayGradient.Evaluate(timeNormalized);
    }
    
    private void DecreasePlayerStamina()
    {
        if (playerStatus != null)
        {
            float baseStaminaDecreaseRate = 100f / (hoursToExhaustion * minutesPerHour); // 걸을 때 게임 1분마다 닳는 양
            float adjustedStaminaDecreaseRate = baseStaminaDecreaseRate * (playerSpeed);

            if (playerSpeed != 0f)
            {
                continuousMovementTime += Time.deltaTime;
                adjustedStaminaDecreaseRate *= 1 + (continuousMovementTime / 60f);
                playerStatus.DecreaseStamina(adjustedStaminaDecreaseRate);
            }
            else
            {
                continuousMovementTime = 0f;
                playerStatus.DecreaseStamina(baseStaminaDecreaseRate * idleDrainRatio);
            }
        }
    }

    public void UpdatePlayerSpeed(float speed)
    {
        playerSpeed = speed;
    }

    public int GetCurrentDay() => currentDay;
    public int GetCurrentHour() => currentHour;
    public int GetCurrentMinute() => currentMinute;

    // 자정까지 남은 게임 시간(1시간 = 60분 기준, 랭킹 점수용)
    public int GetRemainingMinutes()
    {
        int remaining = hoursPerDay * minutesPerHour - (currentHour * minutesPerHour + currentMinute);
        return remaining * 60 / minutesPerHour;
    }

    public bool IsDayOver()
    {
        return currentHour == 0 && currentMinute == 0;
    }

    private bool AllAnimalsCaptured()
    {
        foreach (GameObject animal in GameObject.FindGameObjectsWithTag("Animal"))
        {
            Animal animalScript = animal.GetComponent<Animal>();
            if (animalScript != null && !animalScript.isCaptured)
            {
                return false;
            }
        }
        return true;
    }
}
