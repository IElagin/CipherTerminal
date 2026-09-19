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
        private const int DesignWidth = 1920;
        private const int DesignHeight = 1080;
        private const int LoadingSortOrder = 1000;
        private const float CompositionScale = 1.2f;
        private const float CenterFraction = .5f;
        private const float LineThickness = 1.6f;
        private const int RingSegmentCount = 48;
        private const float RingRadius = 42f;
        private const float RadiansPerTurn = Mathf.PI * 2;
        private const int NextSegmentOffset = 1;
        private const int SymbolDisplayOffset = 1;
        private const float CardColorMultiplier = 2f;
        private const float CardFadeDuration = .12f;
        private const float LoadingCameraDepth = -100f;
        private const int FontSamplingSize = 72;
        private const int FontAtlasPadding = 8;
        private const int FontAtlasSize = 1024;

        private static TMP_FontAsset _font;
        private static readonly Color _amber = TerminalView.Amber;
        private static readonly Color _ivory = TerminalView.Ivory;
        private static readonly Color _muted = new Color(.62f, .54f, .4f);
        private static readonly Color _loadingBackground = new Color(.055f, .05f, .035f);
        private static readonly Dictionary<string, string> _russianLabels = new Dictionary<string, string>
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
        public static void CreateInitialScenes()
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

            CreateScene();

            RectTransform cover = CreateCanvas("StandardLoadingScreen", new Vector2(DesignWidth, DesignHeight), LoadingSortOrder);
            Vector2 backgroundSize = new Vector2(4000, 4000);
            Image loadingBackground = CreateBox(cover, "Background", Vector2.zero, backgroundSize, _loadingBackground);
            loadingBackground.rectTransform.anchorMin = Vector2.zero;
            loadingBackground.rectTransform.anchorMax = Vector2.one;
            loadingBackground.rectTransform.offsetMin = Vector2.zero;
            loadingBackground.rectTransform.offsetMax = Vector2.zero;
            loadingBackground.raycastTarget = true;

            var loadingBounds = new Rect(0, 0, 920, 100);
            const float loadingFontSize = 28;
            const float loadingCharacterSpacing = 2;
            CreateLabel(cover, "Loading", "ИНИЦИАЛИЗАЦИЯ ТЕРМИНАЛА", loadingBounds, loadingFontSize, _amber, loadingCharacterSpacing);
            EnsureLoadingCamera(cover);
            cover.gameObject.AddComponent<StandardLoadingScreen>();
            PrefabUtility.SaveAsPrefabAsset(cover.gameObject, Root + "Resources/Utilities/StandardLoadingScreen.prefab");
            UnityEngine.Object.DestroyImmediate(cover.gameObject);
            SaveScene("Empty");

            CreateScene();
            CreateCamera();
            var projectScope = new GameObject("ProjectLifetimeScope").AddComponent<ProjectLifetimeScope>();
            projectScope.autoRun = false;
            var entryPoint = new GameObject("GameEntryPoint").AddComponent<GameEntryPoint>();
            SetObjectReference(entryPoint, "_projectScope", projectScope);
            SaveScene("GameEntryPoint");

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
            PlayerSettings.defaultScreenWidth = DesignWidth;
            PlayerSettings.defaultScreenHeight = DesignHeight;
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Root + "Scenes/GameEntryPoint.unity");
            Debug.Log("Cipher Terminal scenes and prefabs authored");
        }

        private static void CreateMenu()
        {
            CreateScene();
            CreateCamera();

            RectTransform ui = CreateScreenComposition("ВЫБЕРИТЕ ПРОТОКОЛ", out TerminalView view, out TMP_Text protocol, out Image pulse);
            float digitsHorizontalOffset = -290;
            var digits = CreateModeCard(ui, "Digits", digitsHorizontalOffset, "1", "ЦИФРЫ", "ЧИСЛОВОЙ КАНАЛ  /  0–9");
            float lettersHorizontalOffset = 290;
            var letters = CreateModeCard(ui, "Letters", lettersHorizontalOffset, "2", "БУКВЫ", "БУКВЕННЫЙ КАНАЛ  /  A–Z");
            var instructionsBounds = new Rect(0, -242, 1300, 50);
            const float instructionsFontSize = 18;
            const float instructionsCharacterSpacing = 1;
            CreateLabel(ui, "Instructions", "ПОВТОРИТЕ КОД. ВАЖЕН КАЖДЫЙ СИМВОЛ.", instructionsBounds, instructionsFontSize, _ivory, instructionsCharacterSpacing);
            var inputHintBounds = new Rect(0, -310, 1300, 50);
            const float inputHintFontSize = 18;
            const float inputHintCharacterSpacing = 2;
            CreateLabel(ui, "InputHint", "НАЖМИТЕ 1 ИЛИ 2 ДЛЯ ПОДКЛЮЧЕНИЯ", inputHintBounds, inputHintFontSize, _amber, inputHintCharacterSpacing);
            SetObjectReference(view, "_digitsButton", digits);
            SetObjectReference(view, "_lettersButton", letters);
            SetObjectReference(view, "_pulse", pulse);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var createdObject = new GameObject("Bootstrap");
            var bootstrap = createdObject.AddComponent<MainMenuBootstrap>();
            var controller = createdObject.AddComponent<MainMenuController>();
            var keyboard = createdObject.AddComponent<TerminalKeyboard>();
            SetObjectReference(bootstrap, "_controller", controller);
            SetObjectReference(controller, "_view", view);
            SetObjectReference(controller, "_keyboard", keyboard);

            view.SetMenuEnabled(false);
            SaveScreenPrefabAndConnect((RectTransform)ui.parent, MainMenuScreenPrefabPath);
            SaveScene("MainMenu");
        }

        private static void CreateGameplay()
        {
            CreateScene();
            CreateCamera();

            RectTransform ui = CreateScreenComposition("ЦИФРОВОЙ ПРОТОКОЛ", out TerminalView view, out TMP_Text protocol, out Image pulse);
            float sequenceY = -5;
            float sequenceWidth = 1320;
            float sequenceHeight = 220;
            RectTransform row = CreateRect(ui, "Sequence", 0, sequenceY, sequenceWidth, sequenceHeight);
            TerminalCell[] cells = new TerminalCell[Runtime.Gameplay.Sequence.SequenceGenerator.MaximumLength];

            for (int i = 0; i < cells.Length; i++)
            {
                int symbolNumber = i + SymbolDisplayOffset;
                Vector2 symbolCellPosition = Vector2.zero;
                Vector2 symbolCellSize = new Vector2(190, 215);
                Image border = CreateBox(row, "Symbol " + symbolNumber, symbolCellPosition, symbolCellSize, _muted);
                Vector2 fillSize = new Vector2(186, 211);
                Color fillColor = new Color(.055f, .05f, .035f);
                Image fill = CreateBox(border.rectTransform, "Fill", Vector2.zero, fillSize, fillColor);
                // Fill follows resized cell so configurable sequence length remains supported.
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = Vector2.one;
                Vector2 borderInset = new Vector2(2, 2);
                fill.rectTransform.offsetMin = borderInset;
                fill.rectTransform.offsetMax = -borderInset;

                var symbolBounds = new Rect(0, 12, 170, 140);
                const float symbolFontSize = 94;
                TMP_Text symbol = CreateLabel(border.transform, "Symbol", "0", symbolBounds, symbolFontSize, _ivory);
                var stateBounds = new Rect(0, -72, 180, 32);
                const float stateFontSize = 13;
                const float stateCharacterSpacing = 1;
                TMP_Text marker = CreateLabel(border.transform, "State", "", stateBounds, stateFontSize, _amber, stateCharacterSpacing);
                cells[i] = border.gameObject.AddComponent<TerminalCell>();
                SetObjectReference(cells[i], "_symbol", symbol);
                SetObjectReference(cells[i], "_marker", marker);
                SetObjectReference(cells[i], "_border", border);
                SetObjectReference(cells[i], "_fill", fill);
            }

            Vector2 cursorPosition = new Vector2(0, -92);
            Vector2 cursorSize = new Vector2(40, 2);
            Image cursor = CreateBox(row, "Cursor", cursorPosition, cursorSize, _amber);
            Vector2 completionSweepSize = new Vector2(4, 215);
            Color completionSweepColor = new Color(1, .69f, .23f, .35f);
            Image sweep = CreateBox(row, "CompletionSweep", Vector2.zero, completionSweepSize, completionSweepColor);
            sweep.gameObject.SetActive(false);
            SetObjectReference(view, "_sweep", sweep);
            var progressBounds = new Rect(0, -177, 1300, 42);
            const float progressFontSize = 21;
            const float progressCharacterSpacing = 3;
            TMP_Text progress = CreateLabel(ui, "Progress", "0 / 6  ПРОВЕРЕНО", progressBounds, progressFontSize, _amber, progressCharacterSpacing);
            var outcomeBounds = new Rect(0, -246, 1300, 60);
            const float outcomeFontSize = 30;
            const float outcomeCharacterSpacing = 7;
            TMP_Text outcome = CreateLabel(ui, "Outcome", "", outcomeBounds, outcomeFontSize, _amber, outcomeCharacterSpacing);
            var hintBounds = new Rect(0, -316, 1300, 45);
            const float hintFontSize = 19;
            const float hintCharacterSpacing = 2;
            TMP_Text hint = CreateLabel(ui, "Hint", "ВВЕДИТЕ ПОСЛЕДОВАТЕЛЬНОСТЬ", hintBounds, hintFontSize, _ivory, hintCharacterSpacing);
            SetObjectReference(view, "_protocol", protocol);
            SetObjectReference(view, "_progress", progress);
            SetObjectReference(view, "_outcome", outcome);
            SetObjectReference(view, "_hint", hint);
            SetObjectReference(view, "_cursor", cursor);
            SetObjectReference(view, "_pulse", pulse);
            SetObjectReference(view, "_cellRow", row);

            var serializedView = new SerializedObject(view);
            var cellProperty = serializedView.FindProperty("_cells");
            cellProperty.arraySize = cells.Length;

            for (int i = 0; i < cells.Length; i++)
                cellProperty.GetArrayElementAtIndex(i).objectReferenceValue = cells[i];

            serializedView.ApplyModifiedPropertiesWithoutUndo();
            const int previewSeed = 0;
            var previewGenerator = new Runtime.Gameplay.Sequence.SequenceGenerator(new System.Random(previewSeed));
            var previewSession = new Runtime.Gameplay.Sequence.SequenceSession(previewGenerator);
            previewSession.Initialize("0123456789", cells.Length);
            view.ShowSequence(previewSession, Runtime.Gameplay.Sequence.SequenceMode.Digits);

            var createdObject = new GameObject("Bootstrap");
            var bootstrap = createdObject.AddComponent<GameplayBootstrap>();
            var controller = createdObject.AddComponent<GameplayController>();
            var keyboard = createdObject.AddComponent<TerminalKeyboard>();
            SetObjectReference(bootstrap, "_controller", controller);
            SetObjectReference(controller, "_view", view);
            SetObjectReference(controller, "_keyboard", keyboard);

            SaveScreenPrefabAndConnect((RectTransform)ui.parent, GameplayScreenPrefabPath);
            SaveScene("Gameplay");
        }

        private static RectTransform CreateScreenComposition(string mode, out TerminalView view, out TMP_Text protocol, out Image pulse)
        {
            RectTransform canvas = CreateCanvas("TerminalCanvas", new Vector2(DesignWidth, DesignHeight), 0);
            var background = new GameObject("Background", typeof(RectTransform), typeof(RawImage));
            background.transform.SetParent(canvas, false);

            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var backgroundImage = background.GetComponent<RawImage>();
            backgroundImage.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Art/Textures/TerminalBackground.png");
            backgroundImage.raycastTarget = false;
            float compositionWidth = 1600;
            float compositionHeight = 900;
            RectTransform ui = CreateRect(canvas, "Composition", 0, 0, compositionWidth, compositionHeight);
            // Preserve the authored local coordinates while fitting the 1920×1080 design canvas.
            ui.localScale = Vector3.one * CompositionScale;
            view = ui.gameObject.AddComponent<TerminalView>();

            const float frameLeft = -770;
            const float frameRight = 770;
            const float frameTop = 421;
            const float frameBottom = -421;
            CreateLine(ui, "Frame Top", frameLeft, frameTop, frameRight, frameTop, _muted);
            CreateLine(ui, "Frame Bottom", frameLeft, frameBottom, frameRight, frameBottom, _muted);
            CreateLine(ui, "Frame Left", frameLeft, frameBottom, frameLeft, frameTop, _muted);
            CreateLine(ui, "Frame Right", frameRight, frameBottom, frameRight, frameTop, _muted);

            int[] registrationSides = { -1, 1 };

            foreach (int side in registrationSides)
            {
                const float registrationOffset = 733f;
                const float registrationHalfWidth = 9f;
                const float registrationVerticalOffset = 190f;
                const float registrationTop = 199f;
                const float registrationBottom = 181f;
                float registrationCenter = side * registrationOffset;
                float registrationLeft = registrationCenter - registrationHalfWidth;
                float registrationRight = registrationCenter + registrationHalfWidth;
                CreateLine(ui, "Registration", registrationLeft, registrationVerticalOffset, registrationRight, registrationVerticalOffset, _muted);
                CreateLine(ui, "Registration", registrationCenter, registrationBottom, registrationCenter, registrationTop, _muted);
                CreateLine(ui, "Registration", registrationLeft, -registrationVerticalOffset, registrationRight, -registrationVerticalOffset, _muted);
                CreateLine(ui, "Registration", registrationCenter, -registrationTop, registrationCenter, -registrationBottom, _muted);
            }

            var seriesBounds = new Rect(-564, 365, 360, 60);
            const float seriesFontSize = 12;
            const float seriesCharacterSpacing = 1;
            CreateLabel(ui, "Series", "ШИФРОВАЛЬНЫЙ АППАРАТ\nСЕРИЯ 01", seriesBounds, seriesFontSize, _muted, seriesCharacterSpacing).alignment = TextAlignmentOptions.Left;
            var statusBounds = new Rect(564, 365, 360, 60);
            const float statusFontSize = 12;
            const float statusCharacterSpacing = 1;
            CreateLabel(ui, "Status", "ЛОКАЛЬНОЕ СОЕДИНЕНИЕ\nКЛАВИАТУРА В СЕТИ", statusBounds, statusFontSize, _muted, statusCharacterSpacing).alignment = TextAlignmentOptions.Right;
            Vector2 sealPosition = new Vector2(0, 329);
            CreateSeal(ui, sealPosition);
            var titleBounds = new Rect(0, 244, 1370, 85);
            const float titleFontSize = 38;
            const float titleCharacterSpacing = 5;
            CreateLabel(ui, "Title", "ТЕРМИНАЛ ШИФРОВАНИЯ", titleBounds, titleFontSize, _amber, titleCharacterSpacing);
            var protocolBounds = new Rect(0, 176, 1100, 45);
            const float protocolFontSize = 21;
            const float protocolCharacterSpacing = 3;
            protocol = CreateLabel(ui, "Protocol", mode, protocolBounds, protocolFontSize, _ivory, protocolCharacterSpacing);
            float dividerStartX = -330;
            float dividerStartY = 134;
            float dividerEndX = 330;
            float dividerEndY = 134;
            Color dividerColor = new Color(.4f, .3f, .15f);
            CreateLine(ui, "Divider", dividerStartX, dividerStartY, dividerEndX, dividerEndY, dividerColor);
            var footerBounds = new Rect(0, -386, 1300, 34);
            const float footerFontSize = 12;
            const float footerCharacterSpacing = 1;
            CreateLabel(ui, "Footer", "ТЕРМИНАЛ ШИФРОВАНИЯ   /   ПОЛЕВАЯ МОДЕЛЬ А", footerBounds, footerFontSize, _muted, footerCharacterSpacing);
            Vector2 feedbackPulseSize = new Vector2(1400, 630);
            pulse = CreateBox(ui, "FeedbackPulse", Vector2.zero, feedbackPulseSize, Color.clear);

            return ui;
        }

        private static Button CreateModeCard(Transform ui, string name, float x, string key, string title, string subtitle)
        {
            Vector2 cardPosition = new Vector2(x, -18);
            Vector2 cardSize = new Vector2(530, 267);
            Color cardBorderColor = new Color(.28f, .2f, .08f);
            Image image = CreateBox(ui, name, cardPosition, cardSize, cardBorderColor);
            Vector2 insetSize = new Vector2(528, 265);
            Color insetColor = new Color(.055f, .05f, .035f, .95f);
            CreateBox(image.transform, "Inset", Vector2.zero, insetSize, insetColor);

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            image.raycastTarget = true;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(.7f, .63f, .4f);
            colors.highlightedColor = new Color(1, .86f, .48f);
            colors.pressedColor = new Color(1, .72f, .2f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(.5f, .46f, .35f);
            colors.colorMultiplier = CardColorMultiplier;
            colors.fadeDuration = CardFadeDuration;
            button.colors = colors;

            var keyBounds = new Rect(0, 66, 440, 93);
            const float keyFontSize = 70;
            CreateLabel(image.transform, "Key", key, keyBounds, keyFontSize, _amber);
            var nameBounds = new Rect(0, -21, 470, 65);
            const float nameFontSize = 27;
            const float nameCharacterSpacing = 4;
            CreateLabel(image.transform, "Name", title, nameBounds, nameFontSize, _ivory, nameCharacterSpacing);
            var descriptionBounds = new Rect(0, -87, 490, 38);
            const float descriptionFontSize = 13;
            const float descriptionCharacterSpacing = 1;
            CreateLabel(image.transform, "Description", subtitle, descriptionBounds, descriptionFontSize, _muted, descriptionCharacterSpacing);

            return button;
        }

        private static TMP_FontAsset GetOrCreateRussianFont()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RussianFontPath);

            if (existing != null)
            {
                if (existing.HasCharacters(GetRussianCharacterSet(), out List<char> missing) == false)
                    {
                    string missingGlyphs = new string(missing.ToArray());
                    throw new InvalidOperationException("Existing Russian font asset is missing glyphs: " + missingGlyphs);
                }

                return existing;
            }

            Font source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

            if (source == null)
                throw new InvalidOperationException("TMP LiberationSans source font is missing: " + SourceFontPath);

            Directory.CreateDirectory(Root + "Fonts");
            AssetDatabase.Refresh();

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, FontSamplingSize, FontAtlasPadding, GlyphRenderMode.SDFAA, FontAtlasSize, FontAtlasSize,
                AtlasPopulationMode.Dynamic, false);

            if (font == null)
                throw new InvalidOperationException("Could not create TMP font asset from " + SourceFontPath);

            if (font.TryAddCharacters(GetRussianCharacterSet(), out string missingCharacters, true) == false)
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

        private static string GetRussianCharacterSet()
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

            if (string.IsNullOrEmpty(connectedPrefabPath) == false)
                throw new InvalidOperationException("TerminalCanvas is already connected to a different prefab: " + connectedPrefabPath);

            GameObject connected = PrefabUtility.SaveAsPrefabAssetAndConnect(
                screen, prefabPath, InteractionMode.AutomatedAction, out bool success);

            if (success == false || connected == null || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(connected) != prefabPath)
                throw new InvalidOperationException("Could not create and connect screen prefab: " + prefabPath);

            EditorSceneManager.MarkSceneDirty(scene);

            if (EditorSceneManager.SaveScene(scene) == false)
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

            if (success == false || connected == null)
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
            else if (label.name == "Protocol" && isMenu == false)
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
            else if (_russianLabels.TryGetValue(label.name, out translated) == false)
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
                    const float loadingFontSize = 28;
                    const float loadingCharacterSpacing = 2;
                    size = loadingFontSize;
                    spacing = loadingCharacterSpacing;
                    break;

                case "Title":
                    const float titleFontSize = 38;
                    const float titleCharacterSpacing = 5;
                    size = titleFontSize;
                    spacing = titleCharacterSpacing;
                    break;

                case "Protocol":
                    const float protocolFontSize = 21;
                    const float protocolCharacterSpacing = 3;
                    size = protocolFontSize;
                    spacing = protocolCharacterSpacing;
                    break;

                case "Series":

                case "Status":

                case "Footer":
                    const float footerFontSize = 12;
                    const float footerCharacterSpacing = 1;
                    size = footerFontSize;
                    spacing = footerCharacterSpacing;
                    break;

                case "Instructions":
                    const float instructionsFontSize = 18;
                    const float instructionsCharacterSpacing = 1;
                    size = instructionsFontSize;
                    spacing = instructionsCharacterSpacing;
                    break;

                case "InputHint":
                    const float inputHintFontSize = 18;
                    const float inputHintCharacterSpacing = 2;
                    size = inputHintFontSize;
                    spacing = inputHintCharacterSpacing;
                    break;

                case "Name":
                    const float nameFontSize = 27;
                    const float nameCharacterSpacing = 4;
                    size = nameFontSize;
                    spacing = nameCharacterSpacing;
                    break;

                case "Description":
                    const float descriptionFontSize = 13;
                    const float descriptionCharacterSpacing = 1;
                    size = descriptionFontSize;
                    spacing = descriptionCharacterSpacing;
                    break;

                case "Progress":
                    const float progressFontSize = 21;
                    const float progressCharacterSpacing = 3;
                    size = progressFontSize;
                    spacing = progressCharacterSpacing;
                    break;

                case "Outcome":
                    const float outcomeFontSize = 28;
                    const float outcomeCharacterSpacing = 3;
                    size = outcomeFontSize;
                    spacing = outcomeCharacterSpacing;
                    break;

                case "Hint":
                    const float hintFontSize = 19;
                    const float hintCharacterSpacing = 2;
                    size = hintFontSize;
                    spacing = hintCharacterSpacing;
                    break;

                case "State":
                    const float stateFontSize = 11;
                    const float stateCharacterSpacing = 0;
                    size = stateFontSize;
                    spacing = stateCharacterSpacing;
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
            camera.backgroundColor = _loadingBackground;
            camera.cullingMask = 0;
            camera.depth = LoadingCameraDepth;
            camera.targetDisplay = 0;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            EditorUtility.SetDirty(cameraObject);
            EditorUtility.SetDirty(camera);
        }

        private static void CreateSeal(Transform parent, Vector2 position)
        {
            float cipherSealWidth = 100;
            float cipherSealHeight = 100;
            RectTransform root = CreateRect(parent, "CipherSeal", position.x, position.y, cipherSealWidth, cipherSealHeight);

            for (int i = 0; i < RingSegmentCount; i++)
            {
                float startAngle = i * RadiansPerTurn / RingSegmentCount;
                float endAngle = (i + NextSegmentOffset) * RadiansPerTurn / RingSegmentCount;
                CreateLine(root, "Ring", Mathf.Cos(startAngle) * RingRadius, Mathf.Sin(startAngle) * RingRadius, Mathf.Cos(endAngle) * RingRadius, Mathf.Sin(endAngle) * RingRadius, _amber);
            }

            Vector2 triangleTop = new Vector2(0, 40);
            Vector2 triangleLeft = new Vector2(-35, -23);
            Vector2 triangleRight = new Vector2(35, -23);
            CreateLine(root, "Triangle", triangleTop.x, triangleTop.y, triangleLeft.x, triangleLeft.y, _amber);
            CreateLine(root, "Triangle", triangleLeft.x, triangleLeft.y, triangleRight.x, triangleRight.y, _amber);
            CreateLine(root, "Triangle", triangleRight.x, triangleRight.y, triangleTop.x, triangleTop.y, _amber);
            Vector2 centerSize = new Vector2(7, 7);
            CreateBox(root, "Center", Vector2.zero, centerSize, _amber);
        }

        private static RectTransform CreateCanvas(string name, Vector2 resolution, int order)
        {
            var createdObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = createdObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;

            CanvasScaler scaler = createdObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = resolution;
            scaler.screenMatchMode = order == LoadingSortOrder ? CanvasScaler.ScreenMatchMode.MatchWidthOrHeight : CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = CenterFraction;

            return (RectTransform)createdObject.transform;
        }

        private static RectTransform CreateRect(Transform parent, string name, float x, float y, float width, float height)
        {
            var createdObject = new GameObject(name, typeof(RectTransform));
            createdObject.transform.SetParent(parent, false);

            var rect = (RectTransform)createdObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(CenterFraction, CenterFraction);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);

            return rect;
        }

        private static Image CreateBox(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var rect = CreateRect(parent, name, position.x, position.y, size.x, size.y);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        private static TMP_Text CreateLabel(Transform parent, string name, string text, Rect bounds, float size, Color color, float spacing = 0)
        {
            var rect = CreateRect(parent, name, bounds.x, bounds.y, bounds.width, bounds.height);
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

        private static void CreateLine(Transform parent, string name, float x1, float y1, float x2, float y2, Color color)
        {
            var delta = new Vector2(x2 - x1, y2 - y1);
            var image = CreateBox(parent, name, new Vector2((x1 + x2) * CenterFraction, (y1 + y2) * CenterFraction), new Vector2(delta.magnitude, LineThickness), color);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static void SetObjectReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCamera()
        {
            var createdObject = new GameObject("Main Camera");
            createdObject.tag = "MainCamera";

            var camera = createdObject.AddComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.04f, .04f, .03f);
        }

        private static void CreateScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void SaveScene(string name)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Root + "Scenes/" + name + ".unity");
        }
    }
}
