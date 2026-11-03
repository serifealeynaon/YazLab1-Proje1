using UnityEngine;

namespace YazLab.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private float gravity = -15f;

        private CharacterController characterController;
        private float verticalSpeed;

        public bool MovementEnabled { get; set; } = true;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector3 input = MovementEnabled
                ? new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"))
                : Vector3.zero;
            input = Vector3.ClampMagnitude(input, 1f);

            verticalSpeed = characterController.isGrounded ? -1f : verticalSpeed + gravity * Time.deltaTime;
            characterController.Move((input * moveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);

            if (input.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(input), turnSpeed * Time.deltaTime);
            }
        }
    }
}
