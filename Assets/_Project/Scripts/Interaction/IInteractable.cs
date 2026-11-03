using UnityEngine;

namespace YazLab.Interaction
{
    public interface IInteractable
    {
        Vector3 InteractPosition { get; }
        Vector3 LookPosition { get; }
        bool IsUsed { get; }
        void Interact();
    }
}
