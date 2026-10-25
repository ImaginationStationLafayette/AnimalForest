using UnityEngine;
using UnityEngine.AI;

public class ExhibitHUD : MonoBehaviour
{
    [Header("UI Textures")]
    [SerializeField] private Texture2D interactionPromptTex;

    [Header("Prompt Settings")]
    [SerializeField] private int promptFontSize = 11;

    [SerializeField] private float promptMinWidth = 175f;
    [SerializeField] private float promptMinHeight = 48f;

    [SerializeField] private float promptPaddingX = 54f;
    [SerializeField] private float promptPaddingY = 22f;

    [SerializeField] private Vector2 textOffset =
        Vector2.zero;

    private GUIStyle promptTextStyle;
    private GUIStyle conversationTitleStyle;
    private GUIStyle conversationBodyStyle;

    private bool stylesInitialized;

    private AnimalExhibit nearbyExhibit;
    private AnimalExhibit activeConversationExhibit;

    private NavMeshAgent playerAgent;

    public bool IsConversationActive =>
        activeConversationExhibit != null;

    public AnimalExhibit ActiveConversationExhibit =>
        activeConversationExhibit;

    private void Awake()
    {
        playerAgent =
            GetComponent<NavMeshAgent>();

        LoadTexturesIfNeeded();
    }

    private void Update()
    {
        FindNearestExhibit();

        // If the player somehow leaves the animal's range,
        // automatically close the conversation.
        if (
            activeConversationExhibit != null &&
            !activeConversationExhibit.PlayerInRange
        )
        {
            EndConversation();
        }
    }

    private void OnGUI()
    {
        InitStylesIfNeeded();

        // Nearby greeting prompt.
        if (
            nearbyExhibit != null &&
            !IsConversationActive
        )
        {
            DrawFloatingWorldPrompt(
                nearbyExhibit
            );
        }

        // Temporary conversation panel.
        if (IsConversationActive)
        {
            DrawTemporaryConversationUI();
        }
    }

    private void FindNearestExhibit()
    {
        AnimalExhibit nearest = null;
        float nearestDistance =
            float.MaxValue;

        AnimalExhibit[] exhibits =
            FindObjectsByType<AnimalExhibit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (AnimalExhibit exhibit in exhibits)
        {
            if (!exhibit.PlayerInRange)
                continue;

            float distance =
                Vector3.Distance(
                    transform.position,
                    exhibit.transform.position
                );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = exhibit;
            }
        }

