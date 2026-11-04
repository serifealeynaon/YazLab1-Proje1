using System;
using UnityEngine;

namespace YazLab.Interaction
{
    public class SealPickup : MonoBehaviour
    {
        public event Action Collected;

        public bool IsCollected { get; private set; }

        public void Collect()
        {
            if (IsCollected)
            {
                return;
            }

            IsCollected = true;
            gameObject.SetActive(false);
            Collected?.Invoke();
        }
    }
}
