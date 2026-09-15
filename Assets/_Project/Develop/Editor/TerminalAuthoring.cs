using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using TMPro;
using Assets._Project.Develop.Runtime.Infrastructure.EntryPoint;
using Assets._Project.Develop.Runtime.Gameplay.Configs;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Presentation;
using Assets._Project.Develop.Runtime.Meta.Infrastructure;
using Assets._Project.Develop.Runtime.Meta.Presentation;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Assets._Project.Develop.Runtime.Utilities.LoadingScreen;
using Assets._Project.Develop.Runtime.UI;

namespace Assets._Project.Develop.Editor
{
    public static class TerminalAuthoring
    {
        private const string Root = "Assets/_Project/";
        private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        private const string RussianFontPath = Root + "Fonts/LiberationSans Cyrillic SDF.asset";
        private const string MainMenuScenePath = Root + "Scenes/MainMenu.unity";
        private const string GameplayScenePath = Root + "Scenes/Gameplay.unity";
        private const string ScreenPrefabFolder = Root + "Prefabs/UI";
        private const string MainMenuScreenPrefabPath = ScreenPrefabFolder + "/MainMenuScreen.prefab";
        private const string GameplayScreenPrefabPath = ScreenPrefabFolder + "/GameplayScreen.prefab";
        private const string LoadingPrefabPath = Root + "Resources/Utilities/StandardLoadingScreen.prefab";

        private static TMP_FontAsset _font;
        private static readonly Color Amber = TerminalView.Amber;
        private static readonly Color Ivory = TerminalView.Ivory;
        private static readonly Color Muted = new Color(.62f, .54f, .4f);
        private static readonly Color LoadingBackground = new Color(.055f, .05f, .035f);
        private static readonly Dictionary<string, string> RussianLabels = new Dictionary<string, string>
        {
            { "Loading", "ИНИЦИАЛИЗАЦИЯ ТЕРМИНАЛА" },
            { "Series", "ШИФРОВАЛЬНЫЙ АППАРАТ\nСЕРИЯ 01" },
            { "Status", "ЛОКАЛЬНОЕ СОЕДИНЕНИЕ\nКЛАВИАТУРА В СЕТИ" },
            { "Title", "ТЕРМИНАЛ ШИФРОВАНИЯ" },
            { "Footer", "ТЕРМИНАЛ ШИФРОВАНИЯ   /   ПОЛЕВАЯ МОДЕЛЬ А" },
            { "Protocol", "ВЫБЕРИТЕ ПРОТОКОЛ" },
            { "Instructions", "ПОВТОРИТЕ КОД. ВАЖЕН КАЖДЫЙ СИМВОЛ." },
            { "InputHint", "НАЖМИТЕ 1 ИЛИ 2 ДЛЯ ПОДКЛЮЧЕНИЯ" },
            { "Progress", "0 / 6  ПРОВЕРЕНО" },
            { "Outcome", "" },
            { "Hint", "ВВЕДИТЕ ПОСЛЕДОВАТЕЛЬНОСТЬ" },
        };

