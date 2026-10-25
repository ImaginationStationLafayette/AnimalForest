using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WoodlandAnimalCards.Editor
{
    /// <summary>
    /// Explicit opt-in asset generation. No initialization hook, package changes,
    /// scene save/open, EventSystem creation, camera edits or input-setting edits.
    /// </summary>
    public static class WoodlandCardSetup
    {
        private const string Root = "Assets/WoodlandAnimalCards";

        private static readonly string[] Species =
        {
            "Cat",
            "Penguin",
            "Chicken",
            "Dog",
            "Horse",
            "Tiger",
            "Deer"
        };

        private static readonly string[] Greetings =
        {
            "Hi! I'm a cat. I love exploring, climbing, and napping in sunny spots.",
            "Hi! I'm a penguin. I use my flippers to swim and my feathers to keep warm.",
            "Hi! I'm a chicken. I scratch the ground to find seeds, insects, and tasty things to eat.",
            "Hi! I'm a dog. I explore the world with my nose. There are so many interesting smells!",
            "Hi! I'm a horse. I graze on grass and use my long legs to run across open spaces.",
            "Hi! I'm a tiger. My stripes help me blend in as I move quietly through tall grass.",
            "Hi! I'm a deer. My big ears help me listen for sounds while I explore the forest."
        };

        private static readonly string[] Food =
        {
            "I'm a meat eater! Pet cats need food made especially for cats.",
            "I eat sea creatures such as fish, krill, and squid. My menu depends on my species.",
            "I eat seeds, grains, plants, and small insects. I scratch the ground to find food.",
            "Pet dogs eat balanced dog food. Some foods that people eat are not safe for dogs.",
            "I mostly eat grass and hay. I'm a herbivore, which means I eat plants.",
            "I'm a carnivore. In the wild I hunt animals such as deer and wild pigs.",
            "I eat plants, including leaves, grasses, shoots, and twigs. My menu changes with the seasons."
        };

        private static readonly string[] Homes =
        {
            "Many cats live with people. A safe home gives a pet cat places to rest, play, and explore.",
            "Penguins live in the Southern Hemisphere. Some live in icy places, but others live on warmer coasts.",
            "Domestic chickens often live on farms or in backyard flocks, with shelter to keep them safe.",
            "Many dogs live with people. We need a safe home, exercise, and caring company.",
            "Horses often live in pastures, with shelter or stables. Wild horses live in open landscapes.",
            "Wild tigers live in parts of Asia, in habitats including forests, grasslands, and mangrove swamps.",
            "Different deer live in forests, woodlands, grasslands, and other habitats. I look for plants and cover."
        };

        private static readonly string[] Facts =
        {
            "My whiskers are sensitive touch sensors. They help me notice nearby objects.",
            "I can't fly through the air, but my flippers make me a strong swimmer!",
            "I take dust baths. Rolling in dry soil helps me care for my feathers.",
            "My sense of smell helps me learn about the world. A sniff can tell me a lot!",
            "I can doze while standing, but I also lie down to sleep deeply.",
            "Each tiger has its own stripe pattern, rather like a fingerprint.",
            "I can turn my ears toward sounds. That helps me notice what's happening around me."
        };

        [MenuItem("Tools/Woodland Animal Cards/Generate Both Styles")]
        public static void GenerateBothStyles()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Woodland cards",
                    "Exit Play Mode before generating assets.",
                    "OK"
                );

                return;
            }

            if (
                !Directory.Exists(Root + "/Art") ||
                !Directory.Exists(Root + "/Fonts")
            )
            {
                EditorUtility.DisplayDialog(
                    "Woodland cards",
                    "Keep this kit at Assets/WoodlandAnimalCards, including Art and Fonts.",
                    "OK"
                );

                return;
            }

            if (Shader.Find("TextMeshPro/Distance Field") == null)
            {
                EditorUtility.DisplayDialog(
                    "Import TMP resources first",
                    "Choose Window > TextMeshPro > Import TMP Essential Resources, then run this menu again. This kit does not install packages or change project settings.",
                    "OK"
                );

                return;
            }

            string output =
                Root +
                "/Generated/Build_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

            Scene previewScene = default;

            try
            {
                MakeFolder(output + "/Fonts");
                MakeFolder(output + "/Data");
                MakeFolder(output + "/LowPoly");
                MakeFolder(output + "/Cozy");

                ConfigureSprites();

                TMP_FontAsset body =
                    MakeFont("SemiBold", output);

                TMP_FontAsset label =
                    MakeFont("Bold", output);

                TMP_FontAsset heavy =
                    MakeFont("ExtraBold", output);

                AnimalCardData[] animals =
                    MakeAnimals(output);

                // Temporary construction scene only.
                previewScene =
                    EditorSceneManager.NewPreviewScene();

                foreach (bool cozy in new[] { false, true })
                {
                    string style =
                        cozy ? "Cozy" : "LowPoly";

                    foreach (bool tall in new[] { false, true })
                    {
                        foreach (AnimalCardData animal in animals)
                        {
                            string layout =
                                tall ? "TallLeft" : "Wide";

                            GameObject canvas =
                                BuildCanvas(
                                    previewScene,
                                    cozy,
                                    tall,
                                    animal,
                                    body,
                                    label,
                                    heavy
                                );

                            try
                            {
                                string path =
                                    output +
                                    "/" +
                                    style +
                                    "/" +
                                    style +
                                    "_" +
                                    layout +
                                    "_" +
                                    animal.species +
                                    ".prefab";

                                PrefabUtility.SaveAsPrefabAsset(
                                    canvas,
                                    path,
                                    out bool success
                                );

                                if (!success)
                                {
                                    throw new InvalidOperationException(
                                        "Unity could not save " + path
                                    );
                                }
                            }
                            finally
                            {
                                Object.DestroyImmediate(canvas);
                            }
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject =
                    AssetDatabase.LoadAssetAtPath<Object>(
                        output
                    );

                EditorGUIUtility.PingObject(
                    Selection.activeObject
                );

                EditorUtility.DisplayDialog(
                    "28 card prefabs generated",
                    "Both styles are in:\n" +
                    output +
                    "\n\nDrag ONE prefab into your scene. Add a portrait to its Animal Data asset. Use an existing EventSystem with Input System UI Input Module. Read START HERE for setup and wiring.\n\nYour scenes, input settings, cameras and packages were not changed. Re-running creates a new folder and leaves earlier generated assets alone.",
                    "OK"
                );
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "Generation stopped",
                    "See the Console for the exact error. Any partial output is in:\n" +
                    output +
                    "\n\nNo existing generated folder was overwritten. Correct the error, then run the menu again.",
                    "OK"
                );
            }
            finally
            {
                if (previewScene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(
                        previewScene
                    );
                }
            }
        }

        private static void MakeFolder(string path)
        {
            string[] parts =
                path.Split('/');

            string currentPath =
                parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath =
                    currentPath +
                    "/" +
                    parts[i];

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(
                        currentPath,
                        parts[i]
                    );
                }

                currentPath =
                    nextPath;
            }
        }

        private static void ConfigureSprites()
        {
            foreach (
                string path in
                Directory.GetFiles(
                    Root + "/Art",
                    "*.png",
                    SearchOption.AllDirectories
                )
            )
            {
                string assetPath =
                    path.Replace('\\', '/');

                var importer =
                    AssetImporter.GetAtPath(
                        assetPath
                    ) as TextureImporter;

                if (importer == null)
                {
                    throw new InvalidOperationException(
                        "Texture not imported: " +
                        assetPath
                    );
                }

                importer.textureType =
                    TextureImporterType.Sprite;

                importer.spriteImportMode =
                    SpriteImportMode.Single;

                importer.spritePixelsPerUnit =
                    200f;

                importer.alphaIsTransparency =
                    true;

                importer.mipmapEnabled =
                    false;

                importer.isReadable =
                    false;

                importer.filterMode =
                    FilterMode.Bilinear;

                importer.wrapMode =
                    TextureWrapMode.Clamp;

                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;

                importer.maxTextureSize =
                    2048;

                var settings =
                    new TextureImporterSettings();

                importer.ReadTextureSettings(
                    settings
                );

                settings.spriteMeshType =
                    SpriteMeshType.FullRect;

                importer.SetTextureSettings(
                    settings
                );

                string name =
                    Path.GetFileName(
                        assetPath
                    );

                if (
                    assetPath.Contains("/Cozy/") &&
                    name.StartsWith("panel_base")
                )
                {
                    importer.spriteBorder =
                        new Vector4(
                            96,
                            112,
                            96,
                            112
                        );
                }
                else if (
                    assetPath.Contains("/Cozy/") &&
                    name.StartsWith("button_")
                )
                {
                    importer.spriteBorder =
                        new Vector4(
                            68,
                            32,
                            68,
                            32
                        );
                }
                else if (
                    assetPath.Contains("/LowPoly/") &&
                    name.StartsWith("button_")
                )
                {
                    importer.spriteBorder =
                        new Vector4(
                            24,
                            20,
                            24,
                            20
                        );
                }
                else
                {
                    importer.spriteBorder =
                        Vector4.zero;
                }

                importer.SaveAndReimport();
            }
        }

        private static TMP_FontAsset MakeFont(
            string weight,
            string output
        )
        {
            Font source =
                AssetDatabase.LoadAssetAtPath<Font>(
                    Root +
                    "/Fonts/Nunito-" +
                    weight +
                    ".ttf"
                );

            if (source == null)
            {
                throw new InvalidOperationException(
                    "Missing Nunito-" +
                    weight +
                    ".ttf"
                );
            }

            TMP_FontAsset font =
                TMP_FontAsset.CreateFontAsset(
                    source,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true
                );

            if (font == null)
            {
                throw new InvalidOperationException(
                    "TMP could not create Nunito " +
                    weight
                );
            }

            font.name =
                "Nunito-" +
                weight +
                " SDF";

            string characters =
                new string(
                    Enumerable
                        .Range(32, 95)
                        .Select(i => (char)i)
                        .ToArray()
                ) +
                "\u2018\u2019\u201c\u201d\u2013\u2014\u2026";

            if (
                !font.TryAddCharacters(
                    characters,
                    out string missing
                )
            )
            {
                Debug.LogWarning(
                    "Some requested Nunito glyphs were not added: " +
                    missing
                );
            }

            string assetPath =
                output +
                "/Fonts/" +
                font.name +
                ".asset";

            AssetDatabase.CreateAsset(
                font,
                assetPath
            );

            font.material.name =
                font.name +
                " Material";

            AssetDatabase.AddObjectToAsset(
                font.material,
                font
            );

            foreach (
                Texture2D atlas in
                font.atlasTextures
            )
            {
                if (
                    atlas != null &&
                    !AssetDatabase.Contains(atlas)
                )
                {
                    AssetDatabase.AddObjectToAsset(
                        atlas,
                        font
                    );
                }
            }

            EditorUtility.SetDirty(font);

            return font;
        }

        private static AnimalCardData[] MakeAnimals(
            string output
        )
        {
            var data =
                new AnimalCardData[
                    Species.Length
                ];

            for (int i = 0; i < data.Length; i++)
            {
                var animal =
                    ScriptableObject
                        .CreateInstance<AnimalCardData>();

                animal.species =
                    Species[i];

                animal.animalGroup =
                    (i == 1 || i == 2)
                        ? "BIRD"
                        : "MAMMAL";

                animal.greeting =
                    Greetings[i];

                animal.foodAnswer =
                    Food[i];

                animal.homeAnswer =
                    Homes[i];

                animal.funFactAnswer =
                    Facts[i];

                AssetDatabase.CreateAsset(
                    animal,
                    output +
                    "/Data/" +
                    animal.species +
                    ".asset"
                );

                data[i] =
                    animal;
            }

            return data;
        }

        private static GameObject BuildCanvas(
            Scene scene,
            bool cozy,
            bool tall,
            AnimalCardData animal,
            TMP_FontAsset body,
            TMP_FontAsset label,
            TMP_FontAsset heavy
        )
        {
            string style =
                cozy
                    ? "Cozy"
                    : "LowPoly";

            var root =
                new GameObject(
                    "Woodland " +
                    style +
                    (
                        tall
                            ? " Tall Left "
                            : " Wide "
                    ) +
                    animal.species,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster)
                );

            SceneManager.MoveGameObjectToScene(
                root,
                scene
            );

            root.layer = 5;

            Canvas canvas =
                root.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder =
                10;

            CanvasScaler scaler =
                root.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode
                    .ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(
                    1280,
                    720
                );

            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode
                    .MatchWidthOrHeight;

            scaler.matchWidthOrHeight =
                0.5f;

            scaler.referencePixelsPerUnit =
                100f;

            float cardWidth =
                tall ? 420 : 760;

            float cardHeight =
                tall ? 720 : 440;

            RectTransform card =
                Rect(
                    "Animal Card",
                    root.transform,
                    0,
                    0,
                    cardWidth,
                    cardHeight
                );

            card.anchorMin =
                card.anchorMax =
                    new Vector2(
                        0.5f,
                        0.5f
                    );

            card.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            card.anchoredPosition =
                Vector2.zero;

            CardViewportFit fit =
                card.gameObject
                    .AddComponent<CardViewportFit>();

            fit.anchorToLeft =
                tall;

            AnimalDialogueCard controller =
                card.gameObject
                    .AddComponent<AnimalDialogueCard>();

            controller.animal =
                animal;

            Image panelBackground =
                MakeImage(
                    "Panel Background",
                    card,
                    style,
                    tall
                        ? "panel_tall"
                        : "panel_wide",
                    0,
                    0,
                    cardWidth,
                    cardHeight,
                    true
                );

            if (!cozy)
            {
                panelBackground.color =
                    new Color(
                        1f,
                        1f,
                        1f,
                        0.55f
                    );
            }

            if (cozy)
            {
                MakeImage(
                    "Leaf Decorations",
                    card,
                    "Cozy",
                    tall
                        ? "leaf_decorations_tall"
                        : "leaf_decorations_wide",
                    0,
                    0,
                    cardWidth,
                    cardHeight
                );
            }

            Color primary =
                Hex(
                    cozy
                        ? "493426"
                        : "182718"
                );

            Color secondary =
                Hex(
                    cozy
                        ? "775035"
                        : "2B3E28"
                );

            float px =
                tall
                    ? 118
                    : (cozy ? 58 : 36);

            float py =
                tall
                    ? (cozy ? 54 : 32)
                    : (cozy ? 52 : 30);

            float pw =
                tall
                    ? (cozy ? 184 : 196)
                    : (cozy ? 196 : 220);

            float ph =
                tall
                    ? (cozy ? 148 : 168)
                    : (cozy ? 180 : 224);

            Image backing =
                MakeImage(
                    "Portrait Background",
                    card,
                    style,
                    tall
                        ? "portrait_tall"
                        : "portrait_background",
                    px,
                    py,
                    pw,
                    ph
                );

            backing.raycastTarget =
                false;

            RectTransform portrait =
                Rect(
                    "Portrait Slot",
                    card,
                    px + 4,
                    py + 4,
                    pw - 8,
                    ph - 8
                );

            var picture =
                portrait.gameObject
                    .AddComponent<Image>();

            picture.raycastTarget =
                false;

            picture.preserveAspect =
                true;

            picture.enabled =
                false;

            controller.portraitImage =
                picture;

            TMP_Text hint =
                MakeText(
                    "Empty Portrait Hint",
                    card,
                    "Your animal\nimage",
                    label,
                    cozy || tall ? 16 : 17,
                    cozy
                        ? secondary
                        : Hex("CFD3C9"),
                    px + 12,
                    py +
                        (ph - 50) /
                        2,
                    pw - 24,
                    50
                );

            hint.alignment =
                TextAlignmentOptions.Center;

            controller.emptyPortraitHint =
                hint.gameObject;

            float tx =
                tall
                    ? (cozy ? 64 : 44)
                    : (cozy ? 282 : 284);

            float ty =
                tall
                    ? 218
                    : (cozy ? 50 : 30);

            float tw =
                tall
                    ? (cozy ? 292 : 332)
                    : (cozy ? 400 : 392);

            if (cozy && !tall)
            {
                MakeImage(
                    "Paw Accent",
                    card,
                    "Cozy",
                    "icon_paw",
                    tx,
                    ty,
                    34,
                    34
                );
            }

            controller.speciesText =
                MakeText(
                    "Species (TMP)",
                    card,
                    animal.species,
                    heavy,
                    30,
                    primary,
                    tx +
                        (
                            cozy && !tall
                                ? 42
                                : 0
                        ),
                    ty,
                    cozy && !tall
                        ? tw - 42
                        : (
                            tall
                                ? tw
                                : 360
                        ),
                    39
                );

            controller.groupText =
                MakeText(
                    "Animal Group (TMP)",
                    card,
                    animal.animalGroup,
                    label,
                    14,
                    secondary,
                    tx,
                    ty + 46,
                    tw,
                    20
                );

            controller.groupText
                .characterSpacing =
                1.6f;

            controller.bodyText =
                MakeText(
                    "Dialogue (TMP)",
                    card,
                    animal.greeting,
                    body,
                    tall
                        ? (cozy ? 17 : 18)
                        : 20,
                    primary,
                    tx,
                    tall
                        ? 289
                        : ty + 73,
                    tw,
                    tall
                        ? (cozy ? 84 : 93)
                        : (
                            cozy
                                ? 112
                                : 151
                        )
                );

            controller.bodyText
                .enableAutoSizing =
                true;

            controller.bodyText
                .fontSizeMin =
                16;

            controller.bodyText
                .fontSizeMax =
                tall
                    ? (cozy ? 17 : 18)
                    : 20;

            string[] prompts =
            {
                "What do you eat?",
                "Where do you live?",
                "Tell me something fun!"
            };

            string[] icons =
            {
                "icon_fish",
                "icon_home",
                "icon_sparkles"
            };

            Button[] buttons =
                new Button[3];

            float qx =
                tall
                    ? (cozy ? 64 : 44)
                    : (cozy ? 60 : 36);

            float qy =
                tall
                    ? (cozy ? 384 : 390)
                    : (cozy ? 246 : 274);

            float qw =
                tall
                    ? (cozy ? 292 : 332)
                    : (
                        cozy
                            ? 210
                            : 218.6667f
                    );

            float qh =
                cozy && !tall
                    ? 64
                    : 56;

            float gap =
                cozy
                    ? 5
                    : 16;

            for (int i = 0; i < 3; i++)
            {
                buttons[i] =
                    MakeQuestion(
                        card,
                        style,
                        prompts[i],
                        icons[i],
                        tall
                            ? qx
                            : qx +
                              i *
                              (qw + gap),
                        tall
                            ? qy +
                              i *
                              (
                                  cozy
                                      ? 62
                                      : 68
                              )
                            : qy,
                        qw,
                        qh,
                        heavy,
                        primary,
                        cozy,
                        tall
                    );
            }

            controller.foodButton =
                buttons[0];

            controller.homeButton =
                buttons[1];

            controller.funFactButton =
                buttons[2];

            Button mic =
                MakeQuestion(
                    card,
                    style,
                    "Talk",
                    "icon_mic",
                    tall
                        ? (cozy ? 64 : 44)
                        : (cozy ? 275 : 270),
                    tall
                        ? (cozy ? 580 : 606)
                        : (cozy ? 326 : 350),
                    tall
                        ? (cozy ? 292 : 332)
                        : (cozy ? 210 : 220),
                    56,
                    heavy,
                    primary,
                    cozy,
                    tall,
                    true
                );

            mic.name =
                "Microphone Button (Event Hook)";

            controller.microphoneButton =
                mic;

            Image closeImage =
                MakeImage(
                    "Close Button",
                    card,
                    style,
                    "close_button",
                    tall
                        ? (cozy ? 354 : 350)
                        : (cozy ? 699 : 688),
                    tall
                        ? (cozy ? 4 : 20)
                        : (cozy ? 4 : 28),
                    cozy ? 56 : 44,
                    cozy ? 56 : 44,
                    true
                );

            Button close =
                closeImage
                    .gameObject
                    .AddComponent<Button>();

            close.targetGraphic =
                closeImage;

            close.transition =
                Selectable.Transition.ColorTint;

            ColorBlock colors =
                close.colors;

            colors.highlightedColor =
                new Color(
                    1f,
                    1f,
                    0.88f,
                    1f
                );

            colors.selectedColor =
                colors.highlightedColor;

            colors.pressedColor =
                new Color(
                    0.72f,
                    0.78f,
                    0.67f,
                    1f
                );

            colors.disabledColor =
                new Color(
                    0.55f,
                    0.55f,
                    0.55f,
                    0.7f
                );

            close.colors =
                colors;

            MakeImage(
                "Close Icon",
                closeImage.transform,
                style,
                "icon_close",
                cozy ? 14 : 9,
                cozy ? 10.08f : 9,
                cozy ? 28 : 26,
                cozy ? 28 : 26
            );

            controller.closeButton =
                close;

            SetNavigation(
                new[]
                {
                    buttons[0],
                    buttons[1],
                    buttons[2],
                    mic
                },
                close,
                tall
            );

            controller.RefreshContent();
            fit.FitNow();

            return root;
        }

        private static Button MakeQuestion(
            Transform parent,
            string style,
            string prompt,
            string icon,
            float x,
            float y,
            float width,
            float height,
            TMP_FontAsset font,
            Color color,
            bool cozy,
            bool tall,
            bool microphone = false
        )
        {
            string prefix =
                tall
                    ? "button_tall_"
                    : (
                        cozy
                            ? (
                                microphone
                                    ? "button_mic_"
                                    : "button_wide_"
                            )
                            : "button_"
                    );

            Image face =
                MakeImage(
                    prompt,
                    parent,
                    style,
                    prefix + "default",
                    x,
                    y,
                    width,
                    height,
                    true
                );

            face.preserveAspect =
                false;

            Button button =
                face.gameObject
                    .AddComponent<Button>();

            button.targetGraphic =
                face;

            button.transition =
                Selectable
                    .Transition
                    .SpriteSwap;

            button.spriteState =
                new SpriteState
                {
                    highlightedSprite =
                        Sprite(
                            style,
                            prefix + "hover"
                        ),

                    selectedSprite =
                        Sprite(
                            style,
                            prefix + "hover"
                        ),

                    pressedSprite =
                        Sprite(
                            style,
                            prefix + "pressed"
                        ),

                    disabledSprite =
                        Sprite(
                            style,
                            prefix + "disabled"
                        )
                };

            float iconX;
            float iconY;
            float iconSize;

            float textX;
            float textY;
            float textWidth;
            float textHeight;
            float textSize;

            if (microphone)
            {
                iconX =
                    (width - 84) /
                    2;

                iconY =
                    cozy
                        ? 10
                        : 13;

                iconSize =
                    cozy
                        ? 28
                        : 30;

                textX =
                    iconX +
                    (
                        cozy
                            ? 40
                            : 42
                    );

                textY =
                    0;

                textWidth =
                    60;

                textHeight =
                    cozy
                        ? 50
                        : height - 4;

                textSize =
                    18;
            }
            else if (cozy)
            {
                iconX =
                    tall
                        ? 20
                        : 18;

                iconY =
                    tall
                        ? 11
                        : 15;

                iconSize =
                    26;

                textX =
                    tall
                        ? 56
                        : 48;

                textY =
                    tall
                        ? 0
                        : 2;

                textWidth =
                    tall
                        ? 216
                        : 144;

                textHeight =
                    tall
                        ? 50
                        : 54;

                textSize =
                    tall
                        ? 17
                        : 14.5f;

                if (
                    !tall &&
                    icon ==
                    "icon_sparkles"
                )
                {
                    prompt =
                        "Tell me something\nfun!";
                }
            }
            else
            {
                iconX =
                    14;

                iconY =
                    14;

                iconSize =
                    28;

                textX =
                    tall
                        ? 58
                        : 48;

                textY =
                    0;

                textWidth =
                    width -
                    textX -
                    8;

                textHeight =
                    height -
                    4;

                textSize =
                    tall
                        ? 18
                        : 16;
            }

            MakeImage(
                "Icon",
                face.transform,
                style,
                icon,
                iconX,
                iconY,
                iconSize,
                iconSize
            );

            TMP_Text text =
                MakeText(
                    microphone
                        ? "Talk Label (TMP)"
                        : "Question Label (TMP)",
                    face.transform,
                    prompt,
                    font,
                    textSize,
                    color,
                    textX,
                    textY,
                    textWidth,
                    textHeight
                );

            text.alignment =
                TextAlignmentOptions
                    .MidlineLeft;

            return button;
        }

        private static void SetNavigation(
            Button[] questions,
            Button close,
            bool tall
        )
        {
            for (
                int i = 0;
                i < questions.Length;
                i++
            )
            {
                questions[i].navigation =
                    new Navigation
                    {
                        mode =
                            Navigation.Mode.Explicit,

                        selectOnLeft =
                            i == 0
                                ? close
                                : questions[i - 1],

                        selectOnRight =
                            i ==
                            questions.Length - 1
                                ? close
                                : questions[i + 1],

                        selectOnUp =
                            tall
                                ? (
                                    i == 0
                                        ? close
                                        : questions[i - 1]
                                )
                                : (
                                    i == 3
                                        ? questions[0]
                                        : close
                                ),

                        selectOnDown =
                            tall
                                ? (
                                    i ==
                                    questions.Length - 1
                                        ? close
                                        : questions[i + 1]
                                )
                                : (
                                    i == 3
                                        ? close
                                        : questions[3]
                                )
                    };
            }

            close.navigation =
                new Navigation
                {
                    mode =
                        Navigation.Mode.Explicit,

                    selectOnLeft =
                        questions[
                            questions.Length - 1
                        ],

                    selectOnRight =
                        questions[0],

                    selectOnUp =
                        questions[
                            questions.Length - 1
                        ],

                    selectOnDown =
                        questions[0]
                };
        }

        private static RectTransform Rect(
            string name,
            Transform parent,
            float x,
            float y,
            float w,
            float h
        )
        {
            var gameObject =
                new GameObject(
                    name,
                    typeof(RectTransform)
                );

            gameObject.layer =
                5;

            var rect =
                gameObject
                    .GetComponent<RectTransform>();

            rect.SetParent(
                parent,
                false
            );

            rect.anchorMin =
                rect.anchorMax =
                    new Vector2(
                        0,
                        1
                    );

            rect.pivot =
                new Vector2(
                    0,
                    1
                );

            rect.anchoredPosition =
                new Vector2(
                    x,
                    -y
                );

            rect.sizeDelta =
                new Vector2(
                    w,
                    h
                );

            return rect;
        }

        private static Image MakeImage(
            string name,
            Transform parent,
            string style,
            string asset,
            float x,
            float y,
            float w,
            float h,
            bool raycast = false,
            bool sliced = false
        )
        {
            Image image =
                Rect(
                    name,
                    parent,
                    x,
                    y,
                    w,
                    h
                )
                .gameObject
                .AddComponent<Image>();

            image.sprite =
                Sprite(
                    style,
                    asset
                );

            image.type =
                sliced
                    ? Image.Type.Sliced
                    : Image.Type.Simple;

            image.color =
                Color.white;

            image.raycastTarget =
                raycast;

            return image;
        }

        private static Sprite Sprite(
            string style,
            string name
        )
        {
            string path =
                Root +
                "/Art/" +
                style +
                "/" +
                name +
                "@2x.png";

            Sprite sprite =
                AssetDatabase
                    .LoadAssetAtPath<Sprite>(
                        path
                    );

            if (sprite == null)
            {
                throw new InvalidOperationException(
                    "Missing sprite: " +
                    path
                );
            }

            return sprite;
        }

        private static TMP_Text MakeText(
            string name,
            Transform parent,
            string content,
            TMP_FontAsset font,
            float size,
            Color color,
            float x,
            float y,
            float w,
            float h
        )
        {
            var text =
                Rect(
                    name,
                    parent,
                    x,
                    y,
                    w,
                    h
                )
                .gameObject
                .AddComponent<TextMeshProUGUI>();

            text.font =
                font;

            text.fontSize =
                size;

            text.color =
                color;

            text.text =
                content;

            text.alignment =
                TextAlignmentOptions.TopLeft;

            text.enableWordWrapping =
                true;

            text.overflowMode =
                TextOverflowModes.Ellipsis;

            text.richText =
                false;

            text.raycastTarget =
                false;

            return text;
        }

        private static Color Hex(
            string value
        )
        {
            ColorUtility.TryParseHtmlString(
                "#" + value,
                out Color color
            );

            return color;
        }

        [MenuItem(
            "Tools/Woodland Animal Cards/Check Scene Input (Read Only)"
        )]
        public static void CheckSceneInput()
        {
            EventSystem[] systems =
                Object.FindObjectsByType<EventSystem>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                )
                .Where(
                    item =>
                        item.gameObject
                            .scene
                            .IsValid() &&
                        item.gameObject
                            .scene
                            .isLoaded
                )
                .ToArray();

            string message;

            if (systems.Length == 0)
            {
                message =
                    "No EventSystem found. For a new scene, choose GameObject > UI > Event System. For an Input System project, use Input System UI Input Module, not Standalone Input Module. This check did not create anything.";
            }
            else if (systems.Length > 1)
            {
                message =
                    "Found " +
                    systems.Length +
                    " EventSystems. Usually only one should be active for a single-player UI. Check your scene before testing. Nothing was changed.";
            }
            else if (
                systems[0].GetComponent(
                    "InputSystemUIInputModule"
                ) == null
            )
            {
                message =
                    "An EventSystem exists, but this check did not find Input System UI Input Module. In an Input System project, configure that module on your existing EventSystem. Nothing was changed.";
            }
            else
            {
                message =
                    "Found one EventSystem with Input System UI Input Module. Confirm that it is active and its Point, Left Click, Move and Submit actions are assigned. Test clicks in Play Mode. This structural check cannot prove input works.";
            }

            EditorUtility.DisplayDialog(
                "Woodland input check",
                message,
                "OK"
            );
        }
    }
}