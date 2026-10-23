using UnityEngine;

public class ExhibitHUD : MonoBehaviour
{
    [Header("UI Textures (from Assets/UI)")]
    [SerializeField] private Texture2D interactionPromptTex;
    [SerializeField] private Texture2D micIconTex;

    [Header("Speech Bubble Settings")]
    [Tooltip("Font size for the text inside the prompt bubble.")]
    [SerializeField] private int promptFontSize = 11;
    [Tooltip("Minimum width of the speech bubble.")]
    [SerializeField] private float promptMinWidth = 175f;
    [Tooltip("Minimum height of the speech bubble.")]
    [SerializeField] private float promptMinHeight = 48f;
    [Tooltip("Extra horizontal padding around the text.")]
    [SerializeField] private float promptPaddingX = 54f;
    [Tooltip("Extra vertical padding around the text.")]
    [SerializeField] private float promptPaddingY = 22f;
    [Tooltip("Fine-tune text position inside the bubble (X moves horizontally, Y moves vertically). Positive Y moves down, negative Y moves up.")]
    [SerializeField] private Vector2 textOffset = Vector2.zero;

    [Header("Microphone Popup Settings")]
    [Tooltip("Size (width & height) of the microphone icon in pixels.")]
    [SerializeField] private float microphoneSize = 64f;
    [Tooltip("Distance from the bottom of the screen in pixels.")]
    [SerializeField] private float microphoneBottomMargin = 40f;

    private GUIStyle promptTextStyle;
    private GUIStyle micButtonStyle;
    private bool stylesInitialized;

    private AnimalExhibit activeExhibit;
    private AnimalExhibit openDialogueExhibit;

    private Rect micRect;

    public bool IsMicActive => openDialogueExhibit != null;

    private void Awake()
    {
        LoadTexturesIfNeeded();
    }

    private void LoadTexturesIfNeeded()
    {
        if (interactionPromptTex == null)
        {
            interactionPromptTex = Resources.Load<Texture2D>("interaction_prompt@2x");
#if UNITY_EDITOR
            if (interactionPromptTex == null)
            {
                interactionPromptTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/interaction_prompt@2x.png");
            }
#endif
        }

        if (micIconTex == null)
        {
            micIconTex = Resources.Load<Texture2D>("icon_mic@2x");
#if UNITY_EDITOR
            if (micIconTex == null)
            {
                micIconTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/icon_mic@2x.png");
            }
#endif
        }
    }

    public void OpenMicrophone(AnimalExhibit exhibit)
    {
        openDialogueExhibit = exhibit;
        if (exhibit != null)
        {
            exhibit.OpenDialogue();
        }
    }

    public void CloseMicrophone()
    {
        if (openDialogueExhibit != null)
        {
            openDialogueExhibit.CloseDialogue();
            openDialogueExhibit = null;
        }
    }

    public bool IsScreenPointOverMic(Vector2 screenPoint)
    {
        if (!IsMicActive)
            return false;

        float guiY = Screen.height - screenPoint.y;
        return micRect.Contains(new Vector2(screenPoint.x, guiY));
    }

    private void OnGUI()
    {
        InitStylesIfNeeded();

        // 1. Find nearest animal within range
        AnimalExhibit nearest = null;
        float nearestDist = float.MaxValue;

        var exhibits = FindObjectsByType<AnimalExhibit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (var ex in exhibits)
        {
            if (ex.PlayerInRange)
            {
                float distance = Vector3.Distance(transform.position, ex.transform.position);
                if (distance < nearestDist)
                {
                    nearestDist = distance;
                    nearest = ex;
                }
            }
        }

        activeExhibit = nearest;

        // If player walked away from the open exhibit, close microphone
        if (openDialogueExhibit != null && (!openDialogueExhibit.PlayerInRange || !openDialogueExhibit.DialogueOpen))
        {
            openDialogueExhibit = null;
        }

        // 2. Draw animal popup prompt above their head when player is near and mic is not open for it
        if (activeExhibit != null && activeExhibit != openDialogueExhibit)
        {
            DrawFloatingWorldPrompt(activeExhibit);
        }

        // 3. Draw microphone popup in the middle of screen if open
        if (openDialogueExhibit != null)
        {
            DrawMicrophonePopup(openDialogueExhibit);
        }
    }

    private void DrawFloatingWorldPrompt(AnimalExhibit exhibit)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = exhibit.BillboardWorldPosition;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z > 0.5f)
        {
            float guiY = Screen.height - screenPos.y;
            string text = exhibit.InteractPromptText;

            promptTextStyle.fontSize = promptFontSize;

            GUIContent content = new GUIContent(text);
            Vector2 textSize = promptTextStyle.CalcSize(content);

            float bubbleWidth = Mathf.Max(textSize.x + promptPaddingX, promptMinWidth);
            float bubbleHeight = Mathf.Max(textSize.y + promptPaddingY, promptMinHeight);

            Rect bubbleRect = new Rect(
                screenPos.x - bubbleWidth * 0.5f,
                guiY - bubbleHeight * 0.5f,
                bubbleWidth,
                bubbleHeight
            );

            // Draw prompt background from Assets/UI/interaction_prompt@2x.png
            if (interactionPromptTex != null)
            {
                GUI.DrawTexture(bubbleRect, interactionPromptTex, ScaleMode.StretchToFill);
            }
            else
            {
                GUI.Box(bubbleRect, GUIContent.none);
            }

            // Draw label with independent offset
            Rect textRect = new Rect(
                bubbleRect.x + textOffset.x,
                bubbleRect.y + textOffset.y,
                bubbleRect.width,
                bubbleRect.height
            );
            GUI.Label(textRect, content, promptTextStyle);
        }
    }

    private void DrawMicrophonePopup(AnimalExhibit exhibit)
    {
        float micSize = microphoneSize;
        float centerX = Screen.width * 0.5f;
        float micY = Screen.height - microphoneBottomMargin - micSize;

        micRect = new Rect(centerX - micSize * 0.5f, micY, micSize, micSize);

        // Draw microphone button using Assets/UI/icon_mic@2x.png
        if (micIconTex != null)
        {
            GUI.DrawTexture(micRect, micIconTex, ScaleMode.ScaleToFit);
        }
        else
        {
            GUI.Box(micRect, "MIC", micButtonStyle);
        }

        // Subtitle above mic
        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerCenter,
            fontSize = 12,
            fontStyle = FontStyle.Normal
        };
        subStyle.normal.textColor = new Color(1f, 0.95f, 0.7f, 0.9f);

        Rect subRect = new Rect(centerX - 150f, micY - 24f, 300f, 22f);
        GUI.Label(subRect, $"Tap outside to move", subStyle);
    }

    private void InitStylesIfNeeded()
    {
        if (stylesInitialized) return;

        LoadTexturesIfNeeded();

        promptTextStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            wordWrap = false
        };
        promptTextStyle.normal.textColor = new Color(0.12f, 0.15f, 0.22f);

        micButtonStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        stylesInitialized = true;
    }
}