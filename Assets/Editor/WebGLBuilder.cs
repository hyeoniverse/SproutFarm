using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 웹 배포용 WebGL 빌드 (WebGL 브랜치의 index.html이 쓰는 Build/SproutFarm.* 파일을 만든다)
// 명령줄: Unity -batchmode -quit -projectPath <프로젝트> -buildTarget WebGL -executeMethod WebGLBuilder.Build
public static class WebGLBuilder
{
    private const string OutputPath = "Builds/WebGL/SproutFarm";

    [MenuItem("Build/WebGL (Web Deploy)")]
    public static void Build()
    {
        // index.html은 압축 해제 대체 기능이 켜진 Brotli 빌드(.unityweb)를 불러온다
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildReport report = BuildPipeline.BuildPlayer(scenes, OutputPath, BuildTarget.WebGL, BuildOptions.None);
        Debug.Log($"WebGL build {report.summary.result}: {report.summary.totalSize} bytes, {report.summary.totalErrors} errors");

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
