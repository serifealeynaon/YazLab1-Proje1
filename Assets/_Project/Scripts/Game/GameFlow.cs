using UnityEngine;
using YazLab.Companion;
using YazLab.Interaction;
using YazLab.Player;
using YazLab.UI;

namespace YazLab.Game
{
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private CompanionController companion;
        [SerializeField] private SwitchInteractable switchTarget;
        [SerializeField] private Door firstDoor;
        [SerializeField] private Door secondDoor;
        [SerializeField] private NpcDialoguePresenter dialogue;
        [SerializeField] private FinishZone finishZone;
        [SerializeField] private HudView hud;
        [SerializeField] private EndScreenView endScreen;
        [SerializeField] private SealPickup seal;
        [SerializeField] private float talkDistance = 4f;
        [SerializeField] private float pickupDistance = 2f;

        private float startTime;
        private bool finished;

        private void Start()
        {
            startTime = Time.time;
            hud.SetObjective("Kapı kilitli ve şaltere giden delik sana çok alçak.\n[E] Companion'a şalteri indirme emri ver.");
            companion.StateChanged += state => hud.SetStatus($"Companion durumu: {state}");
            companion.TaskFailed += reason => hud.SetObjective($"Companion başaramadı: {reason}\n[E] Tekrar emir ver.");
            firstDoor.Opened += () => hud.SetObjective("Kapı açıldı! İlerle ve altın renkli kralın mührünü bul.");
            seal.Collected += () => hud.SetObjective("Mührü aldın. Muhafıza git ve mührü göster.");
            secondDoor.Opened += () => hud.SetObjective("Muhafız kapıyı açtı! Çıkışa ulaş.");
            finishZone.PlayerEntered += Finish;
            hud.SetStatus($"Companion durumu: {companion.CurrentStateName}");
        }

        private void Update()
        {
            player.MovementEnabled = !dialogue.IsOpen;

            if (finished || dialogue.IsOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (CanPickUpSeal())
                {
                    seal.Collect();
                }
                else
                {
                    companion.Command(switchTarget);
                }
            }

            if (CanPickUpSeal())
            {
                hud.SetObjective("[E] Kralın mührünü al.");
            }

            if (Input.GetKeyDown(KeyCode.T) && CanTalk())
            {
                dialogue.Open();
            }

            if (firstDoor.IsOpen && !secondDoor.IsOpen && CanTalk())
            {
                hud.SetObjective(seal.IsCollected
                    ? "[T] Muhafıza mührü göster."
                    : "[T] Muhafızla konuş. Geçmek için önce kralın mührünü bulmalısın.");
            }
        }

        private bool CanPickUpSeal()
        {
            return !seal.IsCollected
                && Vector3.Distance(player.transform.position, seal.transform.position) <= pickupDistance;
        }

        private bool CanTalk()
        {
            return Vector3.Distance(player.transform.position, dialogue.transform.position) <= talkDistance;
        }

        private void Finish()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            dialogue.Close();
            endScreen.Show($"Tebrikler, oyunu tamamladın!\nSüre: {Time.time - startTime:F0} saniye");
        }
    }
}
