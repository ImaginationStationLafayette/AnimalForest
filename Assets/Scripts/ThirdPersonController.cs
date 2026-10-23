using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private float gravity = -18f;

    [Header("Fall & Boundary Safety")]
    [Tooltip("If player falls below this height, return them to a safe position.")]
    [SerializeField] private float fallResetHeight = -2.0f;

    [SerializeField] private Vector3 safeRespawnPosition =
        new Vector3(-3.5f, 1.05f, 6.0f);

    private CharacterController characterController;
    private float verticalVelocity;

    public float VerticalVelocity => verticalVelocity;

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleFallSafety();
        HandleGravity();
    }

    private void HandleFallSafety()
    {
        if (transform.position.y >= fallResetHeight)
            return;

        characterController.enabled = false;

        transform.position =
            safeRespawnPosition;

        verticalVelocity = 0f;

        characterController.enabled = true;
    }

    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                // Keeps the character firmly on the ground.
                verticalVelocity = -2f;
            }
        }
        else
        {
            verticalVelocity +=
                gravity * Time.deltaTime;
        }

        Vector3 verticalMovement =
            Vector3.up * verticalVelocity;

        characterController.Move(
            verticalMovement * Time.deltaTime
        );
    }
}