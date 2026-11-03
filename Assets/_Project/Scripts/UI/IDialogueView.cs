using System;

namespace YazLab.UI
{
    public interface IDialogueView
    {
        event Action<string> MessageSubmitted;
        event Action CloseRequested;

        bool IsVisible { get; }
        void Show();
        void Hide();
        void SetThinking(bool thinking);
        void AddPlayerLine(string text);
        void AddNpcLine(string text);
        void AddSystemLine(string text);
    }
}
