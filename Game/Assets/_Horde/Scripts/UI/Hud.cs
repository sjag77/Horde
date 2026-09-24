using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Horde
{
    /// <summary>
    /// All UI, built in code on one overlay canvas (reference 1080x1920, portrait).
    /// Nothing raycasts during play, so the joystick can use the whole screen.
    /// </summary>
    public sealed class Hud
    {
        static readonly Color Cyan = new Color(0.35f, 0.92f, 1f);
        static readonly Color Red = new Color(1f, 0.28f, 0.36f);
        static readonly Color Card = new Color(0.07f, 0.09f, 0.16f, 0.98f);
        static readonly Color Dim = new Color(0.02f, 0.03f, 0.06f, 0.82f);
        static readonly Color Muted = new Color(0.68f, 0.76f, 0.9f);
        static readonly Color Gold = new Color(1f, 0.78f, 0.25f);
        static readonly Vector2 Half = new Vector2(0.5f, 0.5f);

        readonly Game g;
        readonly Font font;
        readonly Canvas canvas;
        readonly RectTransform root, safe, hpFill, xpFill;
        readonly Text timeText, levelText, killsText, overTitle, overStats;
        readonly Image hurt, stickBase, stickKnob;
        readonly GameObject levelPanel, overPanel, pausePanel;
        readonly GameObject[] choiceCard = new GameObject[3];
        readonly Text[] choiceTitle = new Text[3];
        readonly Text[] choiceDesc = new Text[3];
        readonly RectTransform bossFill, ultRect;
        readonly GameObject bossBar, ultButton;
        readonly Image ultFill, flash;
        readonly Text bannerText, ultLabel;
        float bannerTime, flashAlpha;
        int shownUltCd = -1;
        readonly Image[] slotIcon = new Image[AbilitySet.MaxOwned], slotCd = new Image[AbilitySet.MaxOwned];
        readonly Text[] slotLv = new Text[AbilitySet.MaxOwned];
        readonly int[] shownSlotLv = { -1, -1, -1, -1, -1, -1 };
        readonly Image[] slotBg = new Image[AbilitySet.MaxOwned];
        Text choiceHeader, choiceSub, bossName, timelineLabel, menuScores;
        RectTransform timelineFill;
        GameObject timeline, menuPanel;
        int shownBossNum = -1, shownTimelineSec = -1;
        bool shownEndless;
        Image menuHero, swatchRing;
        readonly Vector2[] swatchPos = new Vector2[6];
        const int MapPx = 128;
        Texture2D mapTex;
        Color32[] mapBuf;
        RawImage mapImg;
        float mapTimer;
        RectTransform pauseRect;
        GameObject pauseButton, helpPanel, warningPanel;
        Image blinkShade, warpShade, dashShade;
        RectTransform dashRect;
        GameObject heroPanel, settingsPanel, alertPanel, helpPage1, helpPage2;
        Text volText, joyText, dashSideText, dtapText, diffNote;

        List<UpgradeOption> options;
        float choiceUnlockAt, hurtAlpha;
        int shownSecond = -1, shownLevel = -1;
        UnityEngine.Rect lastSafeArea;

        public Hud(Game g)
        {
            this.g = g;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var go = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;   // portrait: lock UI to width so tall phones gain height, not overlap
            root = (RectTransform)go.transform;
            EnsureEventSystem();

            hurt = Img("Hurt", root, new Color(1f, 0.1f, 0.2f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            stickBase = Img("StickBase", root, new Color(1f, 1f, 1f, 0.45f), Half, Half, Vector2.zero, Vector2.zero, Sprites.JoyBase);
            stickKnob = Img("StickKnob", root, new Color(0.35f, 0.92f, 1f, 0.9f), Half, Half, Vector2.zero, Vector2.zero, Sprites.JoyKnob);
            stickBase.enabled = false;
            stickKnob.enabled = false;

            safe = MakeRect("Safe", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var xpBg = Img("XpBg", safe, new Color(1f, 1f, 1f, 0.08f), new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -16f), Vector2.zero);
            xpFill = Img("XpFill", xpBg.transform, Cyan, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero).rectTransform;

            var hpBg = Img("HpBg", safe, new Color(1f, 1f, 1f, 0.10f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -86f), new Vector2(330f, -52f));
            hpFill = Img("HpFill", hpBg.transform, Red, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).rectTransform;
            levelText = Txt("Level", safe, "LV 1", 44, TextAnchor.MiddleLeft, Cyan, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -150f), new Vector2(330f, -92f));
            timeText = Txt("Time", safe, "0:00", 72, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-150f, -150f), new Vector2(150f, -30f));
            killsText = Txt("Kills", safe, "0 kills", 44, TextAnchor.MiddleRight, Muted, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-340f, -100f), new Vector2(-40f, -40f));

            var bossBg = Img("BossBg", safe, new Color(0f, 0f, 0f, 0.55f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-340f, -240f), new Vector2(340f, -208f));
            bossFill = Img("BossFill", bossBg.transform, Gold, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f)).rectTransform;
            bossName = Txt("BossName", bossBg.transform, "BOSS", 34, TextAnchor.LowerCenter, Gold, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 44f));
            bossBar = bossBg.gameObject;
            bossBar.SetActive(false);

            // Countdown to the next boss fight (replaced by the boss HP bar while a boss is alive).
            var tl = Img("BossTimeline", safe, new Color(0f, 0f, 0f, 0.55f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-340f, -234f), new Vector2(340f, -214f));
            timelineFill = Img("Fill", tl.transform, new Color(1f, 0.78f, 0.25f, 0.8f), Vector2.zero, new Vector2(0f, 1f), new Vector2(2f, 2f), new Vector2(-2f, -2f)).rectTransform;
            timelineLabel = Txt("Label", tl.transform, "", 32, TextAnchor.LowerCenter, Gold, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 40f));
            timeline = tl.gameObject;

            bannerText = Txt("Banner", root, "", 76, TextAnchor.MiddleCenter, Gold, Half, Half, new Vector2(-520f, 380f), new Vector2(520f, 520f));
            bannerText.enabled = false;

            // Bottom-right ULT button. A touch that starts on it never drives the joystick.
            var ultBg = Img("Ult", safe, new Color(0.06f, 0.07f, 0.12f, 0.9f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-300f, 150f), new Vector2(-80f, 370f), Sprites.Circle);
            ultBg.raycastTarget = true;
            ultRect = ultBg.rectTransform;
            ultFill = Img("UltFill", ultBg.transform, Gold, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f), Sprites.Circle);
            ultFill.type = Image.Type.Filled;
            ultFill.fillMethod = Image.FillMethod.Radial360;
            ultFill.fillOrigin = (int)Image.Origin360.Top;
            ultFill.fillClockwise = true;
            ultLabel = Txt("UltLabel", ultBg.transform, "ULT", 54, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ultBg.gameObject.AddComponent<Button>().onClick.AddListener(() => g.Ultimate.Cast());
            ultButton = ultBg.gameObject;
            ultButton.SetActive(false);
            g.Joystick.IsBlocked = pt => (ultButton.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(ultRect, pt, null))
                || (pauseButton != null && pauseButton.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(pauseRect, pt, null))
                || (dashRect != null && dashRect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(dashRect, pt, null));

            flash = Img("Flash", root, new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Bottom-left: ability bar (up to 6 slots; locked slots hidden) with the minimap above it.
            for (int i = 0; i < AbilitySet.MaxOwned; i++)
            {
                float x = 40f + i * 112f;
                slotBg[i] = Img("Slot" + i, safe, new Color(0.05f, 0.06f, 0.11f, 0.85f), Vector2.zero, Vector2.zero, new Vector2(x, 160f), new Vector2(x + 100f, 260f));
                Img("SlotEdge", slotBg[i].transform, new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -4f), Vector2.zero);
                slotIcon[i] = Img("Icon", slotBg[i].transform, Color.white, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f));
                slotCd[i] = Img("Cd", slotBg[i].transform, new Color(0f, 0f, 0f, 0.65f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Sprites.Square);
                slotCd[i].type = Image.Type.Filled;
                slotCd[i].fillMethod = Image.FillMethod.Vertical;
                slotCd[i].fillOrigin = (int)Image.OriginVertical.Top;
                slotLv[i] = Txt("Lv", slotBg[i].transform, "", 28, TextAnchor.LowerRight, Color.white, Vector2.zero, Vector2.one, new Vector2(0f, 2f), new Vector2(-6f, 0f));
                slotIcon[i].enabled = false;
                slotCd[i].enabled = false;
            }

            var mapFrame = Img("Minimap", safe, new Color(0.3f, 0.8f, 1f, 0.35f), Vector2.zero, Vector2.zero, new Vector2(40f, 280f), new Vector2(300f, 540f));
            mapTex = new Texture2D(MapPx, MapPx, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            mapBuf = new Color32[MapPx * MapPx];
            mapImg = MakeRect("MapImage", mapFrame.transform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f)).gameObject.AddComponent<RawImage>();
            mapImg.texture = mapTex;
            mapImg.raycastTarget = false;

            // Top-right pause button (under the kill counter).
            var pauseBg = Img("PauseBtn", safe, new Color(0.06f, 0.07f, 0.12f, 0.85f), Vector2.one, Vector2.one, new Vector2(-150f, -270f), new Vector2(-40f, -160f));
            pauseBg.raycastTarget = true;
            pauseRect = pauseBg.rectTransform;
            Img("BarL", pauseBg.transform, Color.white, Half, Half, new Vector2(-24f, -30f), new Vector2(-8f, 30f));
            Img("BarR", pauseBg.transform, Color.white, Half, Half, new Vector2(8f, -30f), new Vector2(24f, 30f));
            pauseBg.gameObject.AddComponent<Button>().onClick.AddListener(() => g.Pause());
            pauseButton = pauseBg.gameObject;

            // DASH button (fires on touch-down; side is set in Settings) and the portal-warp cooldown pip.
            var dashBg = Img("DashBtn", safe, new Color(0.05f, 0.06f, 0.11f, 0.9f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-300f, 400f), new Vector2(-140f, 560f), Sprites.Circle);
            dashBg.raycastTarget = true;
            dashRect = dashBg.rectTransform;
            Img("Ring", dashBg.transform, Cyan, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Sprites.Ring);
            var dashIcon = Img("Icon", dashBg.transform, Cyan, Vector2.zero, Vector2.one, new Vector2(38f, 38f), new Vector2(-38f, -38f), Sprites.Hero);
            dashIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            dashShade = Img("Cd", dashBg.transform, new Color(0f, 0f, 0f, 0.7f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Sprites.Circle);
            dashShade.type = Image.Type.Filled;
            dashShade.fillMethod = Image.FillMethod.Radial360;
            dashShade.fillOrigin = (int)Image.Origin360.Top;
            dashShade.fillClockwise = false;
            dashShade.fillAmount = 0f;
            Txt("Label", dashBg.transform, "DASH", 30, TextAnchor.UpperCenter, Color.white, Vector2.zero, new Vector2(1f, 0f), new Vector2(-20f, -40f), new Vector2(20f, 0f));
            var trig = dashBg.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => { if (g.State == GameState.Playing) g.Player.Blink(); });
            trig.triggers.Add(down);
            warpShade = MakeCooldownPip("WarpPip", new Vector2(-120f, 590f), new Vector2(-30f, 680f), Sprites.JoyBase, new Color(1f, 0.35f, 0.85f), "WARP");

            levelPanel = BuildLevelPanel();
            overPanel = BuildOverPanel(out overTitle, out overStats);
            pausePanel = BuildPausePanel();
            menuPanel = BuildMenuPanel();
            helpPanel = BuildHelpPanel();
            heroPanel = BuildHeroPanel();
            settingsPanel = BuildSettingsPanel();
            warningPanel = BuildWarningPanel();
            alertPanel = BuildAlertPanel();
        }

        public Font Font => font;

        /// <summary>A full-screen UI layer drawn above the HUD but below every panel.</summary>
        public RectTransform CreateLayerBelowPanels(string name)
        {
            var layer = MakeRect(name, root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            layer.SetSiblingIndex(levelPanel.transform.GetSiblingIndex());
            return layer;
        }

        public void OnState(GameState state)
        {
            levelPanel.SetActive(state == GameState.LevelUp || state == GameState.BossReward);
            if (menuPanel != null) menuPanel.SetActive(state == GameState.Menu);
            if (helpPanel != null && state != GameState.Menu) helpPanel.SetActive(false);
            if (heroPanel != null && state != GameState.Menu) heroPanel.SetActive(false);
            if (settingsPanel != null && state != GameState.Menu && state != GameState.Paused) settingsPanel.SetActive(false);
            overPanel.SetActive(state == GameState.Over);
            pausePanel.SetActive(state == GameState.Paused);
            if (state != GameState.Playing)
            {
                stickBase.enabled = false;
                stickKnob.enabled = false;
            }
        }

        public void ShowLevelUp(List<UpgradeOption> list)
        {
            choiceHeader.text = "LEVEL UP";
            choiceHeader.color = Cyan;
            choiceSub.text = "Choose one";
            options = list;
            for (int i = 0; i < 3; i++)
            {
                bool on = i < list.Count;
                choiceCard[i].SetActive(on);
                if (!on) continue;
                choiceTitle[i].text = list[i].Title;
                choiceDesc[i].text = list[i].Desc;
            }
            choiceUnlockAt = Time.unscaledTime + 0.35f; // a thumb already on the glass can't pick by accident
        }

        public void ShowBossReward(List<UpgradeOption> list)
        {
            ShowLevelUp(list);
            choiceHeader.text = "BOSS DEFEATED";
            choiceHeader.color = Gold;
            choiceSub.text = "Choose your reward";
        }

        public void ShowGameOver(bool survived, float time, int kills, int level)
        {
            overTitle.text = survived ? "SURVIVED" : "OVERWHELMED";
            overTitle.color = survived ? Cyan : Red;
            overStats.text = "Time  " + FormatTime(time) + "\nKills  " + kills + "\nLevel  " + level
                + "\nBosses  " + g.Enemies.BossesKilled + "/" + g.Enemies.BossTotal + (g.LastRank == 0 ? "\n\nNEW BEST RUN!" : "");
        }

        public void FlashHurt() => hurtAlpha = 0.32f;


        public void Banner(string text, Color color)
        {
            bannerText.text = text;
            color.a = 1f;
            bannerText.color = color;
            bannerText.enabled = true;
            bannerTime = 2.4f;
        }

        public void FlashScreen(Color color, float alpha)
        {
            flashAlpha = alpha;
            color.a = alpha;
            flash.color = color;
        }

        public void Tick(float unscaledDt)
        {
            ApplySafeArea();

            var p = g.Player;
            hpFill.anchorMax = new Vector2(Mathf.Clamp01(p.Hp / p.MaxHp), 1f);
            xpFill.anchorMax = new Vector2(Mathf.Clamp01(p.Xp / p.XpToNext), 1f);

            int second = (int)g.RunTime;
            if (second != shownSecond)
            {
                shownSecond = second;
                timeText.text = FormatTime(g.RunTime);
                killsText.text = g.Kills + " kills";   // once a second, not once per kill
            }
            if (p.Level != shownLevel)
            {
                shownLevel = p.Level;
                levelText.text = "LV " + p.Level;
            }

            if (hurtAlpha > 0f)
            {
                hurtAlpha = Mathf.Max(0f, hurtAlpha - unscaledDt * 1.6f);
                var c = hurt.color;
                c.a = hurtAlpha;
                hurt.color = c;
            }

            bool bossUp = g.Enemies.BossAlive;
            if (bossBar.activeSelf != bossUp) bossBar.SetActive(bossUp);
            if (bossUp) bossFill.anchorMax = new Vector2(Mathf.Clamp01(g.Enemies.BossHpFrac), 1f);

            var en = g.Enemies;
            if (timeline.activeSelf == bossUp) timeline.SetActive(!bossUp);
            if (bossUp)
            {
                if (en.BossesSpawned != shownBossNum) { shownBossNum = en.BossesSpawned; bossName.text = "BOSS " + shownBossNum + "/" + en.BossTotal; }
            }
            else if (en.BossesSpawned >= en.BossTotal)
            {
                if (!shownEndless)
                {
                    shownEndless = true;
                    timelineFill.anchorMax = Vector2.one;
                    timelineFill.GetComponent<Image>().color = Red;
                    timelineLabel.color = Red;
                    timelineLabel.text = "ALL BOSSES DOWN  -  ENDLESS SWARM";
                }
            }
            else
            {
                if (shownEndless)
                {
                    shownEndless = false;
                    timelineFill.GetComponent<Image>().color = new Color(1f, 0.78f, 0.25f, 0.8f);
                    timelineLabel.color = Gold;
                }
                int n = en.BossesSpawned;
                float prev = n == 0 ? 0f : en.BossTime(n - 1), next = en.BossTime(n);
                timelineFill.anchorMax = new Vector2(Mathf.Clamp01((g.RunTime - prev) / (next - prev)), 1f);
                int sec = Mathf.CeilToInt(next - g.RunTime);
                if (sec != shownTimelineSec)
                {
                    shownTimelineSec = sec;
                    timelineLabel.text = "BOSS " + (n + 1) + "/" + en.BossTotal + "  IN " + FormatTime(Mathf.Max(0f, next - g.RunTime));
                }
            }

            dashShade.fillAmount = g.Player.BlinkFrac;
            warpShade.fillAmount = g.Portals.CooldownFrac;

            bool showPause = g.State == GameState.Playing;
            if (pauseButton.activeSelf != showPause) pauseButton.SetActive(showPause);
            if (dashRect.gameObject.activeSelf != showPause) dashRect.gameObject.SetActive(showPause);

            var ult = g.Ultimate;
            bool hasUlt = ult.Rank > 0 && g.State != GameState.Over && g.State != GameState.Menu;
            if (ultButton.activeSelf != hasUlt) ultButton.SetActive(hasUlt);
            if (hasUlt)
            {
                bool ready = ult.Ready;
                float wave = Mathf.Sin(Time.unscaledTime * 6f);
                ultFill.fillAmount = ready ? 1f : 1f - ult.Cooldown / ult.MaxCooldown;
                var fc = Gold;
                fc.a = ready ? 0.85f + wave * 0.15f : 0.35f;
                ultFill.color = fc;
                ultRect.localScale = Vector3.one * (ready ? 1f + wave * 0.04f : 1f);
                int cd = ready ? 0 : Mathf.CeilToInt(ult.Cooldown);
                if (cd != shownUltCd)
                {
                    shownUltCd = cd;
                    ultLabel.text = ready ? "ULT" : cd.ToString();
                }
            }

            if (bannerTime > 0f)
            {
                bannerTime -= unscaledDt;
                var bc = bannerText.color;
                bc.a = Mathf.Clamp01(bannerTime / 0.5f);
                bannerText.color = bc;
                if (bannerTime <= 0f) bannerText.enabled = false;
            }
            if (flashAlpha > 0f)
            {
                flashAlpha = Mathf.Max(0f, flashAlpha - unscaledDt * 1.8f);
                var fl = flash.color;
                fl.a = flashAlpha;
                flash.color = fl;
            }

            var ab = g.Abilities;
            for (int i = 0; i < AbilitySet.MaxOwned; i++)
            {
                bool unlocked = i < ab.SlotLimit;
                if (slotBg[i].gameObject.activeSelf != unlocked) slotBg[i].gameObject.SetActive(unlocked);
                bool owned = i < ab.OwnedCount;
                if (slotIcon[i].enabled != owned) { slotIcon[i].enabled = owned; slotCd[i].enabled = owned; }
                if (!owned)
                {
                    if (shownSlotLv[i] != -1) { shownSlotLv[i] = -1; slotLv[i].text = ""; }
                    continue;
                }
                var id = ab.Slot(i);
                int lv = ab.Level(id);
                if (lv != shownSlotLv[i])
                {
                    shownSlotLv[i] = lv;
                    slotLv[i].text = lv >= AbilitySet.MaxLevel ? "MAX" : lv.ToString();
                    slotIcon[i].sprite = AbilitySet.Icon(id);
                    slotIcon[i].color = AbilitySet.Tint(id);
                }
                slotCd[i].fillAmount = ab.CooldownFrac(id);
            }

            mapTimer -= unscaledDt;
            if (mapTimer <= 0f && g.State != GameState.Menu)
            {
                mapTimer = 0.1f;
                DrawMinimap();
            }

            var js = g.Joystick;
            bool show = g.State == GameState.Playing && js.Active;
            stickBase.enabled = show;
            stickKnob.enabled = show;
            if (!show) return;
            float r = js.RadiusPx / canvas.scaleFactor;
            stickBase.rectTransform.sizeDelta = new Vector2(r * 2.2f, r * 2.2f);
            stickKnob.rectTransform.sizeDelta = new Vector2(r * 0.9f, r * 0.9f);
            stickBase.rectTransform.anchoredPosition = ScreenToCanvas(js.Origin);
            stickKnob.rectTransform.anchoredPosition = ScreenToCanvas(js.Knob);
            Color hero = g.Player.HeroColor;
            stickKnob.color = new Color(hero.r, hero.g, hero.b, 0.9f);
            stickBase.color = new Color(hero.r, hero.g, hero.b, 0.45f);
            Vector2 drag = js.Knob - js.Origin;
            if (drag.sqrMagnitude > 4f) stickKnob.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(drag.y, drag.x) * Mathf.Rad2Deg);
        }

        void Choose(int index)
        {
            if (options == null || index >= options.Count || Time.unscaledTime < choiceUnlockAt) return;
            var picked = options[index];
            options = null;
            picked.Apply();
            g.OnUpgradeChosen();
        }

        GameObject BuildLevelPanel()
        {
            var panel = Img("LevelUp", root, Dim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true; // swallow taps behind the cards
            choiceHeader = Txt("Title", panel.transform, "LEVEL UP", 96, TextAnchor.MiddleCenter, Cyan, Half, Half, new Vector2(-500f, 560f), new Vector2(500f, 700f));
            choiceSub = Txt("Sub", panel.transform, "Choose one", 44, TextAnchor.MiddleCenter, Muted, Half, Half, new Vector2(-500f, 480f), new Vector2(500f, 560f));
            for (int i = 0; i < 3; i++)
            {
                float y = 220f - i * 330f;
                var card = Img("Choice" + i, panel.transform, Card, Half, Half, new Vector2(-460f, y - 140f), new Vector2(460f, y + 140f));
                card.raycastTarget = true;
                int index = i;
                card.gameObject.AddComponent<Button>().onClick.AddListener(() => Choose(index));
                Img("Edge", card.transform, Cyan, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(10f, 0f));
                choiceTitle[i] = Txt("Title", card.transform, "", 58, TextAnchor.UpperLeft, Color.white, Vector2.zero, Vector2.one, new Vector2(48f, 20f), new Vector2(-36f, -34f));
                choiceDesc[i] = Txt("Desc", card.transform, "", 38, TextAnchor.LowerLeft, Muted, Vector2.zero, Vector2.one, new Vector2(48f, 30f), new Vector2(-36f, -118f));
                choiceCard[i] = card.gameObject;
            }
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        GameObject BuildOverPanel(out Text title, out Text stats)
        {
            var panel = Img("GameOver", root, Dim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;
            title = Txt("Title", panel.transform, "", 110, TextAnchor.MiddleCenter, Red, Half, Half, new Vector2(-520f, 260f), new Vector2(520f, 440f));
            stats = Txt("Stats", panel.transform, "", 52, TextAnchor.MiddleCenter, Color.white, Half, Half, new Vector2(-520f, -40f), new Vector2(520f, 240f));
            MakeButton(panel.transform, "PLAY AGAIN", new Vector2(0f, -260f), () => g.StartRun());
            MakeButton(panel.transform, "MENU", new Vector2(0f, -480f), () => g.ShowMenu());
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        GameObject BuildPausePanel()
        {
            var panel = Img("Paused", root, Dim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;
            Txt("Title", panel.transform, "PAUSED", 110, TextAnchor.MiddleCenter, Color.white, Half, Half, new Vector2(-500f, 120f), new Vector2(500f, 300f));
            MakeButton(panel.transform, "RESUME", new Vector2(0f, -80f), () => g.Resume());
            MakeButton(panel.transform, "HOME", new Vector2(0f, -300f), () => g.ShowMenu());
            MakeButton(panel.transform, "SETTINGS", new Vector2(0f, -520f), () => OpenSettings());
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        // ---- dashboard: title, how to play, best runs, play ----------------------------------------
        const string Rules =
            "- Drag anywhere to move. The stick stays where your thumb lands. Abilities fire on their own.\n" +
            "- DASH button (or double-tap): a quick dash with a split second of invulnerability. A boss charge locks onto where you stood - dash out of the red ring!\n" +
            "- Collect XP gems and pick 1 of 3 upgrades every level.\n" +
            "- Beat 4 bosses: each gives +1 ability slot (max 6) or the Ultimate. Kill them fast - bosses enrage over time.\n" +
            "- The minimap shows the next boss landing. Cheese heals, magnets pull XP, corner portals warp you (12 s).\n" +
            "- Settings: sound, joystick size, dash button side, double-tap dash.";

        GameObject BuildMenuPanel()
        {
            var panel = Img("Menu", root, new Color(0.02f, 0.03f, 0.06f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;
            Txt("Title", panel.transform, "HORDE", 170, TextAnchor.MiddleCenter, Cyan, Half, Half, new Vector2(-520f, 700f), new Vector2(520f, 900f));
            Txt("Tag", panel.transform, "Beat 4 bosses. Then survive the endless swarm.", 40, TextAnchor.MiddleCenter, Muted, Half, Half, new Vector2(-520f, 620f), new Vector2(520f, 690f));
            menuHero = Img("HeroPreview", panel.transform, Cyan, Half, Half, new Vector2(-110f, 390f), new Vector2(110f, 600f), Sprites.Hero);
            menuHero.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            MakeButton(panel.transform, "PLAY", new Vector2(0f, 250f), () => heroPanel.SetActive(true));
            MakeButton(panel.transform, "HOW TO PLAY", new Vector2(0f, 40f), () => { ShowHelpPage(1); helpPanel.SetActive(true); });
            MakeButton(panel.transform, "SETTINGS", new Vector2(0f, -170f), () => OpenSettings());
            Txt("BestLabel", panel.transform, "BEST RUNS", 44, TextAnchor.MiddleCenter, Gold, Half, Half, new Vector2(-520f, -340f), new Vector2(520f, -280f));
            menuScores = Txt("Scores", panel.transform, "", 34, TextAnchor.UpperCenter, Color.white, Half, Half, new Vector2(-500f, -800f), new Vector2(500f, -350f));
            Txt("Version", panel.transform, "v" + Application.version, 30, TextAnchor.MiddleCenter, Muted, Half, Half, new Vector2(-520f, -900f), new Vector2(520f, -840f));
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        // Two pages: rules + heroes, then abilities.
        GameObject BuildHelpPanel()
        {
            var panel = Img("Help", root, new Color(0.02f, 0.03f, 0.06f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;
            helpPage1 = MakeRect("Page1", panel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            helpPage2 = MakeRect("Page2", panel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var p1 = helpPage1.transform;
            var p2 = helpPage2.transform;

            Txt("Title", p1, "HOW TO PLAY", 80, TextAnchor.MiddleCenter, Cyan, Half, Half, new Vector2(-520f, 820f), new Vector2(520f, 930f));
            Txt("Rules", p1, Rules, 31, TextAnchor.UpperLeft, Color.white, Half, Half, new Vector2(-480f, 290f), new Vector2(480f, 800f));
            Txt("HeroesLabel", p1, "HEROES", 50, TextAnchor.MiddleCenter, Gold, Half, Half, new Vector2(-520f, 210f), new Vector2(520f, 270f));
            for (int h = 0; h < HeroKit.Count; h++)
            {
                var id = (HeroId)h;
                float y = 140f - h * 235f;
                var icon = Img("HeroIcon" + h, p1, HeroKit.Tint(id), Half, Half, new Vector2(-480f, y - 80f), new Vector2(-380f, y + 20f), Sprites.Hero);
                icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Txt("HeroName" + h, p1, HeroKit.Name(id) + "  -  " + HeroKit.Title(id), 36, TextAnchor.UpperLeft, HeroKit.Tint(id), Half, Half, new Vector2(-350f, y - 10f), new Vector2(480f, y + 40f));
                Txt("HeroText" + h, p1, HeroKit.Innate(id) + " Starts with " + AbilitySet.Name(HeroKit.StartAbility(id)) + ".", 27, TextAnchor.UpperLeft, Muted, Half, Half, new Vector2(-350f, y - 200f), new Vector2(480f, y - 10f));
            }
            MakeSmallButton(p1, "ABILITIES >", new Vector2(-200f, -760f), new Vector2(400f, 150f), () => ShowHelpPage(2));
            MakeSmallButton(p1, "BACK", new Vector2(220f, -760f), new Vector2(360f, 150f), () => helpPanel.SetActive(false));

            Txt("Title", p2, "ABILITIES", 80, TextAnchor.MiddleCenter, Gold, Half, Half, new Vector2(-520f, 820f), new Vector2(520f, 930f));
            for (int i = 0; i < AbilitySet.Count; i++)
            {
                var id = (AbilityId)i;
                float y = 690f - i * 185f;
                Img("Icon" + i, p2, AbilitySet.Tint(id), Half, Half, new Vector2(-480f, y - 80f), new Vector2(-380f, y + 20f), AbilitySet.Icon(id));
                Txt("Name" + i, p2, AbilitySet.Name(id), 40, TextAnchor.UpperLeft, AbilitySet.Tint(id), Half, Half, new Vector2(-350f, y - 10f), new Vector2(480f, y + 45f));
                Txt("Desc" + i, p2, AbilitySet.Describe(id, 1), 30, TextAnchor.UpperLeft, Muted, Half, Half, new Vector2(-350f, y - 100f), new Vector2(480f, y - 10f));
            }
            MakeSmallButton(p2, "< HEROES", new Vector2(-200f, -760f), new Vector2(400f, 150f), () => ShowHelpPage(1));
            MakeSmallButton(p2, "BACK", new Vector2(220f, -760f), new Vector2(360f, 150f), () => helpPanel.SetActive(false));

            ShowHelpPage(1);
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        void ShowHelpPage(int page)
        {
            helpPage1.SetActive(page == 1);
            helpPage2.SetActive(page == 2);
        }

        GameObject BuildHeroPanel()
        {
            var panel = Img("Heroes", root, new Color(0.02f, 0.03f, 0.06f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;
            Txt("Title", panel.transform, "CHOOSE YOUR HERO", 76, TextAnchor.MiddleCenter, Cyan, Half, Half, new Vector2(-520f, 740f), new Vector2(520f, 860f));
            for (int h = 0; h < HeroKit.Count; h++)
            {
                var id = (HeroId)h;
                Color tint = HeroKit.Tint(id);
                float y = 460f - h * 400f;
                var card = Img("Hero" + h, panel.transform, Card, Half, Half, new Vector2(-480f, y - 180f), new Vector2(480f, y + 180f));
                card.raycastTarget = true;
                card.gameObject.AddComponent<Button>().onClick.AddListener(() => g.StartWithHero(id));
                Img("Edge", card.transform, tint, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(12f, 0f));
                var ship = Img("Ship", card.transform, tint, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, -80f), new Vector2(200f, 80f), Sprites.Hero);
                ship.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Txt("Name", card.transform, HeroKit.Name(id), 56, TextAnchor.UpperLeft, tint, new Vector2(0f, 1f), Vector2.one, new Vector2(230f, -85f), new Vector2(-30f, -18f));
                Txt("Title", card.transform, HeroKit.Title(id), 32, TextAnchor.UpperLeft, Muted, new Vector2(0f, 1f), Vector2.one, new Vector2(230f, -130f), new Vector2(-30f, -85f));
                Txt("Innate", card.transform, HeroKit.Innate(id), 28, TextAnchor.UpperLeft, Color.white, new Vector2(0f, 1f), Vector2.one, new Vector2(230f, -285f), new Vector2(-30f, -135f));
                var start = HeroKit.StartAbility(id);
                Img("StartIcon", card.transform, AbilitySet.Tint(start), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(230f, 16f), new Vector2(290f, 76f), AbilitySet.Icon(start));
                Txt("Start", card.transform, "Starts with " + AbilitySet.Name(start), 32, TextAnchor.MiddleLeft, Gold, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(305f, 16f), new Vector2(-30f, 76f));
            }
            MakeButton(panel.transform, "BACK", new Vector2(0f, -760f), () => heroPanel.SetActive(false));
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        GameObject BuildSettingsPanel()
        {
            var panel = Img("Settings", root, new Color(0.02f, 0.03f, 0.06f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;
            var t = panel.transform;
            Txt("Title", t, "SETTINGS", 84, TextAnchor.MiddleCenter, Cyan, Half, Half, new Vector2(-520f, 740f), new Vector2(520f, 860f));
            volText = SettingRow(t, "SOUND", 540f, () => g.ChangeVolume(-1), () => g.ChangeVolume(1));
            joyText = SettingRow(t, "JOYSTICK SIZE", 350f, () => g.ChangeJoystickSize(-1), () => g.ChangeJoystickSize(1));
            SettingLabel(t, "DASH BUTTON", 160f);
            dashSideText = MakeSmallButton(t, "", new Vector2(250f, 160f), new Vector2(360f, 120f), () => g.ToggleDashSide());
            SettingLabel(t, "DOUBLE-TAP DASH", -30f);
            dtapText = MakeSmallButton(t, "", new Vector2(250f, -30f), new Vector2(360f, 120f), () => g.ToggleDoubleTap());
            SettingLabel(t, "DIFFICULTY", -220f);
            MakeSmallButton(t, "HARD", new Vector2(140f, -220f), new Vector2(210f, 120f), () => diffNote.text = "Good choice. It's the only choice.", Gold);
            MakeSmallButton(t, "EASY", new Vector2(370f, -220f), new Vector2(210f, 120f), OnEasyPressed);
            diffNote = Txt("DiffNote", t, "", 36, TextAnchor.MiddleCenter, Red, Half, Half, new Vector2(-500f, -400f), new Vector2(500f, -310f));
            MakeButton(t, "BACK", new Vector2(0f, -700f), () => settingsPanel.SetActive(false));
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        void SettingLabel(Transform parent, string label, float y) =>
            Txt(label, parent, label, 42, TextAnchor.MiddleLeft, Color.white, Half, Half, new Vector2(-480f, y - 50f), new Vector2(40f, y + 50f));

        Text SettingRow(Transform parent, string label, float y, UnityEngine.Events.UnityAction minus, UnityEngine.Events.UnityAction plus)
        {
            SettingLabel(parent, label, y);
            MakeSmallButton(parent, "-", new Vector2(110f, y), new Vector2(120f, 110f), minus);
            var value = Txt(label + "Value", parent, "", 42, TextAnchor.MiddleCenter, Cyan, Half, Half, new Vector2(170f, y - 50f), new Vector2(330f, y + 50f));
            MakeSmallButton(parent, "+", new Vector2(390f, y), new Vector2(120f, 110f), plus);
            return value;
        }

        void OpenSettings()
        {
            diffNote.text = "";
            RefreshSettings();
            settingsPanel.SetActive(true);
        }

        // Easy mode does not exist. Every third try earns a word from the developer.
        void OnEasyPressed()
        {
            g.Sfx.Play(Sound.Hurt, 0f);
            if (g.TryEasyMode())
            {
                diffNote.text = "";
                alertPanel.SetActive(true);
            }
            else diffNote.text = g.EasyAttempts == 1 ? "Nice try. Difficulty: HARD." : "Still HARD. Easy mode doesn't exist.";
        }

        GameObject BuildAlertPanel()
        {
            var panel = Img("Alert", root, new Color(0.02f, 0.03f, 0.06f, 0.97f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;
            Txt("Title", panel.transform, "HEY!", 130, TextAnchor.MiddleCenter, Gold, Half, Half, new Vector2(-520f, 380f), new Vector2(520f, 560f));
            Txt("Body", panel.transform, "Read the alerts in this game carefully :)", 58, TextAnchor.MiddleCenter, Color.white, Half, Half, new Vector2(-480f, 0f), new Vector2(480f, 340f));
            Txt("Sign", panel.transform, "SJ", 96, TextAnchor.MiddleCenter, Cyan, Half, Half, new Vector2(-520f, -200f), new Vector2(520f, -40f));
            MakeButton(panel.transform, "OKAY, OKAY", new Vector2(0f, -480f), () => alertPanel.SetActive(false));
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        public void RefreshSettings()
        {
            if (volText == null) return;
            volText.text = Mathf.RoundToInt(g.Volume * 100f) + "%";
            joyText.text = g.JoystickSizeName;
            dashSideText.text = g.DashOnLeft ? "LEFT" : "RIGHT";
            dtapText.text = g.DoubleTapDash ? "ON" : "OFF";
        }

        /// <summary>Moves the DASH button to the side chosen in Settings.</summary>
        public void ApplyControls()
        {
            if (dashRect == null) return;
            bool left = g.DashOnLeft;
            dashRect.anchorMin = dashRect.anchorMax = left ? Vector2.zero : new Vector2(1f, 0f);
            dashRect.offsetMin = left ? new Vector2(320f, 400f) : new Vector2(-300f, 400f);
            dashRect.offsetMax = left ? new Vector2(480f, 560f) : new Vector2(-140f, 560f);
            RefreshSettings();
        }

        public void ShowWarning() => warningPanel.SetActive(true);

        // Shown every launch: a friendly heads-up from the developer.
        GameObject BuildWarningPanel()
        {
            var panel = Img("Warning", root, new Color(0.02f, 0.03f, 0.06f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.raycastTarget = true;
            Txt("Title", panel.transform, "WARNING", 130, TextAnchor.MiddleCenter, Red, Half, Half, new Vector2(-520f, 520f), new Vector2(520f, 700f));
            Txt("Body", panel.transform,
                "This game is HARD.\nLike, really overwhelming.\n\nIf you value your sanity,\nclose it as fast as you can.\n\n...still here?\nOkay. Don't say I didn't warn you :)",
                50, TextAnchor.MiddleCenter, Color.white, Half, Half, new Vector2(-500f, -380f), new Vector2(500f, 480f));
            MakeButton(panel.transform, "I'M NOT SCARED", new Vector2(0f, -600f), () => warningPanel.SetActive(false));
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        public void RefreshMenu()
        {
            if (menuPanel == null) return;
            menuHero.color = HeroKit.Tint(g.SelectedHero);
            var runs = g.Scores;
            var sb = new System.Text.StringBuilder();
            if (runs.Count == 0) sb.Append("No runs yet. Go set the first record!");
            for (int i = 0; i < runs.Count && i < 8; i++)
            {
                var r = runs[i];
                sb.Append(i + 1).Append(".   ").Append(FormatTime(r.time)).Append("    ").Append(r.kills).Append(" kills    Lv ")
                  .Append(r.level).Append("    ").Append(r.bosses).Append('/').Append(g.Enemies.BossTotal).Append(" bosses\n");
            }
            menuScores.text = sb.ToString();
        }

        // ---- minimap: camera view, XP on the ground, pickups, boss landing spot, boss, hero ----
        void DrawMinimap()
        {
            var bg = new Color32(6, 9, 18, 215);
            for (int i = 0; i < mapBuf.Length; i++) mapBuf[i] = bg;

            var cam = g.Cam;
            float vh = cam.orthographicSize, vw = vh * cam.aspect;
            Vector2 cp = cam.transform.position;
            MapRect(cp.x - vw, cp.y - vh, cp.x + vw, cp.y + vh, new Color32(70, 110, 170, 255));

            var xp = g.Xp;
            var xpCol = new Color32(80, 225, 255, 160);
            for (int i = 0; i < xp.Count; i++) MapDot(xp.PosAt(i), 0, xpCol);

            var pk = g.Pickups;
            for (int i = 0; i < pk.Count; i++)
                MapDot(pk.PosAt(i), 2, pk.IsCheese(i) ? new Color32(255, 210, 80, 255) : new Color32(255, 70, 95, 255));

            var en = g.Enemies;
            var bossCol = new Color32(255, 185, 45, 255);
            if (en.BossPointKnown)
            {
                MapRing(en.NextBossPoint, 4f + Mathf.PingPong(Time.unscaledTime * 8f, 3f), bossCol);
                MapDot(en.NextBossPoint, 1, bossCol);
            }
            if (en.BossAlive) MapDot(en.BossPos, 3, bossCol);

            var po = g.Portals;
            for (int i = 0; i < Portals.Count; i++) MapDot(po.PosAt(i), 2, po.MapColor(i));

            Color hc = g.Player.HeroColor;
            MapDot(g.Player.Pos, 2, new Color32((byte)(hc.r * 255f), (byte)(hc.g * 255f), (byte)(hc.b * 255f), 255));

            mapTex.SetPixels32(mapBuf);
            mapTex.Apply(false);
        }

        static int MapX(float x) => Mathf.Clamp((int)((x / Game.ArenaHalfW * 0.5f + 0.5f) * MapPx), 0, MapPx - 1);
        static int MapY(float y) => Mathf.Clamp((int)((y / Game.ArenaHalfH * 0.5f + 0.5f) * MapPx), 0, MapPx - 1);

        void MapPix(int x, int y, Color32 c)
        {
            if (x >= 0 && y >= 0 && x < MapPx && y < MapPx) mapBuf[y * MapPx + x] = c;
        }

        void MapDot(Vector2 p, int r, Color32 c)
        {
            int cx = MapX(p.x), cy = MapY(p.y);
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++) MapPix(x, y, c);
        }

        void MapRing(Vector2 p, float r, Color32 c)
        {
            int cx = MapX(p.x), cy = MapY(p.y);
            for (int k = 0; k < 28; k++)
            {
                float a = k * Mathf.PI * 2f / 28f;
                MapPix(cx + (int)(Mathf.Cos(a) * r), cy + (int)(Mathf.Sin(a) * r), c);
            }
        }

        void MapRect(float x0, float y0, float x1, float y1, Color32 c)
        {
            int ax = MapX(x0), ay = MapY(y0), bx = MapX(x1), by = MapY(y1);
            for (int x = ax; x <= bx; x++) { MapPix(x, ay, c); MapPix(x, by, c); }
            for (int y = ay; y <= by; y++) { MapPix(ax, y, c); MapPix(bx, y, c); }
        }

        Image MakeCooldownPip(string name, Vector2 min, Vector2 max, Sprite icon, Color color, string label)
        {
            var bg = Img(name, safe, new Color(0.05f, 0.06f, 0.11f, 0.85f), new Vector2(1f, 0f), new Vector2(1f, 0f), min, max, Sprites.Circle);
            Img("Icon", bg.transform, color, Vector2.zero, Vector2.one, new Vector2(24f, 24f), new Vector2(-24f, -24f), icon);
            var shade = Img("Cd", bg.transform, new Color(0f, 0f, 0f, 0.7f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Sprites.Circle);
            shade.type = Image.Type.Filled;
            shade.fillMethod = Image.FillMethod.Radial360;
            shade.fillOrigin = (int)Image.Origin360.Top;
            shade.fillClockwise = false;
            shade.fillAmount = 0f;
            Txt("Label", bg.transform, label, 24, TextAnchor.UpperCenter, Color.white, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-20f, -34f), new Vector2(20f, 0f));
            return shade;
        }

        Text MakeSmallButton(Transform parent, string label, Vector2 center, Vector2 size, UnityEngine.Events.UnityAction onClick, Color? color = null)
        {
            var img = Img(label + "Btn", parent, color ?? Cyan, Half, Half, center - size * 0.5f, center + size * 0.5f);
            img.raycastTarget = true;
            img.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
            return Txt("Label", img.transform, label, 44, TextAnchor.MiddleCenter, new Color(0.02f, 0.05f, 0.1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        void MakeButton(Transform parent, string label, Vector2 center, UnityEngine.Events.UnityAction onClick)
        {
            var img = Img(label, parent, Cyan, Half, Half, center + new Vector2(-340f, -95f), center + new Vector2(340f, 95f));
            img.raycastTarget = true;
            img.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
            Txt("Label", img.transform, label, 60, TextAnchor.MiddleCenter, new Color(0.02f, 0.05f, 0.1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        void ApplySafeArea()
        {
            UnityEngine.Rect area = Screen.safeArea;
            if (area == lastSafeArea || Screen.width <= 0 || Screen.height <= 0) return;
            lastSafeArea = area;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }

        public Vector2 ScreenToCanvas(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
            return local;
        }

        static string FormatTime(float t)
        {
            int s = Mathf.FloorToInt(t);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        Image Img(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Sprite sprite = null)
        {
            var img = MakeRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text Txt(string name, Transform parent, string text, int size, TextAnchor align, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var t = MakeRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax).gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