        public static void ImportTextResources()
        {
            string package = Path.Combine(EditorApplication.applicationContentsPath,
                "Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(package, false);
        }

        [MenuItem("Cipher Terminal/Apply Russian UI Localization")]
        public static void ApplyRussianLocalization()
        {
            EnsureSafeToPatchAuthoredAssets();
            TMP_FontAsset russianFont = GetOrCreateRussianFont();
            PatchPrefab(MainMenuScreenPrefabPath, russianFont, true);
            PatchPrefab(GameplayScreenPrefabPath, russianFont, false);
            PatchPrefab(LoadingPrefabPath, russianFont, false);

            if (PlayerSettings.productName == "Cipher Terminal")
                PlayerSettings.productName = "Терминал шифрования";

            AssetDatabase.SaveAssets();

            Debug.Log("Russian UI localization applied without rebuilding authored assets");
        }

        [MenuItem("Cipher Terminal/Migrate Scene Screens To Prefabs")]
        public static void MigrateSceneScreensToPrefabs()
        {
            EnsureSafeToPatchAuthoredAssets();
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EnsureScreenPrefabFolder();
                MigrateSceneScreen(MainMenuScenePath, MainMenuScreenPrefabPath);
                MigrateSceneScreen(GameplayScenePath, GameplayScreenPrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }

            Debug.Log("Main menu and gameplay screens are connected prefab instances");
        }

        [MenuItem("Cipher Terminal/Patch Loading Presentation Camera")]
        public static void PatchLoadingPresentationCamera()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before patching the loading prefab");

            GameObject root = PrefabUtility.LoadPrefabContents(LoadingPrefabPath);

            try
            {
                EnsureLoadingCamera(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, LoadingPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Loading presentation camera patched without rebuilding the loading prefab");
        }

        [MenuItem("Cipher Terminal/Author Initial Scenes")]
        public static void Create()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before authoring");

            if (EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene first");

            EnsureInitialScreenPrefabPathsAreAvailable();
            _font = GetOrCreateRussianFont();

            Directory.CreateDirectory(Root + "Scenes");
            Directory.CreateDirectory(ScreenPrefabFolder);
            Directory.CreateDirectory(Root + "Resources/Utilities");
            Directory.CreateDirectory(Root + "Resources/Configs");
            AssetDatabase.Refresh();

            string configPath = Root + "Resources/Configs/SequenceConfig.asset";

            if (AssetDatabase.LoadAssetAtPath<SequenceConfig>(configPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SequenceConfig>(), configPath);

            NewScene();

            RectTransform cover = Canvas("StandardLoadingScreen", new Vector2(1920, 1080), 1000);
            Image bg = Box(cover, "Background", Vector2.zero, new Vector2(4000, 4000), LoadingBackground);
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.offsetMin = Vector2.zero;
            bg.rectTransform.offsetMax = Vector2.zero;
            bg.raycastTarget = true;

            Label(cover, "Loading", "ИНИЦИАЛИЗАЦИЯ ТЕРМИНАЛА", 0, 0, 920, 100, 28, Amber, 2);
            EnsureLoadingCamera(cover);
            cover.gameObject.AddComponent<StandardLoadingScreen>();
            PrefabUtility.SaveAsPrefabAsset(cover.gameObject, Root + "Resources/Utilities/StandardLoadingScreen.prefab");
            UnityEngine.Object.DestroyImmediate(cover.gameObject);
            Save("Empty");

            NewScene();
            Camera();
            new GameObject("GameEntryPoint").AddComponent<GameEntryPoint>();
            Save("GameEntryPoint");

            CreateMenu();
            CreateGameplay();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Root + "Scenes/GameEntryPoint.unity", true),
                new EditorBuildSettingsScene(Root + "Scenes/Empty.unity", true),
                new EditorBuildSettingsScene(Root + "Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene(Root + "Scenes/Gameplay.unity", true)
            };

            PlayerSettings.productName = "Терминал шифрования";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Root + "Scenes/GameEntryPoint.unity");
            Debug.Log("Cipher Terminal scenes and prefabs authored");
        }

        private static void CreateMenu()
        {
            NewScene();
            Camera();

            RectTransform ui = Common("ВЫБЕРИТЕ ПРОТОКОЛ", out TerminalView view, out TMP_Text protocol, out Image pulse);
            var digits = ModeCard(ui, "Digits", -290, "1", "ЦИФРЫ", "ЧИСЛОВОЙ КАНАЛ  /  0–9");
            var letters = ModeCard(ui, "Letters", 290, "2", "БУКВЫ", "БУКВЕННЫЙ КАНАЛ  /  A–Z");
            Label(ui, "Instructions", "ПОВТОРИТЕ КОД. ВАЖЕН КАЖДЫЙ СИМВОЛ.", 0, -242, 1300, 50, 18, Ivory, 1);
            Label(ui, "InputHint", "НАЖМИТЕ 1 ИЛИ 2 ДЛЯ ПОДКЛЮЧЕНИЯ", 0, -310, 1300, 50, 18, Amber, 2);
            Set(view, "_digitsButton", digits);
            Set(view, "_lettersButton", letters);
            Set(view, "_pulse", pulse);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<MainMenuBootstrap>();
            var controller = go.AddComponent<MainMenuController>();
            var keyboard = go.AddComponent<TerminalKeyboard>();
            Set(bootstrap, "_controller", controller);
            Set(controller, "_view", view);
            Set(controller, "_keyboard", keyboard);

            view.SetMenuEnabled(false);
            SaveScreenPrefabAndConnect((RectTransform)ui.parent, MainMenuScreenPrefabPath);
            Save("MainMenu");
        }

        private static void CreateGameplay()
        {
            NewScene();
            Camera();

            RectTransform ui = Common("ЦИФРОВОЙ ПРОТОКОЛ", out TerminalView view, out TMP_Text protocol, out Image pulse);
            RectTransform row = Rect(ui, "Sequence", 0, -5, 1320, 220);
            TerminalCell[] cells = new TerminalCell[12];

            for (int i = 0; i < cells.Length; i++)
            {
                Image border = Box(row, "Symbol " + (i + 1), new Vector2(0, 0), new Vector2(190, 215), Muted);
                Image fill = Box(border.rectTransform, "Fill", Vector2.zero, new Vector2(186, 211), new Color(.055f, .05f, .035f));
                // Fill follows resized cell so configurable sequence length remains supported.
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.offsetMin = new Vector2(2, 2);
                fill.rectTransform.offsetMax = new Vector2(-2, -2);

                TMP_Text symbol = Label(border.transform, "Symbol", "0", 0, 12, 170, 140, 94, Ivory);
                TMP_Text marker = Label(border.transform, "State", "", 0, -72, 180, 32, 13, Amber, 1);
                cells[i] = border.gameObject.AddComponent<TerminalCell>();
                Set(cells[i], "_symbol", symbol);
                Set(cells[i], "_marker", marker);
                Set(cells[i], "_border", border);
                Set(cells[i], "_fill", fill);
            }

            Image cursor = Box(row, "Cursor", new Vector2(0, -92), new Vector2(40, 2), Amber);
            Image sweep = Box(row, "CompletionSweep", Vector2.zero, new Vector2(4, 215), new Color(1, .69f, .23f, .35f));
            sweep.gameObject.SetActive(false);
            Set(view, "_sweep", sweep);
            TMP_Text progress = Label(ui, "Progress", "0 / 6  ПРОВЕРЕНО", 0, -177, 1300, 42, 21, Amber, 3);
            TMP_Text outcome = Label(ui, "Outcome", "", 0, -246, 1300, 60, 30, Amber, 7);
            TMP_Text hint = Label(ui, "Hint", "ВВЕДИТЕ ПОСЛЕДОВАТЕЛЬНОСТЬ", 0, -316, 1300, 45, 19, Ivory, 2);
            Set(view, "_protocol", protocol);
            Set(view, "_progress", progress);
            Set(view, "_outcome", outcome);
            Set(view, "_hint", hint);
            Set(view, "_cursor", cursor);
            Set(view, "_pulse", pulse);
            Set(view, "_cellRow", row);

            var serializedView = new SerializedObject(view);
            var cellProperty = serializedView.FindProperty("_cells");
            cellProperty.arraySize = cells.Length;

            for (int i = 0; i < cells.Length; i++)
                cellProperty.GetArrayElementAtIndex(i).objectReferenceValue = cells[i];

            serializedView.ApplyModifiedPropertiesWithoutUndo();
            view.ShowSequence(new Runtime.Gameplay.Sequence.SequenceSession("482916"), Runtime.Gameplay.Sequence.SequenceMode.Digits);

            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameplayBootstrap>();
            var controller = go.AddComponent<GameplayController>();
            var keyboard = go.AddComponent<TerminalKeyboard>();
            Set(bootstrap, "_controller", controller);
            Set(controller, "_view", view);
            Set(controller, "_keyboard", keyboard);

            SaveScreenPrefabAndConnect((RectTransform)ui.parent, GameplayScreenPrefabPath);
            Save("Gameplay");
        }

        private static RectTransform Common(string mode, out TerminalView view, out TMP_Text protocol, out Image pulse)
        {
            RectTransform canvas = Canvas("TerminalCanvas", new Vector2(1920, 1080), 0);
            var background = new GameObject("Background", typeof(RectTransform), typeof(RawImage));
            background.transform.SetParent(canvas, false);

            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var raw = background.GetComponent<RawImage>();
            raw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Art/Textures/TerminalBackground.png");
            raw.raycastTarget = false;
            RectTransform ui = Rect(canvas, "Composition", 0, 0, 1600, 900);
            // Preserve the authored local coordinates while fitting the 1920×1080 design canvas.
            ui.localScale = Vector3.one * 1.2f;
            view = ui.gameObject.AddComponent<TerminalView>();

            Line(ui, "Frame Top", -770, 421, 770, 421, Muted);
            Line(ui, "Frame Bottom", -770, -421, 770, -421, Muted);
            Line(ui, "Frame Left", -770, -421, -770, 421, Muted);
            Line(ui, "Frame Right", 770, -421, 770, 421, Muted);
            foreach (int side in new[] { -1, 1 })
            {
                Line(ui, "Registration", side * 733 - 9, 190, side * 733 + 9, 190, Muted);
                Line(ui, "Registration", side * 733, 181, side * 733, 199, Muted);
                Line(ui, "Registration", side * 733 - 9, -190, side * 733 + 9, -190, Muted);
                Line(ui, "Registration", side * 733, -199, side * 733, -181, Muted);
            }

            Label(ui, "Series", "ШИФРОВАЛЬНЫЙ АППАРАТ\nСЕРИЯ 01", -564, 365, 360, 60, 12, Muted, 1).alignment = TextAlignmentOptions.Left;
            Label(ui, "Status", "ЛОКАЛЬНОЕ СОЕДИНЕНИЕ\nКЛАВИАТУРА В СЕТИ", 564, 365, 360, 60, 12, Muted, 1).alignment = TextAlignmentOptions.Right;
            Seal(ui, new Vector2(0, 329));
            Label(ui, "Title", "ТЕРМИНАЛ ШИФРОВАНИЯ", 0, 244, 1370, 85, 38, Amber, 5);
            protocol = Label(ui, "Protocol", mode, 0, 176, 1100, 45, 21, Ivory, 3);
            Line(ui, "Divider", -330, 134, 330, 134, new Color(.4f, .3f, .15f));
            Label(ui, "Footer", "ТЕРМИНАЛ ШИФРОВАНИЯ   /   ПОЛЕВАЯ МОДЕЛЬ А", 0, -386, 1300, 34, 12, Muted, 1);
            pulse = Box(ui, "FeedbackPulse", Vector2.zero, new Vector2(1400, 630), Color.clear);

            return ui;
        }

        private static Button ModeCard(Transform ui, string name, float x, string key, string title, string subtitle)
        {
            Image image = Box(ui, name, new Vector2(x, -18), new Vector2(530, 267), new Color(.28f, .2f, .08f));
            Box(image.transform, "Inset", Vector2.zero, new Vector2(528, 265), new Color(.055f, .05f, .035f, .95f));

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            image.raycastTarget = true;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(.7f, .63f, .4f);
            colors.highlightedColor = new Color(1, .86f, .48f);
            colors.pressedColor = new Color(1, .72f, .2f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(.5f, .46f, .35f);
            colors.colorMultiplier = 2;
            colors.fadeDuration = .12f;
            button.colors = colors;

            Label(image.transform, "Key", key, 0, 66, 440, 93, 70, Amber);
            Label(image.transform, "Name", title, 0, -21, 470, 65, 27, Ivory, 4);
            Label(image.transform, "Description", subtitle, 0, -87, 490, 38, 13, Muted, 1);

            return button;
        }

        private static TMP_FontAsset GetOrCreateRussianFont()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RussianFontPath);
            if (existing != null)
            {
                if (!existing.HasCharacters(RussianCharacterSet(), out List<char> missing))
                    throw new InvalidOperationException("Existing Russian font asset is missing glyphs: " + new string(missing.ToArray()));

                return existing;
            }

            Font source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

            if (source == null)
                throw new InvalidOperationException("TMP LiberationSans source font is missing: " + SourceFontPath);

            Directory.CreateDirectory(Root + "Fonts");
            AssetDatabase.Refresh();

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, false);

