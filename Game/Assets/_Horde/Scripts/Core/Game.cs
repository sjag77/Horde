using System.Collections.Generic;
using UnityEngine;

namespace Horde
{
    public enum GameState { Playing, LevelUp, Paused, Over, Menu, BossReward }

    [System.Serializable]
    public class ScoreEntry
    {
        public float time;
        public int kills, level, bosses;
    }

    [System.Serializable]
    public class ScoreBook
    {
        public List<ScoreEntry> runs = new List<ScoreEntry>();
    }

    /// <summary>
    /// The single MonoBehaviour that runs a HORDE run. Every system is a plain C# object
    /// ticked from here in a fixed order: no Update() per entity, no Instantiate during play.
    /// </summary>
    public sealed class Game : MonoBehaviour
    {
        public const float ArenaHalfW = 32f;
        public const float ArenaHalfH = 32f;
        public const float RunLength = 600f; // 10:00

        public GameState State { get; private set; }
        public float RunTime { get; private set; }
        public int Kills { get; set; }

        public Camera Cam { get; private set; }
        public Material SpriteMat { get; private set; }
        public Material LitMat { get; private set; }
        public Material GlowMat { get; private set; }
        public Light KeyLight { get; private set; }
        /// <summary>Rotation that makes a flat quad face the tilted camera.</summary>
        public Quaternion Billboard => Cam.transform.rotation;
        /// <summary>How much arena the camera shows, in world units (the 3D view is a trapezoid,
        /// so this is the honest half-extent at the hero's feet).</summary>
        public float ViewHalfH { get; private set; } = 8.8f;
        public float ViewHalfW => ViewHalfH * Mathf.Max(0.45f, Cam.aspect);
        public const float CamPitch = 42f;      // degrees of tilt: enough to see the models stand up
        public const float CamFov = 46f;
        public Player Player { get; private set; }
        public EnemySystem Enemies { get; private set; }
        public ProjectileSystem Projectiles { get; private set; }
        public XpSystem Xp { get; private set; }
        public AbilitySet Abilities { get; private set; }
        public Upgrades Upgrades { get; private set; }
        public FloatingJoystick Joystick { get; private set; }
        public Hud Hud { get; private set; }
        public DamageNumbers Numbers { get; private set; }
        public Fx Fx { get; private set; }
        public Sfx Sfx { get; private set; }
        public PickupSystem Pickups { get; private set; }
        public Ultimate Ultimate { get; private set; }
        public BossBullets BossBullets { get; private set; }
        public Portals Portals { get; private set; }
        public HeroKit Heroes { get; private set; }

        void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Sprites.Init();
            Models.Init();
            SpriteMat = Resources.Load<Material>("HordeSprite");
            LitMat = MakeMaterial("Horde/Lit", "HordeLitMat");
            GlowMat = MakeMaterial("Horde/Glow", "HordeGlowMat");
            SetUpCamera();
            BuildArena();

            Player = new Player(this);
            Enemies = new EnemySystem(this, 600);
            Projectiles = new ProjectileSystem(this, 400);
            Xp = new XpSystem(this, 900);
            Abilities = new AbilitySet(this);
            Upgrades = new Upgrades(this);
            Joystick = new FloatingJoystick();
            Hud = new Hud(this);
            Numbers = new DamageNumbers(this, Hud);
            Fx = new Fx(this);
            Sfx = new Sfx(gameObject);
            Pickups = new PickupSystem(this);
            Ultimate = new Ultimate(this);
            BossBullets = new BossBullets(this);
            Portals = new Portals(this);
            Heroes = new HeroKit(this);

            LoadSettings();
            LoadProfile();
            ShowMenu();
            Hud.ShowWarning();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-hordeshot") >= 0)
                StartCoroutine(CaptureRun());
        }

        public void StartRun()
        {
            RunTime = 0f;
            Kills = 0;
            Player.Reset();
            Enemies.Reset();
            Projectiles.Reset();
            Xp.Reset();
            Abilities.Reset();
            Upgrades.Reset();
            Numbers.Reset();
            Fx.Reset();
            Pickups.Reset();
            Ultimate.Reset();
            BossBullets.Reset();
            Portals.Reset();
            bossRewardPending = false;
            Heroes.Reset(SelectedHero);
            Player.SetColor(HeroKit.Tint(SelectedHero));
            camBase = Vector2.zero;
            trauma = 0f;
            Abilities.Grant(HeroKit.StartAbility(SelectedHero));
            Cam.transform.position = CamPos(Vector2.zero);
            SetState(GameState.Playing);
        }

