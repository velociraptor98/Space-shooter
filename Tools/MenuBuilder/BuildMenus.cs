// Builds the menu UI: the MenuButton and OptionsPanel prefabs, the title screen in MainMenu.unity, and the
// pause and game-over menus in Game.unity. Run in the open Editor (after ui_sprites.py and
// build_pixel_font.cs) with:
//   unity command run_script --file Tools/MenuBuilder/BuildMenus.cs --entry MenuBuilder.BuildAll
// Each step replaces what it built before, so it's safe to re-run after tweaking the layout here.
// Layout units are the Pixel Perfect Camera's pixels: the canvas is 640x360, scaled up by whole numbers.
using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MenuBuilder
{
    private const string SpriteDir = "Assets/Sprites/UI/";
    private const string PrefabDir = "Assets/Prefab/UI/";
    private const string ButtonPrefabPath = PrefabDir + "MenuButton.prefab";
    private const string OptionsPrefabPath = PrefabDir + "OptionsPanel.prefab";
    private const string MenuScene = "Assets/Scenes/MainMenu.unity";
    private const string GameScene = "Assets/Scenes/Game.unity";
    private const int TextSize = 7;
    private const int HeadingSize = 14;
    private const int RowHeight = 13;

    // Sweetie 16.
    private static readonly Color Ink = new Color32(26, 28, 44, 255);
    private static readonly Color Plum = new Color32(93, 39, 93, 255);
    private static readonly Color Yellow = new Color32(255, 205, 117, 255);
    private static readonly Color Blue = new Color32(59, 93, 201, 255);
    private static readonly Color Cyan = new Color32(115, 239, 247, 255);
    private static readonly Color White = new Color32(244, 244, 244, 255);
    private static readonly Color Silver = new Color32(148, 176, 194, 255);
    private static readonly Color Slate = new Color32(86, 108, 134, 255);
    private static readonly Color Steel = new Color32(51, 60, 87, 255);
    private static readonly Color Sky = new Color32(65, 166, 246, 255);

    private static Font font;
    private static Sprite pixel, panel, buttonHighlight, segment, cursor, logo;

    public static string BuildAll()
    {
        return BuildPrefabs() + "\n" + BuildTitle() + "\n" + BuildGame();
    }

    public static string BuildPrefabs()
    {
        Load();
        if (!AssetDatabase.IsValidFolder("Assets/Prefab/UI"))
        {
            AssetDatabase.CreateFolder("Assets/Prefab", "UI");
        }
        var button = MenuButton();
        PrefabUtility.SaveAsPrefabAsset(button, ButtonPrefabPath);
        Object.DestroyImmediate(button);
        var options = OptionsPanel();
        PrefabUtility.SaveAsPrefabAsset(options, OptionsPrefabPath);
        Object.DestroyImmediate(options);
        return "Prefabs: " + ButtonPrefabPath + ", " + OptionsPrefabPath;
    }

    public static string BuildTitle()
    {
        Load();
        var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        // The old title screen: a static background image and a single New Game button.
        foreach (var name in new[] { "Canvas", "SpaceBG_Overlay", "MenuCanvas", "Starfield", "Scenery", "SpaceScroller", "MenuShip", "Music" })
        {
            var old = GameObject.Find(name);
            if (old)
            {
                Object.DestroyImmediate(old);
            }
        }
        CopyBackdropFromGame(scene);
        AddMusic(scene);

        var canvas = MenuCanvas();
        var navigator = canvas.GetComponent<MenuNavigator>();
        var title = canvas.gameObject.AddComponent<TitleScreen>();

        var logoImage = AddImage("Logo", canvas, logo, White, new Vector2(0.5f, 1.0f), new Vector2(0, -40), new Vector2(324, 100));
        logoImage.rectTransform.pivot = new Vector2(0.5f, 1.0f);
        Set(logoImage.gameObject.AddComponent<EchoLogo>(), "ringSprite", LoadSprite("EchoLogoRing"));

        var press = Label("PressAnyButton", canvas, "PRESS ANY BUTTON", TextSize, White, TextAnchor.MiddleCenter, Center, new Vector2(0, -50), new Vector2(200, RowHeight));
        press.gameObject.AddComponent<Blink>();

        var main = Panel("MainPanel", canvas, new Vector2(0, -45), new Vector2(120, 5 * RowHeight + 4 * 2), false);
        var list = main.gameObject.AddComponent<VerticalLayoutGroup>();
        list.spacing = 2;
        list.childAlignment = TextAnchor.UpperCenter;
        list.childControlWidth = list.childControlHeight = false;
        list.childForceExpandWidth = list.childForceExpandHeight = false;
        var play = AddButton(main, "START", title.Play, MenuItem.Sound.None);
        AddButton(main, "OPTIONS", title.OpenOptions);
        AddButton(main, "CONTROLS", title.OpenControls);
        AddButton(main, "CREDITS", title.OpenCredits);
        var quit = AddButton(main, "QUIT", title.Quit, MenuItem.Sound.Back);
        Set(main.GetComponent<MenuPanel>(), "firstSelected", play);

        var options = Options(canvas, new Vector2(0, -40));

        var controls = Panel("ControlsPanel", canvas, new Vector2(0, -40), new Vector2(270, 128), true);
        Heading(controls, "CONTROLS");
        float[] columns = { -125, -58, 36 };
        string[][] rows =
        {
            new[] { "", "KEYBOARD", "GAMEPAD" },
            new[] { "MOVE", "WASD/ARROWS", "L-STICK" },
            new[] { "AIM", "MOUSE", "R-STICK" },
            new[] { "FIRE", "L-CLICK", "A / RT" },
            new[] { "FOCUS", "SHIFT", "LB" },
            new[] { "ROLL", "SPACE", "B" },
            new[] { "PAUSE", "ESC / P", "START" },
        };
        for (int r = 0; r < rows.Length; ++r)
        {
            for (int c = 0; c < columns.Length; ++c)
            {
                var color = r == 0 ? Slate : c == 0 ? Cyan : White;
                var cell = Label("Cell", controls, rows[r][c], TextSize, color, TextAnchor.MiddleLeft, Top, new Vector2(columns[c] + 45, -34 - r * 11), new Vector2(90, 10));
                cell.name = (rows[r][0] == "" ? "Header" : rows[r][0]) + " " + c;
            }
        }
        var controlsBack = BackButton(controls, navigator.Back);
        Set(controls.GetComponent<MenuPanel>(), "firstSelected", controlsBack);

        var credits = Panel("CreditsPanel", canvas, new Vector2(0, -40), new Vector2(240, 112), true);
        Heading(credits, "CREDITS");
        string[] lines = { "GAME BY", "KUNAL SINGH", "", "PALETTE", "SWEETIE 16 BY GRAFXKID" };
        for (int i = 0; i < lines.Length; ++i)
        {
            if (lines[i] != "")
            {
                Label(lines[i], credits, lines[i], TextSize, i % 3 == 0 ? Slate : White, TextAnchor.MiddleCenter, Top, new Vector2(0, -36 - i * 10), new Vector2(220, 10));
            }
        }
        var creditsBack = BackButton(credits, navigator.Back);
        Set(credits.GetComponent<MenuPanel>(), "firstSelected", creditsBack);

        var best = Label("BestScore", canvas, "BEST 000000", TextSize, Slate, TextAnchor.LowerLeft, Vector2.zero, new Vector2(8, 8), new Vector2(120, 10));
        best.rectTransform.pivot = Vector2.zero;
        var madeBy = Label("MadeBy", canvas, "MADE BY KUNAL SINGH", TextSize, Slate, TextAnchor.LowerRight, Vector2.right, new Vector2(-8, 8), new Vector2(160, 10));
        madeBy.rectTransform.pivot = Vector2.right;

        var fader = Fader(canvas);
        Set(title, "navigator", navigator);
        Set(title, "fader", fader);
        Set(title, "pressAnyButton", press.gameObject);
        Set(title, "mainPanel", main.GetComponent<MenuPanel>());
        Set(title, "optionsPanel", options);
        Set(title, "controlsPanel", controls.GetComponent<MenuPanel>());
        Set(title, "creditsPanel", credits.GetComponent<MenuPanel>());
        Set(title, "quitButton", quit.gameObject);
        Set(title, "bestScore", best);
        Set(title, "startClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/Start.wav"));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Title screen built in " + MenuScene;
    }

    public static string BuildGame()
    {
        Load();
        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        // Replaced by the game-over menu.
        foreach (var name in new[] { "GameOver_Text", "Reset_Text", "MenuCanvas" })
        {
            var old = GameObject.Find(name);
            if (old)
            {
                Object.DestroyImmediate(old);
            }
        }
        var music = GameObject.Find("AudioManager/Background");
        if (music && !music.GetComponent<MusicVolume>())
        {
            music.AddComponent<MusicVolume>();
        }

        var canvas = MenuCanvas();
        var navigator = canvas.GetComponent<MenuNavigator>();
        var pauseMenu = canvas.gameObject.AddComponent<PauseMenu>();
        var gameOverMenu = canvas.gameObject.AddComponent<GameOverMenu>();

        var backdrop = AddImage("Backdrop", canvas, pixel, new Color(Ink.r, Ink.g, Ink.b, 0.6f), Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(backdrop.rectTransform);
        backdrop.raycastTarget = true;
        backdrop.gameObject.SetActive(false);

        var pause = Panel("PausePanel", canvas, Vector2.zero, new Vector2(150, 106), true);
        Heading(pause, "PAUSED");
        var pauseList = ButtonList(pause, 4);
        var resume = AddButton(pauseList, "RESUME", pauseMenu.Resume, MenuItem.Sound.None);
        AddButton(pauseList, "RESTART", pauseMenu.Restart);
        AddButton(pauseList, "OPTIONS", pauseMenu.OpenOptions);
        AddButton(pauseList, "MAIN MENU", pauseMenu.MainMenu);
        Set(pause.GetComponent<MenuPanel>(), "firstSelected", resume);

        var options = Options(canvas, Vector2.zero);

        var over = Panel("GameOverPanel", canvas, Vector2.zero, new Vector2(170, 118), true);
        Heading(over, "GAME OVER");
        var score = Label("Score", over, "SCORE 000000", TextSize, White, TextAnchor.MiddleCenter, Top, new Vector2(0, -38), new Vector2(150, 10));
        var best = Label("Best", over, "BEST  000000", TextSize, Silver, TextAnchor.MiddleCenter, Top, new Vector2(0, -49), new Vector2(150, 10));
        var newBest = Label("NewBest", over, "NEW BEST!", TextSize, Yellow, TextAnchor.MiddleCenter, Top, new Vector2(0, -60), new Vector2(150, 10));
        newBest.gameObject.AddComponent<Blink>();
        var overList = ButtonList(over, 2);
        var retry = AddButton(overList, "RETRY", gameOverMenu.Retry);
        AddButton(overList, "MAIN MENU", gameOverMenu.MainMenu);
        Set(over.GetComponent<MenuPanel>(), "firstSelected", retry);

        var fader = Fader(canvas);
        Set(pauseMenu, "navigator", navigator);
        Set(pauseMenu, "fader", fader);
        Set(pauseMenu, "gameManager", Object.FindAnyObjectByType<GameManager>());
        Set(pauseMenu, "pausePanel", pause.GetComponent<MenuPanel>());
        Set(pauseMenu, "optionsPanel", options);
        Set(pauseMenu, "backdrop", backdrop.gameObject);
        Set(gameOverMenu, "navigator", navigator);
        Set(gameOverMenu, "fader", fader);
        Set(gameOverMenu, "panel", over.GetComponent<MenuPanel>());
        Set(gameOverMenu, "backdrop", backdrop.gameObject);
        Set(gameOverMenu, "score", score);
        Set(gameOverMenu, "best", best);
        Set(gameOverMenu, "newBest", newBest.gameObject);
        // The death sequence ends on this menu; it's rebuilt here, so point the sequence at the new one.
        var deathSequence = Object.FindAnyObjectByType<DeathSequence>();
        if (deathSequence)
        {
            Set(deathSequence, "gameOverMenu", gameOverMenu);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Pause and game-over menus built in " + GameScene;
    }

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 Top = new Vector2(0.5f, 1.0f);
    private static readonly Vector2 Bottom = new Vector2(0.5f, 0.0f);

    private static void Load()
    {
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PixelFont.fontsettings");
        pixel = LoadSprite("Pixel");
        panel = LoadSprite("Panel");
        buttonHighlight = LoadSprite("ButtonSelected");
        segment = LoadSprite("BarSegment");
        cursor = LoadSprite("Cursor");
        logo = LoadSprite("EchoLogo");
    }

    private static Sprite LoadSprite(string name)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
        if (!sprite)
        {
            throw new Exception("Missing sprite " + name + "; run Tools/RetroArt/ui_sprites.py and build_pixel_font.cs first");
        }
        return sprite;
    }

    // --- Scene pieces ---------------------------------------------------------------------------------------

    private static RectTransform MenuCanvas()
    {
        var go = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(AudioSource));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        // Above the in-game HUD.
        canvas.sortingOrder = 10;
        // UI sprites are imported at one pixel per unit, so sliced borders and tiles stay one texel per pixel.
        go.GetComponent<CanvasScaler>().referencePixelsPerUnit = 1;
        go.AddComponent<PixelCanvasScaler>();
        go.GetComponent<AudioSource>().playOnAwake = false;
        var navigator = go.AddComponent<MenuNavigator>();
        Set(navigator, "moveClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/Move.wav"));
        Set(navigator, "confirmClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/Confirm.wav"));
        Set(navigator, "backClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/Back.wav"));
        return (RectTransform)go.transform;
    }

    private static ScreenFader Fader(RectTransform canvas)
    {
        var cover = AddImage("Fader", canvas, pixel, Ink, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(cover.rectTransform);
        cover.raycastTarget = true;
        var fader = cover.gameObject.AddComponent<ScreenFader>();
        Set(fader, "cover", cover);
        return fader;
    }

    // The living background from the game: star layers, drifting nebulae and planets, and the scroller that
    // drives them, cruising gently. Plus the player's ship idling on screen.
    private static void CopyBackdropFromGame(Scene menu)
    {
        var game = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Additive);
        try
        {
            foreach (var name in new[] { "Starfield", "Scenery", "SpaceScroller" })
            {
                var copy = Object.Instantiate(Find(game, name));
                copy.name = name;
                SceneManager.MoveGameObjectToScene(copy, menu);
                if (name == "SpaceScroller")
                {
                    // No ship in the menu, so the scroller runs at its drift speed; make that a steady cruise.
                    Set(copy.GetComponent<SpaceScroller>(), "player", null);
                    Set(copy.GetComponent<SpaceScroller>(), "driftSpeed", 3.0f);
                }
            }
            var player = Find(game, "Player");
            var ship = new GameObject("MenuShip");
            SceneManager.MoveGameObjectToScene(ship, menu);
            ship.transform.position = new Vector3(12.5f, -4.0f, 0.0f);
            ship.transform.localScale = player.transform.localScale;
            foreach (var part in new[] { "Body", "Exhaust" })
            {
                var copy = Object.Instantiate(player.transform.Find(part).gameObject, ship.transform, false);
                copy.name = part;
            }
            ship.AddComponent<MenuShip>();
        }
        finally
        {
            EditorSceneManager.CloseScene(game, true);
        }
    }

    private static void AddMusic(Scene menu)
    {
        var go = new GameObject("Music", typeof(AudioSource));
        SceneManager.MoveGameObjectToScene(go, menu);
        var source = go.GetComponent<AudioSource>();
        source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/2D Galaxy Assets/Game/Audio/music_background.wav");
        source.loop = true;
        source.playOnAwake = true;
        go.AddComponent<MusicVolume>();
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }
        throw new Exception("No " + name + " in " + scene.path);
    }

    // --- Menu pieces ----------------------------------------------------------------------------------------

    private static RectTransform Panel(string name, RectTransform parent, Vector2 position, Vector2 size, bool framed)
    {
        var rect = Rect(name, parent, Center, position, size);
        if (framed)
        {
            var frame = rect.gameObject.AddComponent<Image>();
            frame.sprite = panel;
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.raycastTarget = false;
        }
        rect.gameObject.AddComponent<CanvasGroup>();
        rect.gameObject.AddComponent<MenuPanel>();
        return rect;
    }

    // A title across the top of a framed panel, with the logo's two-colour echo behind it.
    private static void Heading(RectTransform panelRect, string text)
    {
        var heading = Label("Heading", panelRect, text, HeadingSize, White, TextAnchor.MiddleCenter, Top, new Vector2(0, -16), new Vector2(panelRect.sizeDelta.x, 16));
        Echo(heading, Blue, new Vector2(1, -1));
        Echo(heading, Plum, new Vector2(2, -2));
    }

    // A hard one-colour copy of the text, offset by whole pixels.
    private static void Echo(Text text, Color color, Vector2 offset)
    {
        var shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = offset;
        shadow.useGraphicAlpha = true;
    }

    // A column of buttons along the bottom of a framed panel.
    private static RectTransform ButtonList(RectTransform panelRect, int count)
    {
        var rect = Rect("Buttons", panelRect, Bottom, new Vector2(0, 8), new Vector2(120, count * RowHeight + (count - 1) * 2));
        rect.pivot = Bottom;
        rect.anchoredPosition = new Vector2(0, 8);
        var list = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        list.spacing = 2;
        list.childAlignment = TextAnchor.LowerCenter;
        list.childControlWidth = list.childControlHeight = false;
        list.childForceExpandWidth = list.childForceExpandHeight = false;
        return rect;
    }

    private static Button BackButton(RectTransform panelRect, UnityAction onClick)
    {
        var list = ButtonList(panelRect, 1);
        return AddButton(list, "BACK", onClick, MenuItem.Sound.Back);
    }

    private static Button AddButton(RectTransform parent, string text, UnityAction onClick, MenuItem.Sound sound = MenuItem.Sound.Confirm)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = text.Replace(" ", "") + "Button";
        go.GetComponentInChildren<Text>().text = text;
        var button = go.GetComponent<UnityEngine.UI.Button>();
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        Set(go.GetComponent<MenuItem>(), "clickSound", (int)sound);
        return button;
    }

    private static MenuPanel Options(RectTransform canvas, Vector2 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OptionsPrefabPath);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
        ((RectTransform)go.transform).anchoredPosition = position;
        return go.GetComponent<MenuPanel>();
    }

    // --- Prefabs --------------------------------------------------------------------------------------------

    // A menu entry: an invisible hit area, the highlight bar and cursor shown when selected, and its label.
    private static GameObject MenuButton()
    {
        var rect = Rect("MenuButton", null, Center, Vector2.zero, new Vector2(120, RowHeight));
        var hit = rect.gameObject.AddComponent<Image>();
        hit.sprite = pixel;
        hit.color = Color.clear;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        Configure(button, hit);
        var highlight = Highlight(rect);
        var label = Label("Label", rect, "BUTTON", TextSize, Silver, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(120, RowHeight));
        // Keeps the labels readable where they cross a bright nebula.
        Echo(label, Ink, new Vector2(1, -1));
        var pointer = AddCursor(rect, Center, Vector2.zero);
        var item = rect.gameObject.AddComponent<MenuItem>();
        Set(item, "label", label);
        Set(item, "highlight", highlight);
        Set(item, "cursor", pointer);
        return rect.gameObject;
    }

    // Volume sliders and ON/OFF toggles as full-width rows: label on the left, value on the right.
    private static GameObject OptionsPanel()
    {
        var rect = Panel("OptionsPanel", null, Vector2.zero, new Vector2(240, 114), true);
        Heading(rect, "OPTIONS");
        var master = SliderRow(rect, "MASTER VOLUME", 0);
        var music = SliderRow(rect, "MUSIC VOLUME", 1);
        var shake = ToggleRow(rect, "SCREEN SHAKE", 2, out var shakeValue);
        var fullscreen = ToggleRow(rect, "FULLSCREEN", 3, out var fullscreenValue);
        var options = rect.gameObject.AddComponent<OptionsMenu>();
        Set(options, "masterVolume", master);
        Set(options, "musicVolume", music);
        Set(options, "screenShake", shake);
        Set(options, "screenShakeValue", shakeValue);
        Set(options, "fullscreen", fullscreen);
        Set(options, "fullscreenValue", fullscreenValue);
        BackButton(rect, options.Back);
        Set(rect.GetComponent<MenuPanel>(), "firstSelected", master);
        return rect.gameObject;
    }

    private static RectTransform Row(RectTransform parent, string text, int index, out Image highlight, out Text label, out Image pointer)
    {
        var row = Rect(text.Replace(" ", ""), parent, Top, new Vector2(0, -34 - index * (RowHeight + 2)), new Vector2(210, RowHeight));
        highlight = Highlight(row);
        pointer = AddCursor(row, new Vector2(0.0f, 0.5f), new Vector2(8, 0));
        label = Label("Label", row, text, TextSize, Silver, TextAnchor.MiddleLeft, new Vector2(0.0f, 0.5f), new Vector2(81, 0), new Vector2(130, RowHeight));
        Echo(label, Ink, new Vector2(1, -1));
        return row;
    }

    private static Slider SliderRow(RectTransform parent, string text, int index)
    {
        var row = Row(parent, text, index, out var highlight, out var label, out var pointer);
        // Only the bar takes clicks: a slider sets its value from where the pointer lands, and a click on the
        // label would read as "all the way down".
        var hit = AddImage("Hit", row, pixel, Color.clear, new Vector2(1.0f, 0.5f), new Vector2(-30, 0), new Vector2(52, RowHeight));
        hit.raycastTarget = true;
        var track = AddImage("Track", row, segment, Steel, new Vector2(1.0f, 0.5f), new Vector2(-30, 0), new Vector2(40, 6));
        track.type = UnityEngine.UI.Image.Type.Tiled;
        var fillArea = Rect("FillArea", track.rectTransform, Center, Vector2.zero, Vector2.zero);
        Stretch(fillArea);
        var fill = AddImage("Fill", fillArea, segment, Sky, Vector2.zero, Vector2.zero, Vector2.zero);
        fill.type = UnityEngine.UI.Image.Type.Tiled;
        Stretch(fill.rectTransform);
        var slider = row.gameObject.AddComponent<Slider>();
        Configure(slider, hit);
        slider.fillRect = fill.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 10;
        slider.wholeNumbers = true;
        slider.value = 8;
        Item(row.gameObject, label, highlight, pointer);
        return slider;
    }

    private static Toggle ToggleRow(RectTransform parent, string text, int index, out Text value)
    {
        var row = Row(parent, text, index, out var highlight, out var label, out var pointer);
        var hit = row.gameObject.AddComponent<Image>();
        hit.sprite = pixel;
        hit.color = Color.clear;
        value = Label("Value", row, "ON", TextSize, Cyan, TextAnchor.MiddleCenter, new Vector2(1.0f, 0.5f), new Vector2(-30, 0), new Vector2(40, RowHeight));
        var toggle = row.gameObject.AddComponent<Toggle>();
        Configure(toggle, hit);
        toggle.graphic = null;
        Item(row.gameObject, label, highlight, pointer);
        return toggle;
    }

    private static void Item(GameObject row, Text label, Image highlight, Image pointer)
    {
        var item = row.AddComponent<MenuItem>();
        Set(item, "label", label);
        Set(item, "highlight", highlight);
        Set(item, "cursor", pointer);
        Set(item, "cursorFollowsLabel", false);
        // The options menu plays its own blip when a value changes.
        Set(item, "clickSound", (int)MenuItem.Sound.None);
    }

    private static void Configure(Selectable selectable, Graphic target)
    {
        selectable.transition = Selectable.Transition.None;
        selectable.targetGraphic = target;
        selectable.navigation = new Navigation { mode = Navigation.Mode.Vertical };
    }

    private static Image Highlight(RectTransform parent)
    {
        var highlight = AddImage("Highlight", parent, buttonHighlight, White, Center, Vector2.zero, Vector2.zero);
        highlight.type = UnityEngine.UI.Image.Type.Sliced;
        Stretch(highlight.rectTransform);
        return highlight;
    }

    private static Image AddCursor(RectTransform parent, Vector2 anchor, Vector2 position)
    {
        return AddImage("Cursor", parent, cursor, Yellow, anchor, position, new Vector2(5, 7));
    }

    // --- Primitives -----------------------------------------------------------------------------------------

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        if (parent)
        {
            rect.SetParent(parent, false);
        }
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = Center;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Image AddImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var image = Rect(name, parent, anchor, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 rectSize)
    {
        var label = Rect(name, parent, anchor, position, rectSize).gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.text = text;
        label.color = color;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.supportRichText = false;
        label.raycastTarget = false;
        return label;
    }

    private static void Set(Object target, string field, object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null)
        {
            throw new Exception(target.GetType().Name + " has no serialized field " + field);
        }
        switch (value)
        {
            case null: property.objectReferenceValue = null; break;
            case Object reference: property.objectReferenceValue = reference; break;
            case bool flag: property.boolValue = flag; break;
            case int number when property.propertyType == SerializedPropertyType.Enum: property.enumValueIndex = number; break;
            case int number: property.intValue = number; break;
            case float number: property.floatValue = number; break;
            default: throw new Exception("Unsupported value for " + field);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
