using UnityEngine;

public class AnimalExhibit : MonoBehaviour
{
    [Header("Exhibit Info")]

    [SerializeField]
    private string animalTypeName = "Animal";

    [SerializeField]
    private string animalName = "Animal";

    [SerializeField]
    private string species = "Species";

    [SerializeField]
    private string diet = "Food";

    [SerializeField]
    private string habitat = "Habitat";

    [TextArea(2, 4)]
    [SerializeField]
    private string funFact = "Animals are amazing!";


    [Header("Portrait")]

    [SerializeField]
    private Texture2D portrait;


    [Header("AI Voice")]

    [Tooltip(
        "Gemini voice name, for example: Puck, Leda, Aoede, Charon, Sulafat."
    )]
    [SerializeField]
    private string aiVoice = "Kore";


    [Header("Interaction Settings")]

    [SerializeField]
    private float interactionRadius = 3f;

    [SerializeField]
    private Vector3 billboardOffset =
        new Vector3(
            0f,
            2.5f,
            0f
        );


    private bool dialogueOpen;


    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    public string AnimalTypeName =>
        animalTypeName;

    public string AnimalName =>
        animalName;

    public string Species =>
        species;

    public string Diet =>
        diet;

    public string Habitat =>
        habitat;

    public string FunFact =>
        funFact;

    public Texture2D Portrait =>
        portrait;

    public string AIVoice =>
        aiVoice;

    public float InteractionRadius =>
        interactionRadius;

    public bool DialogueOpen =>
        dialogueOpen;

    public Vector3 BillboardWorldPosition =>
        transform.position +
        billboardOffset;

    public string InteractPromptText =>
        $"Talk with the {animalTypeName.ToLower()}";


    // =========================================================
    // DIALOGUE
    // =========================================================

    public void OpenDialogue()
    {
        dialogueOpen = true;
    }


    public void CloseDialogue()
    {
        dialogueOpen = false;
    }
}