        public void SetState(GameState next)
        {
            State = next;
            Time.timeScale = next == GameState.Playing ? 1f : 0f;
            Hud.OnState(next);
        }

        void Update()
        {
            Hud.Tick(Time.unscaledDeltaTime);
            if (State != GameState.Playing) return;

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            RunTime += dt;

            Player.Tick(dt, autoMove.sqrMagnitude > 0.01f ? autoMove : Joystick.Read());
            if (Joystick.ConsumeBlink()) Player.Blink();
            Portals.Tick(dt);
            Enemies.Tick(dt);
            BossBullets.Tick(dt);
            Abilities.Tick(dt);
            Heroes.Tick(dt);
            Ultimate.Tick(dt);
            Projectiles.Tick(dt);
            Xp.Tick(dt);
            Pickups.Tick(dt);
            Fx.Tick(dt);
            FollowCamera(dt);
            Numbers.Tick(dt);

            if (Player.Hp <= 0f) EndRun(false);
            else if (bossRewardPending) OpenBossReward();
            else if (Xp.PendingLevelUps > 0) OpenLevelUp();
        }

        void OpenLevelUp()
        {
            var options = Upgrades.Roll();
            if (options.Count == 0)
            {
                Xp.PendingLevelUps = 0;
                if (State != GameState.Playing) SetState(GameState.Playing);
                return;
            }
            Hud.ShowLevelUp(options);
            Sfx.Play(Sound.LevelUp, 0f);
            SetState(GameState.LevelUp);
        }

        public void OnUpgradeChosen()
        {
            if (State == GameState.BossReward) bossRewardPending = false;
            else Xp.PendingLevelUps = Mathf.Max(0, Xp.PendingLevelUps - 1);
            if (Xp.PendingLevelUps > 0) OpenLevelUp();
            else SetState(GameState.Playing);
        }

        void EndRun(bool survived)
        {
            SaveScore();
            SetState(GameState.Over);
            Hud.ShowGameOver(survived, RunTime, Kills, Player.Level);
        }

        // ---- boss rewards -------------------------------------------------------------------
        bool bossRewardPending;
        readonly List<UpgradeOption> rewards = new List<UpgradeOption>(2);

        public void QueueBossReward() => bossRewardPending = true;

        void OpenBossReward()
        {
            rewards.Clear();
            if (Abilities.SlotLimit < AbilitySet.MaxOwned)
                rewards.Add(new UpgradeOption
                {
                    Title = "+1 Ability Slot",
                    Desc = "Carry " + (Abilities.SlotLimit + 1) + " abilities instead of " + Abilities.SlotLimit + " (max " + AbilitySet.MaxOwned + ").",
                    Apply = () => Abilities.AddSlot()
                });
            if (Ultimate.Rank < Ultimate.MaxRank)
                rewards.Add(new UpgradeOption
                {
                    Title = Ultimate.Rank == 0 ? "Ultimate" : "Ultimate  Rank " + (Ultimate.Rank + 1),
                    Desc = Ultimate.Rank == 0 ? "Unlock a tap-to-cast blast that wipes out every enemy on your screen (long cooldown)." : "Bigger blast damage and a shorter cooldown.",
                    Apply = () => Ultimate.OnBossKilled()
                });
            if (rewards.Count == 0)
            {
                bossRewardPending = false;
                Player.Heal(Player.MaxHp);
                return;
            }
            Hud.ShowBossReward(rewards);
            Sfx.Play(Sound.LevelUp, 0f);
            SetState(GameState.BossReward);
        }

        // ---- profile: hero colour + best runs (PlayerPrefs) -----------------------------------
        public static readonly Color[] HeroColors =
        {
            new Color(0.35f, 0.95f, 1f), new Color(0.45f, 1f, 0.55f), new Color(1f, 0.82f, 0.3f),
            new Color(1f, 0.45f, 0.8f), new Color(0.7f, 0.55f, 1f), new Color(0.95f, 0.97f, 1f)
        };

        public int HeroColorIndex { get; private set; }
        public int LastRank { get; private set; } = -1;
        public List<ScoreEntry> Scores => book.runs;
        ScoreBook book = new ScoreBook();

