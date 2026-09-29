using System.Runtime.InteropServices;
using UnityEngine;

// 터치 기기에서 화면 조작(MobileControls)으로 들어온 입력. 키보드 입력과 함께 읽어서 어느 쪽으로든 같은 일을 하게 한다.
public static class MobileInput
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int SproutFarm_IsTouchDevice();
    private static int touchDevice = -1;
#endif

    public static Vector2 Move;       // 조이스틱 방향. 키보드 방향키처럼 8방향으로 맞춰 둔다
    public static bool RunButton;     // 달리기 버튼을 누르고 있는 동안 true
    public static bool InteractButton; // 확인 버튼을 누른 그 프레임에만 true

    // 손가락이 주 입력인 기기(휴대폰·태블릿)인지. 웹에서는 페이지에 묻고, 에디터에서는 Device Simulator를 따른다.
    public static bool Enabled
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (touchDevice < 0)
            {
                touchDevice = SproutFarm_IsTouchDevice();
            }
            return touchDevice == 1;
#else
            return Application.isMobilePlatform;
#endif
        }
    }

    // Space 바 또는 확인 버튼을 누른 프레임
    public static bool InteractDown => Input.GetKeyDown(KeyCode.Space) || InteractButton;

    // Shift 키 또는 달리기 버튼을 누르고 있는 동안
    public static bool RunHeld => Input.GetKey(KeyCode.LeftShift) || RunButton;

    // 조작 안내 문구를 키보드용과 터치용 중에서 고른다
    public static string Text(string keyboard, string touch)
    {
        return Enabled ? touch : keyboard;
    }

    public static void Clear()
    {
        Move = Vector2.zero;
        RunButton = false;
        InteractButton = false;
    }
}
