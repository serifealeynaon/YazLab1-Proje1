using System;
using UnityEngine;

namespace YazLab.Interaction
{
    public class Door : MonoBehaviour, IOpenable
    {
        [SerializeField] private float openHeight = 3.2f;
        [SerializeField] private float openSpeed = 2.5f;

        private Vector3 closedPosition;

        public event Action Opened;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            closedPosition = transform.position;
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            Opened?.Invoke();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            Vector3 target = closedPosition + Vector3.up * openHeight;
            transform.position = Vector3.MoveTowards(transform.position, target, openSpeed * Time.deltaTime);
        }
    }
}