        void LoadProfile()
        {
            HeroColorIndex = 0;   // colour choice removed: the hero is always cyan
            string json = PlayerPrefs.GetString("horde.scores", "");
            if (!string.IsNullOrEmpty(json))
            {
                try { book = JsonUtility.FromJson<ScoreBook>(json) ?? new ScoreBook(); }
                catch { book = new ScoreBook(); }
            }
            if (book.runs == null) book.runs = new List<ScoreEntry>();
        }

        public void SetHeroColor(int index)
        {
            HeroColorIndex = Mathf.Clamp(index, 0, HeroColors.Length - 1);
            PlayerPrefs.SetInt("horde.color", HeroColorIndex);
            PlayerPrefs.Save();
            Player.SetColor(HeroColors[HeroColorIndex]);
            Hud.RefreshMenu();
        }

        void SaveScore()
        {
            var entry = new ScoreEntry { time = RunTime, kills = Kills, level = Player.Level, bosses = Enemies.BossesKilled };
            book.runs.Add(entry);
            book.runs.Sort((a, b) => b.time != a.time ? b.time.CompareTo(a.time) : b.kills.CompareTo(a.kills));
            if (book.runs.Count > 8) book.runs.RemoveRange(8, book.runs.Count - 8);
            LastRank = book.runs.IndexOf(entry);
            PlayerPrefs.SetString("horde.scores", JsonUtility.ToJson(book));
            PlayerPrefs.Save();
        }

        public void ShowMenu()
        {
            StartRun();                  // fresh arena behind the menu
            SetState(GameState.Menu);
            Hud.RefreshMenu();
        }

        // ---- hero choice -------------------------------------------------------------------
        public HeroId SelectedHero { get; private set; }

        public void StartWithHero(HeroId id)
        {
            SelectedHero = id;
            PlayerPrefs.SetInt("horde.hero", (int)id);
            PlayerPrefs.Save();
            StartRun();
        }

        // ---- settings (PlayerPrefs) ---------------------------------------------------------
        static readonly float[] JoySizes = { 0.8f, 1f, 1.25f };
        public float Volume { get; private set; } = 0.8f;
        public int JoystickSize { get; private set; } = 1;
        public bool DashOnLeft { get; private set; }
        public bool DoubleTapDash { get; private set; } = true;
        public int EasyAttempts { get; private set; }
        public string JoystickSizeName => JoystickSize == 0 ? "SMALL" : JoystickSize == 1 ? "MEDIUM" : "LARGE";

