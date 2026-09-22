using UnityEngine;

// 한 판의 기록과 점수. 씬이 바뀌어도 남도록 static으로 두고,
// 게임이 끝나는 순간 Finish()로 한 번 계산해 결과 화면(웹 랭킹)에 넘긴다.
// 점수 공식은 웹의 api/scores.js와 같아야 한다 (서버가 같은 공식으로 다시 계산해 저장한다).
public static class GameResult
{
    public const int PointsPerAnimal = 100;          // 잡은 동물 한 마리 (따라오는 동물 포함)
    public const int PointsPerBerry = 20;            // 먹은 열매 한 개
    public const int ClearBonus = 1000;              // 클리어했을 때만
    public const int PointsPerRemainingMinute = 2;   // 자정까지 남은 게임 시간 1분 (잡은 비율만큼)
    public const int PointsPerStamina = 5;           // 남은 체력 1%

    public static int BerriesEaten { get; private set; }
    public static int AnimalsCaptured { get; private set; }
    public static int TotalAnimals { get; private set; }
    public static bool IsVictory { get; private set; }
    public static int RemainingMinutes { get; private set; }
    public static int Stamina { get; private set; }

    public static int Score => Calculate(AnimalsCaptured, TotalAnimals, BerriesEaten, RemainingMinutes, Stamina, IsVictory);

    // 잡은 동물 + 먹은 열매 + 남은 시간 + 남은 체력, 클리어하면 보너스를 더한다.
    // 남은 시간 점수는 잡은 비율만큼만 준다: 빨리 많이 잡을수록 커지고, 일찍 쓰러졌다고 커지지는 않는다.
    public static int Calculate(int animals, int totalAnimals, int berries, int remainingMinutes, int stamina, bool victory)
    {
        int timePoints = totalAnimals > 0 ? remainingMinutes * PointsPerRemainingMinute * animals / totalAnimals : 0;
        return animals * PointsPerAnimal
            + berries * PointsPerBerry
            + timePoints
            + stamina * PointsPerStamina
            + (victory ? ClearBonus : 0);
    }

    // 게임 중 지금 끝나면 받을 점수 (클리어 보너스는 클리어할 때 더해진다)
    public static int CurrentScore(Animal[] animals, DayNightCycle cycle, PlayerStatus status)
    {
        return Calculate(CountCaught(animals), animals.Length, BerriesEaten, RemainingMinutesOf(cycle), StaminaOf(status), false);
    }

    // 잡은 동물 수 (울타리에 넣었거나 따라오는 중). 튜토리얼 중에는 동물이 아직 도망치기 전이라 세지 않는다.
    public static int CountCaught(Animal[] animals)
    {
        if (GameManager.instance != null && GameManager.instance.IsInitialDialogue())
            return 0;

        int caught = 0;
        foreach (Animal animal in animals)
        {
            if (animal.isCaptured || animal.isFollowing)
                caught++;
        }
        return caught;
    }

    // 자정까지 남은 게임 시간 (자정이 되면 시계가 0시로 돌아가 있으므로 0분)
    private static int RemainingMinutesOf(DayNightCycle cycle)
    {
        return cycle != null && !cycle.IsDayOver() ? cycle.GetRemainingMinutes() : 0;
    }

    private static int StaminaOf(PlayerStatus status)
    {
        return status != null ? Mathf.RoundToInt(status.stamina) : 0;
    }

    public static void Reset()
    {
        BerriesEaten = 0;
        AnimalsCaptured = 0;
        TotalAnimals = 0;
        IsVictory = false;
        RemainingMinutes = 0;
        Stamina = 0;
    }

    public static void AddBerry()
    {
        BerriesEaten++;
    }

    // 게임이 끝나는 곳(모두 잡음, 체력 0, 자정)에서 결과 화면으로 넘어가기 직전에 부른다
    public static void Finish(bool isVictory)
    {
        IsVictory = isVictory;

        Animal[] animals = Object.FindObjectsByType<Animal>(FindObjectsSortMode.None);
        TotalAnimals = animals.Length;
        AnimalsCaptured = CountCaught(animals);
        RemainingMinutes = RemainingMinutesOf(Object.FindAnyObjectByType<DayNightCycle>());
        Stamina = StaminaOf(Object.FindAnyObjectByType<PlayerStatus>());

        // 결과 화면이 클리어/게임오버 중 무엇을 보여줄지 정하는 기존 값 (0 = 승리)
        PlayerPrefs.SetInt("IsVictory", isVictory ? 0 : 1);
    }

    [System.Serializable]
    private class Summary
    {
        public bool victory;
        public int animals;
        public int totalAnimals;
        public int berries;
        public int remainingMinutes;
        public int stamina;
        public int score;
    }

    public static string ToJson()
    {
        return JsonUtility.ToJson(new Summary
        {
            victory = IsVictory,
            animals = AnimalsCaptured,
            totalAnimals = TotalAnimals,
            berries = BerriesEaten,
            remainingMinutes = RemainingMinutes,
            stamina = Stamina,
            score = Score,
        });
    }
}
