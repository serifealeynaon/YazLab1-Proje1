using System;
using UnityEngine;

namespace YazLab.Interaction
{
    public class SealPickup : MonoBehaviour
    {
        [SerializeField] private string[] words = { "ŞAFAK", "ELMAS", "ZÜMRÜT" };

        public event Action Collected;

        public bool IsCollected { get; private set; }
        public string Word { get; private set; }

        public void Collect()
        {
            if (IsCollected)
            {
                return;
            }

            IsCollected = true;
            Word = words[UnityEngine.Random.Range(0, words.Length)];
            gameObject.SetActive(false);
            Collected?.Invoke();
        }
    }
}