        nearbyExhibit = nearest;
    }

    public void StartConversation(
        AnimalExhibit exhibit
    )
    {
        if (exhibit == null)
            return;

        if (!exhibit.PlayerInRange)
            return;

        // Close any previous conversation.
        if (
            activeConversationExhibit != null &&
            activeConversationExhibit != exhibit
        )
        {
            activeConversationExhibit
                .CloseDialogue();
        }

        activeConversationExhibit =
            exhibit;

        activeConversationExhibit
            .OpenDialogue();

        // Stop the player during the conversation.
        if (playerAgent != null)
        {
            playerAgent.ResetPath();
            playerAgent.isStopped = true;
        }
    }

    public void EndConversation()
    {
        if (activeConversationExhibit != null)
        {
            activeConversationExhibit
                .CloseDialogue();

            activeConversationExhibit =
                null;
        }

        // Allow movement again.
        if (playerAgent != null)
        {
            playerAgent.isStopped = false;
        }
    }

    private void DrawFloatingWorldPrompt(
        AnimalExhibit exhibit
    )
    {
        Camera cam =
            Camera.main;

        if (cam == null)
            return;

        Vector3 screenPosition =
            cam.WorldToScreenPoint(
                exhibit.BillboardWorldPosition
            );

        if (screenPosition.z <= 0.5f)
            return;

        float guiY =
            Screen.height -
            screenPosition.y;

        string text =
            exhibit.InteractPromptText;

        promptTextStyle.fontSize =
            promptFontSize;

        GUIContent content =
            new GUIContent(text);

        Vector2 textSize =
            promptTextStyle
                .CalcSize(content);

        float bubbleWidth =
            Mathf.Max(
                textSize.x +
                promptPaddingX,
                promptMinWidth
            );

        float bubbleHeight =
            Mathf.Max(
                textSize.y +
                promptPaddingY,
                promptMinHeight
            );

        Rect bubbleRect =
            new Rect(
                screenPosition.x -
                bubbleWidth * 0.5f,
                guiY -
                bubbleHeight * 0.5f,
                bubbleWidth,
                bubbleHeight
            );

        if (interactionPromptTex != null)
        {
            GUI.DrawTexture(
                bubbleRect,
                interactionPromptTex,
                ScaleMode.StretchToFill
            );
        }
        else
        {
            GUI.Box(
                bubbleRect,
                GUIContent.none
            );
        }

        Rect textRect =
            new Rect(
                bubbleRect.x +
                textOffset.x,
                bubbleRect.y +
                textOffset.y,
                bubbleRect.width,
                bubbleRect.height
            );

        GUI.Label(
            textRect,
            content,
            promptTextStyle
        );
    }

    private void DrawTemporaryConversationUI()
    {
        if (activeConversationExhibit == null)
            return;

        float width = 440f;
        float height = 185f;

        float x =
            (Screen.width - width) * 0.5f;

        float y =
            Screen.height - height - 35f;

        Rect panelRect =
            new Rect(
                x,
                y,
                width,
                height
            );

        GUI.Box(
            panelRect,
            GUIContent.none
        );

        // Title
        GUI.Label(
            new Rect(
                x + 22f,
                y + 20f,
                width - 44f,
                30f
            ),
            $"Hi! I'm a {activeConversationExhibit.AnimalTypeName.ToLower()}.",
            conversationTitleStyle
        );

        // Message
        GUI.Label(
            new Rect(
                x + 22f,
                y + 58f,
                width - 44f,
                30f
            ),
            "Would you like to chat with me?",
            conversationBodyStyle
        );

        // Large explicit close button
        Rect closeButtonRect =
            new Rect(
                x + 40f,
                y + 110f,
                width - 80f,
                48f
            );

        if (
            GUI.Button(
                closeButtonRect,
                "Close conversation"
            )
        )
        {
            EndConversation();
        }
    }

    private void InitStylesIfNeeded()
    {
        if (stylesInitialized)
            return;

        LoadTexturesIfNeeded();

        promptTextStyle =
            new GUIStyle(
                GUI.skin.label
            )
            {
                alignment =
                    TextAnchor.MiddleCenter,

                fontSize =
                    promptFontSize,

                fontStyle =
                    FontStyle.Bold,

                wordWrap =
                    false
            };

        promptTextStyle
            .normal
            .textColor =
            new Color(
                0.12f,
                0.15f,
                0.22f
            );

        conversationTitleStyle =
            new GUIStyle(
                GUI.skin.label
            )
            {
                alignment =
                    TextAnchor.MiddleLeft,

                fontSize = 18,

                fontStyle =
                    FontStyle.Bold
            };

        conversationTitleStyle
            .normal
            .textColor =
            Color.white;

        conversationBodyStyle =
            new GUIStyle(
                GUI.skin.label
            )
            {
                alignment =
                    TextAnchor.MiddleLeft,

                fontSize = 14,

                wordWrap =
                    true
            };

        conversationBodyStyle
            .normal
            .textColor =
            Color.white;

        stylesInitialized = true;
    }

    private void LoadTexturesIfNeeded()
    {
        if (interactionPromptTex != null)
            return;

        interactionPromptTex =
            Resources.Load<Texture2D>(
                "interaction_prompt@2x"
            );

#if UNITY_EDITOR
        if (interactionPromptTex == null)
        {
            interactionPromptTex =
                UnityEditor.AssetDatabase
                    .LoadAssetAtPath<Texture2D>(
                        "Assets/UI/interaction_prompt@2x.png"
                    );
        }
#endif
    }
}