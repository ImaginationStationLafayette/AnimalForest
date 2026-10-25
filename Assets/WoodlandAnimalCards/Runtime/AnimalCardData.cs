using UnityEngine;

namespace WoodlandAnimalCards
{
    /// <summary>One editable animal. The same data can be used by either visual style.</summary>
    [CreateAssetMenu(fileName = "Animal", menuName = "Woodland/Animal Card Data")]
    public sealed class AnimalCardData : ScriptableObject
    {
        public string species = "Cat";
        public string animalGroup = "MAMMAL";
        [TextArea(3, 6)] public string greeting;
        [Tooltip("Import your image as Sprite (2D and UI), then drop it here.")]
        public Sprite portrait;
        [TextArea(3, 6)] public string foodAnswer;
        [TextArea(3, 6)] public string homeAnswer;
        [TextArea(3, 6)] public string funFactAnswer;

        public string GetAnswer(int questionIndex)
        {
            switch (questionIndex)
            {
                case 0: return foodAnswer;
                case 1: return homeAnswer;
                case 2: return funFactAnswer;
                default: return string.Empty;
            }
        }
    }
}
