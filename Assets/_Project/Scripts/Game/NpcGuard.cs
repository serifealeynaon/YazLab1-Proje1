using UnityEngine;
using YazLab.Interaction;
using YazLab.LLM;

namespace YazLab.Game
{
    public class NpcGuard : MonoBehaviour
    {
        [SerializeField] private Door door;
        [SerializeField] private Renderer body;
        [SerializeField] private Color calmColor = new Color(0.55f, 0.3f, 0.8f);
        [SerializeField] private Color hostileColor = new Color(0.85f, 0.1f, 0.1f);

        private IOpenable openable;

        public bool IsHostile { get; private set; }

        private void Awake()
        {
            openable = door;
            body.material.color = calmColor;
        }

        public void Apply(NpcAction action)
        {
            IsHostile = action.hostile && !action.door_unlocked;
            body.material.color = IsHostile ? hostileColor : calmColor;

            if (action.door_unlocked)
            {
                openable.Open();
            }
        }
    }
}
