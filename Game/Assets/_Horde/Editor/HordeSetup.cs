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
        ConfigureRenderPipeline();
        CreateMainScene();
        ConfigureAndroid();
        ConfigureIcon();
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

    // A desktop player, used only to eyeball the 3D rendering before shipping an APK.
    [MenuItem("HORDE/3. Build Mac (verification)")]
    public static void BuildMac()
    {
        Setup();
        string path = "Builds/Mac/HORDE.app";
        Directory.CreateDirectory("Builds/Mac");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = BuildTarget.StandaloneOSX,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.Development
        });
        Debug.Log("[HORDE] Mac build " + report.summary.result + " -> " + path);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
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
        PlayerSettings.bundleVersion = "0.2.0";
        PlayerSettings.SetApplicationIdentifier(android, "com.hordestudio.horde");
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;   // Google Play requires 64-bit
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;            // Unity 6.3 minimum: Android 7.1
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.bundleVersionCode = 4;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.gpuSkinning = false;
    }

    // v0.1.0 is a 3D game: swap URP off the 2D renderer and onto a forward 3D renderer
    // with a shadow-casting main light.
    static void ConfigureRenderPipeline()
    {
        const string urpPath = "Assets/Settings/UniversalRP.asset";
        const string rendererPath = "Assets/Settings/Renderer3D.asset";
        var urp = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(urpPath);
        if (urp == null) { Debug.LogWarning("[HORDE] No URP asset at " + urpPath); return; }

        var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(rendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, rendererPath);
            AssetDatabase.SaveAssets();
        }

        var so = new SerializedObject(urp);
        var list = so.FindProperty("m_RendererDataList");
        if (list != null)
        {
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        }
        SetBool(so, "m_MainLightShadowsSupported", true);
        SetBool(so, "m_SupportsHDR", false);
        SetInt(so, "m_MainLightRenderingMode", 1);
        SetInt(so, "m_ShadowCascadeCount", 1);
        SetFloat(so, "m_ShadowDistance", 38f);
        SetInt(so, "m_MSAA", 1);          // MSAA breaks the uGUI overlay render pass in URP 17
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(urp);
        Debug.Log("[HORDE] URP switched to the 3D forward renderer.");
    }

    static void SetBool(SerializedObject so, string path, bool v) { var p = so.FindProperty(path); if (p != null) p.boolValue = v; }
    static void SetInt(SerializedObject so, string path, int v) { var p = so.FindProperty(path); if (p != null) p.intValue = v; }
    static void SetFloat(SerializedObject so, string path, float v) { var p = so.FindProperty(path); if (p != null) p.floatValue = v; }

    // The launcher icon: one 1024 PNG, handed to every Android icon slot.
    static void ConfigureIcon()
    {
        const string iconPath = "Assets/_Horde/Art/AppIcon.png";
        var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (importer != null && (!importer.isReadable || importer.npotScale != TextureImporterNPOTScale.None))
        {
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (tex == null) { Debug.LogWarning("[HORDE] No app icon at " + iconPath); return; }

        var android = NamedBuildTarget.Android;
        int n = PlayerSettings.GetIconSizes(android, IconKind.Application).Length;
        var icons = new Texture2D[n];
        for (int i = 0; i < n; i++) icons[i] = tex;
        PlayerSettings.SetIcons(android, icons, IconKind.Application);
        Debug.Log("[HORDE] App icon applied.");
    }
}