        void LoadSettings()
        {
            SelectedHero = (HeroId)Mathf.Clamp(PlayerPrefs.GetInt("horde.hero", 0), 0, HeroKit.Count - 1);
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat("horde.volume", 0.8f));
            JoystickSize = Mathf.Clamp(PlayerPrefs.GetInt("horde.joySize", 1), 0, 2);
            DashOnLeft = PlayerPrefs.GetInt("horde.dashLeft", 0) == 1;
            DoubleTapDash = PlayerPrefs.GetInt("horde.doubleTap", 1) == 1;
            ApplySettings();
        }

        public void ChangeVolume(int step) { Volume = Mathf.Clamp01(Mathf.Round((Volume + step * 0.1f) * 10f) / 10f); SaveSettings(); }
        public void ChangeJoystickSize(int step) { JoystickSize = Mathf.Clamp(JoystickSize + step, 0, 2); SaveSettings(); }
        public void ToggleDashSide() { DashOnLeft = !DashOnLeft; SaveSettings(); }
        public void ToggleDoubleTap() { DoubleTapDash = !DoubleTapDash; SaveSettings(); }

        /// <summary>There is no easy mode. Returns true on every third attempt, when the player earns a lecture.</summary>
        public bool TryEasyMode()
        {
            EasyAttempts++;
            if (EasyAttempts < 3) return false;
            EasyAttempts = 0;
            return true;
        }

        void SaveSettings()
        {
            PlayerPrefs.SetFloat("horde.volume", Volume);
            PlayerPrefs.SetInt("horde.joySize", JoystickSize);
            PlayerPrefs.SetInt("horde.dashLeft", DashOnLeft ? 1 : 0);
            PlayerPrefs.SetInt("horde.doubleTap", DoubleTapDash ? 1 : 0);
            PlayerPrefs.Save();
            ApplySettings();
        }

        void ApplySettings()
        {
            AudioListener.volume = Volume;
            Joystick.SizeScale = JoySizes[JoystickSize];
            Joystick.DoubleTapBlink = DoubleTapDash;
            Hud.ApplyControls();
        }

        public void Pause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        // Pause only when the app really goes to the background, never on a mere focus change.
        void OnApplicationPause(bool paused)
        {
            if (paused && State == GameState.Playing) SetState(GameState.Paused);
        }

        // A tilted perspective camera looking down at the XY battlefield. The gameplay stays 2D;
        // the models stand up out of it towards the lens.
        // Build-verification hook: plays a run unattended and writes screenshots, so the
        // 3D rendering can be checked without a phone in hand. Never runs in a shipped build
        // unless the player is launched with -hordeshot.
        System.Collections.IEnumerator CaptureRun()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            Hud.CloseWarning();
            StartWithHero(HeroId.Ember);
            string dir = System.Environment.GetEnvironmentVariable("HORDE_SHOT_DIR") ?? "/tmp";
            float[] at = { 4f, 12f, 24f, 40f };
            var move = new Vector2(0.6f, 0.4f).normalized;
            for (int i = 0; i < at.Length; i++)
            {
                float until = at[i];
                while (RunTime < until)
                {
                    autoMove = Quaternion.Euler(0f, 0f, Mathf.Sin(RunTime * 0.7f) * 70f) * move;
                    // take the first offered upgrade so the run never stalls on a menu
                    if (State == GameState.LevelUp || State == GameState.BossReward) Hud.AutoPick();
                    yield return null;
                }
                ScreenCapture.CaptureScreenshot(dir + "/horde_shot_" + i + ".png");
                yield return new WaitForSecondsRealtime(1.2f);
            }
            Application.Quit();
        }

        Vector2 autoMove;
        public bool AutoPlay { get; private set; }

        void SetUpCamera()
        {
            Cam = Camera.main;
            if (Cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                Cam = go.AddComponent<Camera>();
            }
            Cam.orthographic = false;
            Cam.fieldOfView = CamFov;
            Cam.nearClipPlane = 0.5f;
            Cam.farClipPlane = 140f;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.035f, 0.04f, 0.07f);
            Cam.transform.rotation = Quaternion.Euler(CamPitch, 0f, 0f);
            Cam.transform.position = CamPos(Vector2.zero);
            if (Cam.GetComponent<AudioListener>() == null) Cam.gameObject.AddComponent<AudioListener>();

            // One hard key light from over the player's shoulder, plus the shader's sky fill.
            var lightGo = new GameObject("KeyLight");
            KeyLight = lightGo.AddComponent<Light>();
            KeyLight.type = LightType.Directional;
            KeyLight.color = new Color(1f, 0.94f, 0.85f);
            KeyLight.intensity = 1.35f;
            KeyLight.shadows = LightShadows.Hard;
            KeyLight.shadowStrength = 0.55f;
            KeyLight.shadowBias = 0.06f;
            KeyLight.shadowNormalBias = 0.5f;
            lightGo.transform.rotation = Quaternion.LookRotation(new Vector3(-0.42f, -0.55f, 1f).normalized, Vector3.up);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.30f);
            RenderSettings.fog = false;
        }

        /// <summary>Where the camera sits to frame a point on the arena floor.</summary>
        Vector3 CamPos(Vector2 look)
        {
            float p = CamPitch * Mathf.Deg2Rad;
            float dist = ViewHalfH / Mathf.Tan(CamFov * 0.5f * Mathf.Deg2Rad);
            return new Vector3(look.x, look.y, 0f) + new Vector3(0f, Mathf.Sin(p), -Mathf.Cos(p)) * dist;
        }

        Material MakeMaterial(string shaderName, string name)
        {
            var sh = Shader.Find(shaderName);
            if (sh == null) return null;
            var m = new Material(sh) { name = name, enableInstancing = true };
            return m;
        }

        /// <summary>A pooled 3D object drawn with the lit (or additive) material.</summary>
        public Rig NewRig(string name, Mesh mesh, Color tint, Transform parent = null, bool glow = false, bool shadows = true)
        {
            var rig = new Rig(name, mesh, glow ? GlowMat : LitMat, parent, shadows && !glow);
            rig.SetTint(tint, glow ? 1f : 0f);
            return rig;
        }

        void FollowCamera(float dt)
        {
            float halfH = ViewHalfH, halfW = ViewHalfW;
            Vector2 target = Player.Pos;
            target.x = Mathf.Clamp(target.x, -ArenaHalfW + halfW, ArenaHalfW - halfW);
            target.y = Mathf.Clamp(target.y, -ArenaHalfH + halfH, ArenaHalfH - halfH);
            camBase = Vector2.Lerp(camBase, target, 1f - Mathf.Exp(-10f * dt));
            trauma = Mathf.Max(0f, trauma - dt * 2.2f);
            float s = trauma * trauma * 0.35f;
            float sx = s > 0f ? Random.Range(-s, s) : 0f;
            float sy = s > 0f ? Random.Range(-s, s) : 0f;
            Cam.transform.position = CamPos(new Vector2(camBase.x + sx, camBase.y + sy));
        }

        Vector2 camBase;
        float trauma;

        /// <summary>The point on the arena floor the camera is looking at (not where it sits).</summary>
        public Vector2 CamFocus => camBase;

        /// <summary>Adds screen shake; it stacks and fades out on its own.</summary>
        public void SnapCamera() => camBase = Player.Pos;

        public void Shake(float amount) => trauma = Mathf.Min(1f, trauma + amount);

        void BuildArena()
        {
            var root = new GameObject("Arena").transform;
            var floor = NewSprite("Floor", Sprites.Floor, Color.white, -20, root);
            floor.drawMode = SpriteDrawMode.Tiled;
            floor.size = new Vector2(ArenaHalfW * 2f, ArenaHalfH * 2f);
            floor.transform.position = new Vector3(0f, 0f, 0.06f);   // just under the decals
            BuildWalls(root);
            var grid = new Color(0.25f, 0.35f, 0.55f, 0.05f);
            for (float x = -ArenaHalfW; x <= ArenaHalfW + 0.01f; x += 3f)
                Line(root, new Vector2(x, 0f), new Vector2(0.04f, ArenaHalfH * 2f), grid, -10);
            for (float y = -ArenaHalfH; y <= ArenaHalfH + 0.01f; y += 3f)
                Line(root, new Vector2(0f, y), new Vector2(ArenaHalfW * 2f, 0.04f), grid, -10);

        }

        // Real walls around the arena so the edge of the world reads in 3D.
        void BuildWalls(Transform root)
        {
            var b = new MeshBuilder();
            var stone = new Color(0.30f, 0.34f, 0.46f);
            var cap = new Color(0.55f, 0.85f, 1f);
            const float t = 0.7f, h = 1.7f;
            float w = ArenaHalfW + t * 0.5f, d = ArenaHalfH + t * 0.5f;
            // built in model space (Y up) and laid down with the same Stand rotation as everything else
            b.Box(new Vector3(-w, h * 0.5f, 0f), new Vector3(t, h, d * 2f + t * 2f), stone, 0.7f);
            b.Box(new Vector3(w, h * 0.5f, 0f), new Vector3(t, h, d * 2f + t * 2f), stone, 0.7f);
            b.Box(new Vector3(0f, h * 0.5f, -d), new Vector3(w * 2f + t * 2f, h, t), stone, 0.7f);
            b.Box(new Vector3(0f, h * 0.5f, d), new Vector3(w * 2f + t * 2f, h, t), stone, 0.7f);
            b.Box(new Vector3(-w, h + 0.06f, 0f), new Vector3(t * 0.7f, 0.12f, d * 2f + t * 2f), cap);
            b.Box(new Vector3(w, h + 0.06f, 0f), new Vector3(t * 0.7f, 0.12f, d * 2f + t * 2f), cap);
            b.Box(new Vector3(0f, h + 0.06f, -d), new Vector3(w * 2f + t * 2f, 0.12f, t * 0.7f), cap);
            b.Box(new Vector3(0f, h + 0.06f, d), new Vector3(w * 2f + t * 2f, 0.12f, t * 0.7f), cap);
            var rig = NewRig("Walls", b.Build("Walls"), Color.white, root);
            rig.Tr.rotation = Rig.Stand;
        }

        void Line(Transform parent, Vector2 center, Vector2 size, Color color, int order)
        {
            var sr = NewSprite("Line", Sprites.Square, color, order, parent);
            sr.transform.position = center;
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        public SpriteRenderer NewSprite(string name, Sprite sprite, Color color, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (SpriteMat != null) sr.sharedMaterial = SpriteMat;
            return sr;
        }
    }
}
