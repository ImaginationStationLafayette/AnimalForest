using UnityEngine;
using UnityEngine.InputSystem;

public class AnimalExhibit : MonoBehaviour
{
    [Header("Exhibit Info")]
    [Tooltip("General animal type/name used in prompts (e.g. 'Deer', 'Tiger').")]
    [SerializeField] private string animalTypeName = "Animal";

    [Tooltip("Friendly display name for kids (e.g. 'Bambi the Deer').")]
    [SerializeField] private string animalName = "Animal";

    [Tooltip("Biological species name.")]
    [SerializeField] private string species = "Species";

    [Tooltip("Diet type (Herbivore, Carnivore, Omnivore).")]
    [SerializeField] private string diet = "Herbivore";

    [Tooltip("Natural habitat.")]
    [SerializeField] private string habitat = "Forest";

    [TextArea(2, 4)]
    [Tooltip("Kid-friendly fun fact.")]
    [SerializeField] private string funFact = "Animals are amazing!";

    [Header("Interaction Settings")]
    [Tooltip("Distance at which the kid can interact with this animal exhibit.")]
    [SerializeField] private float interactionRadius = 3.0f;
    [SerializeField] private Vector3 billboardOffset = new Vector3(0f, 2.5f, 0f);

    private Animator animator;
    private Transform playerTransform;
    private bool playerInRange;
    private bool dialogueOpen;

    public string AnimalTypeName => animalTypeName;
    public string AnimalName => animalName;
    public string Species => species;
    public string Diet => diet;
    public string Habitat => habitat;
    public string FunFact => funFact;
    public bool PlayerInRange => playerInRange;
    public bool DialogueOpen => dialogueOpen;
    public Vector3 BillboardWorldPosition => transform.position + billboardOffset;
    public string InteractPromptText => $"Talk with the {animalTypeName.ToLower()}";

    public void OpenDialogue()
    {
        dialogueOpen = true;
    }

    public void CloseDialogue()
    {
        dialogueOpen = false;
    }

    public void Setup(string typeName, string name, string spec, string animalDiet, string hab, string fact, Vector3 uiOffset, float radius = 3.0f)
    {
        animalTypeName = typeName;
        animalName = name;
        species = spec;
        diet = animalDiet;
        habitat = hab;
        funFact = fact;
        billboardOffset = uiOffset;
        interactionRadius = radius;
    }

    private void Awake()
    {
        // 1. Disable and remove movement and player input scripts so the animal stays firmly where placed
        var mover = GetComponent("CreatureMover");
        if (mover != null)
        {
            if (mover is Behaviour b) b.enabled = false;
            Destroy(mover);
        }

        var playerInputComp = GetComponent("MovePlayerInput");
        if (playerInputComp != null)
        {
            if (playerInputComp is Behaviour b) b.enabled = false;
            Destroy(playerInputComp);
        }

        // Disable CharacterController gravity/collision interference
        var cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            Destroy(cc);
        }

        // 2. Ensure animator stays active playing idle
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetFloat("Vert", 0f);
            animator.SetFloat("State", 0f);
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                var controller = Object.FindAnyObjectByType<ThirdPersonController>();
                if (controller != null) playerObj = controller.gameObject;
            }
            if (playerObj != null) playerTransform = playerObj.transform;
            return;
        }

        // Horizontal distance so vertical elevation difference (ridge, plinth) doesn't prevent interaction
        Vector3 animalGroundPos = transform.position;
        Vector3 playerGroundPos = playerTransform.position;
        animalGroundPos.y = 0f;
        playerGroundPos.y = 0f;

        float horizontalDist = Vector3.Distance(animalGroundPos, playerGroundPos);
        float verticalDist = Mathf.Abs(transform.position.y - playerTransform.position.y);
        bool inRangeNow = (horizontalDist <= interactionRadius) && (verticalDist < 4.0f);

        if (inRangeNow != playerInRange)
        {
            playerInRange = inRangeNow;
            if (!playerInRange)
            {
                dialogueOpen = false;
            }
        }

        // Check for interaction key 'E' or Gamepad West
        if (playerInRange)
        {
            bool interactPressed = false;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }
            if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                interactPressed = true;
            }

            if (interactPressed)
            {
                dialogueOpen = !dialogueOpen;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
