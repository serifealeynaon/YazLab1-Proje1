using UnityEngine;

namespace YazLab.Interaction
{
    public class SwitchInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform interactPoint;
        [SerializeField] private Door door;
        [SerializeField] private Renderer indicator;

        private IOpenable openable;

        public Vector3 InteractPosition => interactPoint.position;
        public Vector3 LookPosition => transform.position;
        public bool IsUsed { get; private set; }

        private void Awake()
        {
            openable = door;
        }

        public void Interact()
        {
            if (IsUsed)
            {
                return;
            }

            IsUsed = true;
            openable.Open();

            if (indicator != null)
            {
                indicator.material.color = Color.green;
            }
        }
    }
}
