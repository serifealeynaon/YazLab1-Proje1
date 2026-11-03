using System;
using UnityEngine;
using YazLab.Player;

namespace YazLab.Game
{
    public class FinishZone : MonoBehaviour
    {
        public event Action PlayerEntered;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                PlayerEntered?.Invoke();
            }
        }
    }
}
