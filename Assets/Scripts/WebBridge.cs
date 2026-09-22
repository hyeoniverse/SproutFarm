using System.Runtime.InteropServices;
using UnityEngine;

// 웹 페이지(index.html)와 주고받는 통로. WebGL 빌드에서만 동작하고, 에디터에서는 아무것도 하지 않는다.
public static class WebBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SproutFarm_ShowResult(string json);
#endif

    // 게임 결과를 웹 페이지로 보내 점수·랭킹 화면을 띄운다
    public static void ShowResult(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // 랭킹에 이름을 입력할 수 있도록 키보드 입력을 페이지에 넘긴다
        WebGLInput.captureAllKeyboardInput = false;
        SproutFarm_ShowResult(json);
#endif
    }

    // 게임 화면으로 돌아오면 다시 게임이 키보드 입력을 받는다
    public static void CaptureKeyboard()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = true;
#endif
    }
}
