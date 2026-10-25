using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class ExhibitHUD : MonoBehaviour
{
    [Header("Conversation UI")]
    [SerializeField] private GameObject conversationCard;

    [Tooltip("The X / Close button on the conversation card.")]
    [SerializeField] private Button conversationCloseButton;

    [Header("Conversation Text")]
    [SerializeField] private TMP_Text speciesText;
    [SerializeField] private TMP_Text animalGroupText;
    [SerializeField] private TMP_Text dialogueText;

    private AnimalExhibit activeConversationExhibit;

    private NavMeshAgent playerAgent;
    private TapToMove tapToMove;
    private ConversationCamera conversationCamera;

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

        if (Camera.main != null)
        {
            conversationCamera =
                Camera.main.GetComponent<ConversationCamera>();
        }

        // IMPORTANT:
        // This should reference the INNER "Animal Card",
        // not the outer AnimalConversationCard canvas.
        if (conversationCard != null)
        {
            conversationCard.SetActive(false);
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

        // Clear any previous dialogue state.
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

        // -----------------------------------------------------
        // SHOW CARD
        // -----------------------------------------------------

        if (conversationCard != null)
        {
            conversationCard.SetActive(
                true
            );
        }

        // Update text for tapped animal.
        UpdateConversationCard(
            exhibit
        );

        // -----------------------------------------------------
        // CAMERA
        // -----------------------------------------------------

        if (conversationCamera == null)
        {
            if (Camera.main != null)
            {
                conversationCamera =
                    Camera.main
                        .GetComponent<ConversationCamera>();
            }
        }

        if (conversationCamera != null)
        {
            conversationCamera
                .BeginConversation(
                    exhibit
                );
        }
    }

    // =========================================================
    // UPDATE CARD
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
            animalType =
                "Animal";
        }

        string species =
            exhibit.Species;

        if (
            string.IsNullOrWhiteSpace(
                species
            )
        )
        {
            species =
                "Animal";
        }

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
                "What would you like to know about me?";
        }
    }

    // =========================================================
    // END CONVERSATION
    // =========================================================

    public void EndConversation()
    {
        // -----------------------------------------------------
        // CLEAR ANIMAL STATE
        // -----------------------------------------------------

        if (activeConversationExhibit != null)
        {
            activeConversationExhibit
                .CloseDialogue();

            activeConversationExhibit =
                null;
        }

        // -----------------------------------------------------
        // HIDE CARD
        // -----------------------------------------------------

        if (conversationCard != null)
        {
            conversationCard.SetActive(
                false
            );
        }

        // -----------------------------------------------------
        // RETURN CAMERA
        // -----------------------------------------------------

        if (conversationCamera != null)
        {
            conversationCamera
                .EndConversation();
        }

        // -----------------------------------------------------
        // RE-ENABLE PLAYER
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // RESET TAP STATE
        // -----------------------------------------------------

        if (tapToMove != null)
        {
            tapToMove
                .ResetInteractionState();
        }
    }
}