using UnityEngine;

// 한 판의 기록과 점수. 씬이 바뀌어도 남도록 static으로 두고,
// 게임이 끝나는 순간 Finish()로 한 번 계산해 결과 화면(웹 랭킹)에 넘긴다.
// 점수 공식은 웹의 api/scores.js와 같아야 한다 (서버가 같은 공식으로 다시 계산해 저장한다).
public static class GameResult
{
    public const int PointsPerAnimal = 100;          // 울타리에 넣은 동물 한 마리
    public const int PointsPerBerry = 20;            // 먹은 열매 한 개
    public const int ClearBonus = 1000;              // 클리어했을 때만
    public const int PointsPerRemainingMinute = 2;   // 클리어했을 때 자정까지 남은 게임 시간 1분
    public const int PointsPerStamina = 5;           // 클리어했을 때 남은 체력 1%

    public static int BerriesEaten { get; private set; }
    public static int AnimalsCaptured { get; private set; }
    public static int TotalAnimals { get; private set; }
    public static bool IsVictory { get; private set; }
    public static int RemainingMinutes { get; private set; }
    public static int Stamina { get; private set; }

    public static int Score =>
        AnimalsCaptured * PointsPerAnimal
        + BerriesEaten * PointsPerBerry
        + (IsVictory ? ClearBonus + RemainingMinutes * PointsPerRemainingMinute + Stamina * PointsPerStamina : 0);

    // 게임 중 지금까지의 점수: 울타리에 넣은 동물과 먹은 열매 (클리어 보너스·남은 시간·체력은 끝날 때 더해진다)
    public static int CurrentScore(int capturedAnimals)
    {
        return capturedAnimals * PointsPerAnimal + BerriesEaten * PointsPerBerry;
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
        AnimalsCaptured = 0;
        foreach (Animal animal in animals)
        {
            if (animal.isCaptured)
                AnimalsCaptured++;
        }

        // 자정에 끝났다면 시계가 이미 0시로 돌아가 있으므로 남은 시간은 0분이다
        DayNightCycle cycle = Object.FindAnyObjectByType<DayNightCycle>();
        RemainingMinutes = isVictory && cycle != null && !cycle.IsDayOver() ? cycle.GetRemainingMinutes() : 0;

        PlayerStatus status = Object.FindAnyObjectByType<PlayerStatus>();
        Stamina = status != null ? Mathf.RoundToInt(status.stamina) : 0;

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
