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
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Sprites.Init();
            SpriteMat = Resources.Load<Material>("HordeSprite");
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
            Cam.transform.position = new Vector3(0f, 0f, -10f);
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

            Player.Tick(dt, Joystick.Read());
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

        void SetUpCamera()
        {
            Cam = Camera.main;
            if (Cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                Cam = go.AddComponent<Camera>();
            }
            Cam.orthographic = true;
            Cam.orthographicSize = 8.5f;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.035f, 0.04f, 0.07f);
            Cam.transform.position = new Vector3(0f, 0f, -10f);
            if (Cam.GetComponent<AudioListener>() == null) Cam.gameObject.AddComponent<AudioListener>();
        }

        void FollowCamera(float dt)
        {
            float halfH = Cam.orthographicSize, halfW = halfH * Cam.aspect;
            Vector2 target = Player.Pos;
            target.x = Mathf.Clamp(target.x, -ArenaHalfW + halfW, ArenaHalfW - halfW);
            target.y = Mathf.Clamp(target.y, -ArenaHalfH + halfH, ArenaHalfH - halfH);
            camBase = Vector2.Lerp(camBase, target, 1f - Mathf.Exp(-10f * dt));
            trauma = Mathf.Max(0f, trauma - dt * 2.2f);
            float s = trauma * trauma * 0.35f;
            float sx = s > 0f ? Random.Range(-s, s) : 0f;
            float sy = s > 0f ? Random.Range(-s, s) : 0f;
            Cam.transform.position = new Vector3(camBase.x + sx, camBase.y + sy, -10f);
        }

        Vector2 camBase;
        float trauma;

        /// <summary>Adds screen shake; it stacks and fades out on its own.</summary>
        public void SnapCamera() => camBase = Player.Pos;

        public void Shake(float amount) => trauma = Mathf.Min(1f, trauma + amount);

        void BuildArena()
        {
            var root = new GameObject("Arena").transform;
            var floor = NewSprite("Floor", Sprites.Floor, Color.white, -20, root);
            floor.drawMode = SpriteDrawMode.Tiled;
            floor.size = new Vector2(ArenaHalfW * 2f, ArenaHalfH * 2f);
            var grid = new Color(0.25f, 0.35f, 0.55f, 0.05f);
            for (float x = -ArenaHalfW; x <= ArenaHalfW + 0.01f; x += 3f)
                Line(root, new Vector2(x, 0f), new Vector2(0.04f, ArenaHalfH * 2f), grid, -10);
            for (float y = -ArenaHalfH; y <= ArenaHalfH + 0.01f; y += 3f)
                Line(root, new Vector2(0f, y), new Vector2(ArenaHalfW * 2f, 0.04f), grid, -10);

            var wall = new Color(0.3f, 0.9f, 1f, 0.85f);
            const float t = 0.16f;
            Line(root, new Vector2(-ArenaHalfW, 0f), new Vector2(t, ArenaHalfH * 2f + t), wall, -5);
            Line(root, new Vector2(ArenaHalfW, 0f), new Vector2(t, ArenaHalfH * 2f + t), wall, -5);
            Line(root, new Vector2(0f, -ArenaHalfH), new Vector2(ArenaHalfW * 2f + t, t), wall, -5);
            Line(root, new Vector2(0f, ArenaHalfH), new Vector2(ArenaHalfW * 2f + t, t), wall, -5);
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
