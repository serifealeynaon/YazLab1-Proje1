using System;
using System.Threading;
using UnityEngine;
using YazLab.LLM;
using YazLab.UI;

namespace YazLab.Game
{
    public class NpcDialoguePresenter : MonoBehaviour
    {
        public const string SealHeldContext = "Oyun durumu: mühür VAR.";
        public const string SealMissingContext = "Oyun durumu: mühür YOK.";

        [SerializeField] private OllamaSettings settings = new OllamaSettings();
        [SerializeField] private DialogueView dialogueView;
        [SerializeField] private NpcGuard guard;

        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private IDialogueView view;
        private ILlmDialogueService service;

        public OllamaSettings Settings => settings;
        public bool IsOpen => view.IsVisible;

        private void Awake()
        {
            view = dialogueView;
            service = new OllamaDialogueService(settings, new JsonNpcResponseParser());
            view.MessageSubmitted += OnMessageSubmitted;
            view.CloseRequested += Close;
        }

        private async void Start()
        {
            await service.PreloadAsync(lifetime.Token);
        }

        private void OnDestroy()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }

        public void Open()
        {
            view.Show();
        }

        public void Close()
        {
            view.Hide();
        }

        private async void OnMessageSubmitted(string message)
        {
            view.AddPlayerLine(message);
            view.SetThinking(true);

            try
            {
                service.SetContext(guard.HasSeal ? SealHeldContext : SealMissingContext);
                LlmResult result = await service.SendAsync(message, lifetime.Token);

                if (result.Status == LlmStatus.Cancelled)
                {
                    return;
                }

                if (result.IsSuccess)
                {
                    if (result.Action.door_unlocked && !guard.HasSeal)
                    {
                        view.AddSystemLine("Kapı açılmadı: önce kralın mührünü bulmalısın.");
                    }
                    else
                    {
                        view.AddNpcLine(result.Action.reply);
                    }

                    guard.Apply(result.Action);
                }
                else
                {
                    view.AddSystemLine(result.ErrorMessage);
                }

                view.SetThinking(false);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                Debug.LogError($"[Diyalog] Beklenmeyen hata: {exception}");
                view.AddSystemLine("Beklenmeyen bir hata oluştu. Oyun devam ediyor.");
                view.SetThinking(false);
            }
            catch (OperationCanceledException) { }
        }
    }
}
