using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WoodlandAnimalCards
{
    /// <summary>
    /// UI only: no networking, audio recording, AI, camera or gameplay-input changes.
    /// Buttons bind exactly once while enabled and preserve your own listeners.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimalDialogueCard : MonoBehaviour
    {
        [Serializable] public sealed class QuestionEvent : UnityEvent<int> { }

        [Header("Content")]
        public AnimalCardData animal;
        [Tooltip("If enabled, clicking a question shows its editable sample answer.")]
        public bool showSampleAnswers = true;

        [Header("Live text and portrait references")]
        public TMP_Text speciesText;
        public TMP_Text groupText;
        public TMP_Text bodyText;
        public Image portraitImage;
        public GameObject emptyPortraitHint;

        [Header("Real Unity UI Buttons")]
        public Button foodButton;
        public Button homeButton;
        public Button funFactButton;
        public Button microphoneButton;
        public Button closeButton;

        [Header("Connect your game here (Inspector + button)")]
        public UnityEvent onFoodQuestion = new UnityEvent();
        public UnityEvent onHomeQuestion = new UnityEvent();
        public UnityEvent onFunFactQuestion = new UnityEvent();
        [Tooltip("0 = food, 1 = home, 2 = fun fact.")]
        public QuestionEvent onQuestionSelected = new QuestionEvent();
        [Tooltip("UI hook only. No recording, audio permissions or speech service is included.")]
        public UnityEvent onMicrophonePressed = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();

        private bool listenersBound;

        private void OnEnable()
        {
            BindButtons();
            RefreshContent();
        }

        private void OnDisable() { UnbindButtons(); }

        private void BindButtons()
        {
            if (listenersBound) return;
            if (foodButton != null) foodButton.onClick.AddListener(AskFood);
            if (homeButton != null) homeButton.onClick.AddListener(AskHome);
            if (funFactButton != null) funFactButton.onClick.AddListener(AskFunFact);
            if (microphoneButton != null) microphoneButton.onClick.AddListener(PressMicrophone);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            listenersBound = true;
        }

        private void UnbindButtons()
        {
            if (!listenersBound) return;
            if (foodButton != null) foodButton.onClick.RemoveListener(AskFood);
            if (homeButton != null) homeButton.onClick.RemoveListener(AskHome);
            if (funFactButton != null) funFactButton.onClick.RemoveListener(AskFunFact);
            if (microphoneButton != null) microphoneButton.onClick.RemoveListener(PressMicrophone);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            listenersBound = false;
        }

        public void SetAnimal(AnimalCardData value)
        {
            animal = value;
            RefreshContent();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshContent();
        }

        public void ShowAnimal(AnimalCardData value)
        {
            animal = value;
            Show();
        }

        [ContextMenu("Refresh From Animal Data")]
        public void RefreshContent()
        {
            // With no data assigned, leave manually edited TMP text alone.
            if (animal == null) return;
            if (speciesText != null) speciesText.text = animal.species;
            if (groupText != null) groupText.text = animal.animalGroup;
            SetBodyText(animal.greeting);
            SetPortrait(animal.portrait);
        }

        public void SetBodyText(string text)
        {
            if (bodyText != null) bodyText.text = text ?? string.Empty;
        }

        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage != null)
            {
                portraitImage.sprite = sprite;
                portraitImage.preserveAspect = true;
                portraitImage.enabled = sprite != null;
            }
            if (emptyPortraitHint != null) emptyPortraitHint.SetActive(sprite == null);
        }

        public void AskFood() { Ask(0); }
        public void AskHome() { Ask(1); }
        public void AskFunFact() { Ask(2); }
        public void PressMicrophone() { onMicrophonePressed.Invoke(); }

        private void Ask(int index)
        {
            if (showSampleAnswers && animal != null)
            {
                string answer = animal.GetAnswer(index);
                if (!string.IsNullOrWhiteSpace(answer)) SetBodyText(answer);
            }
            switch (index)
            {
                case 0: onFoodQuestion.Invoke(); break;
                case 1: onHomeQuestion.Invoke(); break;
                case 2: onFunFactQuestion.Invoke(); break;
            }
            onQuestionSelected.Invoke(index);
        }

        public void Close()
        {
            // Hide only this card. Leave its Canvas and all game systems alone.
            gameObject.SetActive(false);
            onClosed.Invoke();
        }
    }
}