            if (font == null)
                throw new InvalidOperationException("Could not create TMP font asset from " + SourceFontPath);

            if (!font.TryAddCharacters(RussianCharacterSet(), out string missingCharacters, true))
            {
                UnityEngine.Object.DestroyImmediate(font);
                throw new InvalidOperationException("LiberationSans does not contain required glyphs: " + missingCharacters);
            }

            font.name = "LiberationSans Cyrillic SDF";
            font.atlasTextures[0].name = "LiberationSans Cyrillic Atlas";
            font.material.name = "LiberationSans Cyrillic Material";
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, RussianFontPath);
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();

            return font;
        }

        private static string RussianCharacterSet()
        {
            var characters = new System.Text.StringBuilder();

            for (char character = ' '; character <= '~'; character++)
                characters.Append(character);

            characters.Append('Ё');

            for (char character = 'А'; character <= 'я'; character++)
                characters.Append(character);

            characters.Append("ё–");

            return characters.ToString();
        }

        private static void EnsureSafeToPatchAuthoredAssets()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before applying localization");

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scene changes before applying localization");
            }
        }

        private static void MigrateSceneScreen(string scenePath, string prefabPath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject screen = FindTerminalCanvasRoot(scene, scenePath);
            string connectedPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(screen);
            UnityEngine.Object existingAsset = AssetDatabase.LoadMainAssetAtPath(prefabPath);

            if (existingAsset != null)
            {
                if (connectedPrefabPath == prefabPath && PrefabUtility.GetNearestPrefabInstanceRoot(screen) == screen)
                    return;

                throw new InvalidOperationException("Refusing to overwrite existing screen prefab: " + prefabPath);
            }

            if (!string.IsNullOrEmpty(connectedPrefabPath))
                throw new InvalidOperationException("TerminalCanvas is already connected to a different prefab: " + connectedPrefabPath);

            GameObject connected = PrefabUtility.SaveAsPrefabAssetAndConnect(
                screen, prefabPath, InteractionMode.AutomatedAction, out bool success);
            if (!success || connected == null || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(connected) != prefabPath)
                throw new InvalidOperationException("Could not create and connect screen prefab: " + prefabPath);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save migrated scene: " + scenePath);
        }

        private static GameObject FindTerminalCanvasRoot(Scene scene, string scenePath)
        {
            GameObject match = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "TerminalCanvas")
                    continue;

                if (match != null)
                    throw new InvalidOperationException("Scene has more than one TerminalCanvas root: " + scenePath);

                match = root;
            }

            if (match == null)
                throw new InvalidOperationException("Scene is missing the TerminalCanvas root: " + scenePath);

            if (match.GetComponent<Canvas>() == null || match.GetComponentInChildren<TerminalView>(true) == null)
                throw new InvalidOperationException("TerminalCanvas does not contain the expected screen components: " + scenePath);

            return match;
        }

        private static void SaveScreenPrefabAndConnect(RectTransform screen, string prefabPath)
        {
            if (screen.name != "TerminalCanvas" || screen.GetComponent<Canvas>() == null)
                throw new InvalidOperationException("Screen prefab root must be the TerminalCanvas: " + prefabPath);

            EnsureScreenPrefabFolder();

            if (AssetDatabase.LoadMainAssetAtPath(prefabPath) != null)
                throw new InvalidOperationException("Refusing to overwrite existing screen prefab: " + prefabPath);

            GameObject connected = PrefabUtility.SaveAsPrefabAssetAndConnect(
                screen.gameObject, prefabPath, InteractionMode.AutomatedAction, out bool success);
            if (!success || connected == null)
                throw new InvalidOperationException("Could not author screen prefab: " + prefabPath);
        }

        private static void EnsureInitialScreenPrefabPathsAreAvailable()
        {
            if (AssetDatabase.LoadMainAssetAtPath(MainMenuScreenPrefabPath) != null)
                throw new InvalidOperationException("Initial authoring would overwrite an existing screen prefab: " + MainMenuScreenPrefabPath);

            if (AssetDatabase.LoadMainAssetAtPath(GameplayScreenPrefabPath) != null)
                throw new InvalidOperationException("Initial authoring would overwrite an existing screen prefab: " + GameplayScreenPrefabPath);
        }

        private static void EnsureScreenPrefabFolder()
        {
            if (AssetDatabase.IsValidFolder(ScreenPrefabFolder))
                return;

            Directory.CreateDirectory(ScreenPrefabFolder);
            AssetDatabase.Refresh();
        }

        private static void PatchPrefab(string path, TMP_FontAsset font, bool isMenu)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                throw new InvalidOperationException("Prefab is missing: " + path);

            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                bool changed = false;

                foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
                    changed |= PatchLabel(label, font, isMenu);

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool PatchLabel(TMP_Text label, TMP_FontAsset font, bool isMenu)
        {
            bool changed = false;

            if (label.font != font)
            {
                label.font = font;
                changed = true;
            }

            string translated;

            if (label.name == "Name")
                translated = label.text == "DIGITS" ? "ЦИФРЫ" : label.text == "LETTERS" ? "БУКВЫ" : label.text;
            else if (label.name == "Description")
                translated = label.text.Contains("0–9") ? "ЧИСЛОВОЙ КАНАЛ  /  0–9" : label.text.Contains("A–Z") ? "БУКВЕННЫЙ КАНАЛ  /  A–Z" : label.text;
            else if (label.name == "Protocol" && !isMenu)
                translated = "ЦИФРОВОЙ ПРОТОКОЛ";
            else if (label.name == "State")
            {
                if (label.text == "VERIFIED")
                    translated = "ПРОВЕРЕНО";
                else if (label.text == "ERROR")
                    translated = "ОШИБКА";
                else if (label.text == "INPUT")
                    translated = "ВВОД";
                else
                    translated = label.text;
            }
            else if (!RussianLabels.TryGetValue(label.name, out translated))
            {
                translated = label.text;
            }

            if (label.text != translated)
            {
                label.text = translated;
                changed = true;
            }

            changed |= ApplyRussianTypography(label);

            if (changed)
                EditorUtility.SetDirty(label);

            return changed;
        }

        private static bool ApplyRussianTypography(TMP_Text label)
        {
            float size = label.fontSize;
            float spacing = label.characterSpacing;

            switch (label.name)
            {
                case "Loading":
                    size = 28;
                    spacing = 2;
                    break;
                case "Title":
                    size = 38;
                    spacing = 5;
                    break;
                case "Protocol":
                    size = 21;
                    spacing = 3;
                    break;
                case "Series":
                case "Status":
                case "Footer":
                    size = 12;
                    spacing = 1;
                    break;
                case "Instructions":
                    size = 18;
                    spacing = 1;
                    break;
                case "InputHint":
                    size = 18;
                    spacing = 2;
                    break;
                case "Name":
                    size = 27;
                    spacing = 4;
                    break;
                case "Description":
                    size = 13;
                    spacing = 1;
                    break;
                case "Progress":
                    size = 21;
                    spacing = 3;
                    break;
                case "Outcome":
                    size = 28;
                    spacing = 3;
                    break;
                case "Hint":
                    size = 19;
                    spacing = 2;
                    break;
                case "State":
                    size = 11;
                    spacing = 0;
                    break;
            }

            if (Mathf.Approximately(label.fontSize, size) && Mathf.Approximately(label.characterSpacing, spacing))
                return false;

            label.fontSize = size;
            label.characterSpacing = spacing;

            return true;
        }

        private static void EnsureLoadingCamera(Transform loadingRoot)
        {
            Transform child = loadingRoot.Find("Loading Camera");
            GameObject cameraObject = child == null ? new GameObject("Loading Camera", typeof(Camera)) : child.gameObject;

            if (child == null)
                cameraObject.transform.SetParent(loadingRoot, false);

            if (cameraObject.GetComponent<AudioListener>() != null)
                throw new InvalidOperationException("Loading Camera has an unexpected AudioListener; remove it manually before patching");

            Camera camera = cameraObject.GetComponent<Camera>();

            if (camera == null)
                camera = cameraObject.AddComponent<Camera>();

            cameraObject.SetActive(true);
            camera.enabled = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = LoadingBackground;
            camera.cullingMask = 0;
            camera.depth = -100;
            camera.targetDisplay = 0;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            EditorUtility.SetDirty(cameraObject);
            EditorUtility.SetDirty(camera);
        }

        private static void Seal(Transform parent, Vector2 position)
        {
            RectTransform root = Rect(parent, "CipherSeal", position.x, position.y, 100, 100);

            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48;
                float b = (i + 1) * Mathf.PI * 2 / 48;
                Line(root, "Ring", Mathf.Cos(a) * 42, Mathf.Sin(a) * 42, Mathf.Cos(b) * 42, Mathf.Sin(b) * 42, Amber);
            }

            Line(root, "Triangle", 0, 40, -35, -23, Amber);
            Line(root, "Triangle", -35, -23, 35, -23, Amber);
            Line(root, "Triangle", 35, -23, 0, 40, Amber);
            Box(root, "Center", Vector2.zero, new Vector2(7, 7), Amber);
        }

        private static RectTransform Canvas(string name, Vector2 resolution, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = resolution;
            scaler.screenMatchMode = order == 1000 ? CanvasScaler.ScreenMatchMode.MatchWidthOrHeight : CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = .5f;

            return (RectTransform)go.transform;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);

            return rect;
        }

        private static Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(parent, name, position.x, position.y, size.x, size.y);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        private static TMP_Text Label(Transform parent, string name, string text, float x, float y, float w, float h, float size, Color color, float spacing = 0)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.characterSpacing = spacing;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;

            return label;
        }

        private static void Line(Transform parent, string name, float x1, float y1, float x2, float y2, Color color)
        {
            var delta = new Vector2(x2 - x1, y2 - y1);
            var image = Box(parent, name, new Vector2((x1 + x2) / 2, (y1 + y2) / 2), new Vector2(delta.magnitude, 1.6f), color);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Camera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            var camera = go.AddComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.04f, .04f, .03f);
        }

        private static void NewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void Save(string name)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Root + "Scenes/" + name + ".unity");
        }
    }
}
