using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class ExhibitHUD : MonoBehaviour
{
    [Header("Conversation UI")]

    [SerializeField]
    private GameObject conversationCard;

    [SerializeField]
    private Button conversationCloseButton;


    [Header("Conversation Text")]

    [SerializeField]
    private TMP_Text speciesText;

    [SerializeField]
    private TMP_Text animalGroupText;

    [SerializeField]
    private TMP_Text dialogueText;


    [Header("Animal Portrait")]

    [SerializeField]
    private Image portraitImage;

    [SerializeField]
    private GameObject emptyPortraitHint;


    private AnimalExhibit activeConversationExhibit;

    private NavMeshAgent playerAgent;
    private TapToMove tapToMove;
    private ConversationCamera conversationCamera;
    private AnimalLiveClient animalLiveClient;


    public bool IsConversationActive =>
        activeConversationExhibit != null;

    public AnimalExhibit ActiveConversationExhibit =>
        activeConversationExhibit;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        playerAgent =
            GetComponent<NavMeshAgent>();

        tapToMove =
            GetComponent<TapToMove>();

        animalLiveClient =
            GetComponent<AnimalLiveClient>();

        if (Camera.main != null)
        {
            conversationCamera =
                Camera.main.GetComponent<
                    ConversationCamera
                >();
        }

        if (conversationCard != null)
        {
            conversationCard.SetActive(
                false
            );
        }

        if (conversationCloseButton != null)
        {
            conversationCloseButton
                .onClick
                .AddListener(
                    EndConversation
                );
        }
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (conversationCloseButton != null)
        {
            conversationCloseButton
                .onClick
                .RemoveListener(
                    EndConversation
                );
        }
    }


    // =========================================================
    // START CONVERSATION
    // =========================================================

    public void StartConversation(
        AnimalExhibit exhibit
    )
    {
        if (exhibit == null)
            return;

        if (activeConversationExhibit != null)
        {
            activeConversationExhibit
                .CloseDialogue();
        }

        activeConversationExhibit =
            exhibit;

        activeConversationExhibit
            .OpenDialogue();


        // Stop player movement.
        if (
            playerAgent != null &&
            playerAgent.enabled
        )
        {
            playerAgent.ResetPath();

            playerAgent.velocity =
                Vector3.zero;

            playerAgent.isStopped =
                true;
        }


        // Show card.
        if (conversationCard != null)
        {
            conversationCard.SetActive(
                true
            );
        }


        // Fill card with this animal.
        UpdateConversationCard(
            exhibit
        );


        // Start camera framing.
        if (
            conversationCamera == null &&
            Camera.main != null
        )
        {
            conversationCamera =
                Camera.main.GetComponent<
                    ConversationCamera
                >();
        }

        if (conversationCamera != null)
        {
            conversationCamera
                .BeginConversation(
                    exhibit
                );
        }


        // Prepare Gemini Live connection.
        if (animalLiveClient != null)
        {
            animalLiveClient
                .PrepareSession(
                    exhibit
                );
        }
    }


    // =========================================================
    // UPDATE CARD CONTENT
    // =========================================================

    private void UpdateConversationCard(
        AnimalExhibit exhibit
    )
    {
        if (exhibit == null)
            return;


        string animalType =
            exhibit.AnimalTypeName;

        if (
            string.IsNullOrWhiteSpace(
                animalType
            )
        )
        {
            animalType = "Animal";
        }


        string species =
            exhibit.Species;

        if (
            string.IsNullOrWhiteSpace(
                species
            )
        )
        {
            species = "Animal";
        }


        // -----------------------------------------------------
        // TEXT
        // -----------------------------------------------------

        if (speciesText != null)
        {
            speciesText.text =
                animalType.ToUpper();
        }


        if (animalGroupText != null)
        {
            animalGroupText.text =
                species.ToUpper();
        }


        if (dialogueText != null)
        {
            dialogueText.text =
                $"Hi! I'm a {animalType.ToLower()}. " +
                "Hold the microphone button and ask me something!";
        }


        // -----------------------------------------------------
        // PORTRAIT
        // -----------------------------------------------------

        if (portraitImage != null)
        {
            if (exhibit.Portrait != null)
            {
                Texture2D texture =
                    exhibit.Portrait;

                Sprite portraitSprite =
                    Sprite.Create(
                        texture,
                        new Rect(
                            0f,
                            0f,
                            texture.width,
                            texture.height
                        ),
                        new Vector2(
                            0.5f,
                            0.5f
                        ),
                        100f
                    );

                portraitImage.sprite =
                    portraitSprite;

                portraitImage.enabled =
                    true;

                portraitImage.preserveAspect =
                    true;
            }
            else
            {
                portraitImage.sprite =
                    null;

                portraitImage.enabled =
                    false;
            }
        }


        if (emptyPortraitHint != null)
        {
            emptyPortraitHint.SetActive(
                exhibit.Portrait == null
            );
        }
    }


    // =========================================================
    // END CONVERSATION
    // =========================================================

    public void EndConversation()
    {
        // Stop Gemini Live.
        if (animalLiveClient != null)
        {
            animalLiveClient
                .StopLiveConversation();
        }


        // Clear animal state.
        if (activeConversationExhibit != null)
        {
            activeConversationExhibit
                .CloseDialogue();

            activeConversationExhibit =
                null;
        }


        // Hide UI.
        if (conversationCard != null)
        {
            conversationCard.SetActive(
                false
            );
        }


        // Return camera.
        if (conversationCamera != null)
        {
            conversationCamera
                .EndConversation();
        }


        // Restore movement.
        if (
            playerAgent != null &&
            playerAgent.enabled
        )
        {
            playerAgent.ResetPath();

            playerAgent.velocity =
                Vector3.zero;

            playerAgent.isStopped =
                false;
        }


        // Reset tap state.
        if (tapToMove != null)
        {
            tapToMove
                .ResetInteractionState();
        }
    }
}