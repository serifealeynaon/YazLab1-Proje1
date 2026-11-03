using System;
using UnityEngine;
using UnityEngine.UI;

namespace YazLab.UI
{
    public class DialogueView : MonoBehaviour, IDialogueView
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text logText;
        [SerializeField] private Text thinkingText;
        [SerializeField] private InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button closeButton;

        private bool thinking;

        public event Action<string> MessageSubmitted;
        public event Action CloseRequested;

        public bool IsVisible => panel.activeSelf;

        private void Awake()
        {
            sendButton.onClick.AddListener(Submit);
            closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            thinkingText.text = string.Empty;
            panel.SetActive(false);
        }

        private void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            if (thinking)
            {
                int dots = Mathf.FloorToInt(Time.unscaledTime * 3f) % 4;
                thinkingText.text = "Muhafız düşünüyor" + new string('.', dots);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseRequested?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Submit();
            }
        }

        public void Show()
        {
            panel.SetActive(true);
            FocusInput();
        }

        public void Hide()
        {
            panel.SetActive(false);
        }

        public void SetThinking(bool value)
        {
            thinking = value;
            sendButton.interactable = !value;
            inputField.interactable = !value;

            if (!value)
            {
                thinkingText.text = string.Empty;
                FocusInput();
            }
        }

        public void AddPlayerLine(string text)
        {
            Append($"<color=#7FC8FF><b>Sen:</b></color> {Sanitize(text)}");
        }

        public void AddNpcLine(string text)
        {
            Append($"<color=#D9A6FF><b>Muhafız:</b></color> {Sanitize(text)}");
        }

        public void AddSystemLine(string text)
        {
            Append($"<color=#FF9E80><b>[Sistem]</b> {Sanitize(text)}</color>");
        }

        private void Submit()
        {
            if (thinking || string.IsNullOrWhiteSpace(inputField.text))
            {
                return;
            }

            string message = inputField.text.Trim();
            inputField.text = string.Empty;
            MessageSubmitted?.Invoke(message);
        }

        private void Append(string line)
        {
            logText.text = string.IsNullOrEmpty(logText.text) ? line : $"{logText.text}\n\n{line}";
        }

        private void FocusInput()
        {
            inputField.ActivateInputField();
        }

        private static string Sanitize(string text)
        {
            return text.Replace("<", "‹").Replace(">", "›");
        }
    }
}
