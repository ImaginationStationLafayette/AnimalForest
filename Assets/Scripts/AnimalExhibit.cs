using UnityEngine;

public class AnimalExhibit : MonoBehaviour
{
    [Header("Exhibit Info")]
    [SerializeField] private string animalTypeName = "Animal";
    [SerializeField] private string animalName = "Animal";
    [SerializeField] private string species = "Animal";
    [SerializeField] private string diet = "Food";
    [SerializeField] private string habitat = "Habitat";

    [TextArea(2, 4)]
    [SerializeField] private string funFact = "Animals are amazing!";

    [Header("Interaction")]
    [Tooltip("A safe point on the NavMesh where the player stands to talk.")]
    [SerializeField] private Transform interactionPoint;

    [SerializeField] private float interactionRadius = 1.2f;

    [SerializeField] private Vector3 billboardOffset =
        new Vector3(0f, 2.5f, 0f);

    private bool dialogueOpen;

    public string AnimalTypeName => animalTypeName;
    public string AnimalName => animalName;
    public string Species => species;
    public string Diet => diet;
    public string Habitat => habitat;
    public string FunFact => funFact;

    public Transform InteractionPoint => interactionPoint;

    public float InteractionRadius => interactionRadius;

    public bool DialogueOpen => dialogueOpen;

    public Vector3 BillboardWorldPosition =>
        transform.position + billboardOffset;

    public string InteractPromptText =>
        $"Talk with the {animalTypeName.ToLower()}";

    public void OpenDialogue()
    {
        dialogueOpen = true;
    }

    public void CloseDialogue()
    {
        dialogueOpen = false;
    }
}