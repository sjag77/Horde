using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click project setup and Android build, usable from the HORDE menu or from the
/// command line with -executeMethod HordeSetup.Setup / HordeSetup.BuildAndroid.
/// </summary>
public static class HordeSetup
{
    const string Root = "Assets/_Horde";
    const string ScenePath = Root + "/Scenes/Main.unity";
    const string MaterialPath = Root + "/Resources/HordeSprite.mat";
    const string ApkPath = "Builds/Android/HORDE.apk";

    [MenuItem("HORDE/1. Set Up Project")]
    public static void Setup()
    {
        Directory.CreateDirectory(Root + "/Scenes");
        Directory.CreateDirectory(Root + "/Resources");
        AssetDatabase.Refresh();
        CreateSpriteMaterial();
        CreateMainScene();
        ConfigureAndroid();
        AssetDatabase.SaveAssets();
        Debug.Log("[HORDE] Setup complete: scene, sprite material, Android player settings.");
    }

    [MenuItem("HORDE/2. Build Android APK")]
    public static void BuildAndroid()
    {
        Setup();
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        });

        var s = report.summary;
        Debug.Log("[HORDE] Build " + s.result + ": " + (s.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, "
                  + s.totalErrors + " errors, " + s.totalTime.TotalSeconds.ToString("0") + " s -> " + ApkPath);
        if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }

    static void CreateSpriteMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null) return;
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        AssetDatabase.CreateAsset(new Material(shader), MaterialPath);
    }

    static void CreateMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.035f, 0.04f, 0.07f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        new GameObject("Game").AddComponent<Horde.Game>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    static void ConfigureAndroid()
    {
        var android = NamedBuildTarget.Android;
        PlayerSettings.companyName = "Horde Studio";
        PlayerSettings.productName = "HORDE";
        PlayerSettings.bundleVersion = "0.0.1";
        PlayerSettings.SetApplicationIdentifier(android, "com.hordestudio.horde");
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;   // Google Play requires 64-bit
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;            // Unity 6.3 minimum: Android 7.1
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.bundleVersionCode = 2;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
    }
}
