using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(NavMeshAgent))]
public class TapToMove : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Animator animator;

    [Header("Tap Detection")]
    [SerializeField] private float tapThresholdPixels = 18f;

    [Header("Navigation")]
    [SerializeField] private float navMeshSampleDistance = 2f;

    private NavMeshAgent agent;
    private ExhibitHUD hud;

    // Mouse tap state
    private Vector2 mousePressStart;
    private float mouseMaxMovement;
    private bool mousePressActive;

    // Touch tap state
    private Vector2 touchPressStart;
    private float touchMaxMovement;
    private bool touchPressActive;

    private static readonly int IdleState =
        Animator.StringToHash("Base Layer.idle");

    private static readonly int MoveState =
        Animator.StringToHash("Base Layer.move");

    private static readonly int SpeedParameter =
        Animator.StringToHash("Speed");

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hud = GetComponent<ExhibitHUD>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        HandleMouse();
        HandleTouch();
        UpdateAnimation();
    }

    // =========================================================
    // MOUSE
    // =========================================================

    private void HandleMouse()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mousePressStart =
                Mouse.current.position.ReadValue();

            mouseMaxMovement = 0f;
            mousePressActive = true;
        }

        if (
            mousePressActive &&
            Mouse.current.leftButton.isPressed
        )
        {
            Vector2 currentPosition =
                Mouse.current.position.ReadValue();

            float movement =
                Vector2.Distance(
                    mousePressStart,
                    currentPosition
                );

            if (movement > mouseMaxMovement)
            {
                mouseMaxMovement = movement;
            }
        }

        if (
            mousePressActive &&
            Mouse.current.leftButton.wasReleasedThisFrame
        )
        {
            Vector2 releasePosition =
                Mouse.current.position.ReadValue();

            mousePressActive = false;

            if (mouseMaxMovement > tapThresholdPixels)
                return;

            TryTap(releasePosition);
        }
    }

    // =========================================================
    // TOUCH
    // =========================================================

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
            return;

        TouchControl touch =
            Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            touchPressStart =
                touch.position.ReadValue();

            touchMaxMovement = 0f;
            touchPressActive = true;
        }

        if (
            touchPressActive &&
            touch.press.isPressed
        )
        {
            Vector2 currentPosition =
                touch.position.ReadValue();

            float movement =
                Vector2.Distance(
                    touchPressStart,
                    currentPosition
                );

            if (movement > touchMaxMovement)
            {
                touchMaxMovement = movement;
            }
        }

        if (
            touchPressActive &&
            touch.press.wasReleasedThisFrame
        )
        {
            Vector2 releasePosition =
                touch.position.ReadValue();

            touchPressActive = false;

            if (touchMaxMovement > tapThresholdPixels)
                return;

            TryTap(releasePosition);
        }
    }

    // =========================================================
    // TAP
    // =========================================================

    private void TryTap(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return;

        // No world interaction while a conversation is open.
        if (
            hud != null &&
            hud.IsConversationActive
        )
        {
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );

        if (
            !Physics.Raycast(
                ray,
                out RaycastHit hit,
                1000f,
                ~0,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            return;
        }

        // Ignore the player's own collider.
        if (
            hit.transform == transform ||
            hit.transform.IsChildOf(transform)
        )
        {
            return;
        }

        // =====================================================
        // ANIMAL TAP
        // =====================================================

        AnimalExhibit animal =
            hit.collider.GetComponentInParent<AnimalExhibit>();

        if (animal != null)
        {
            TryTalkToAnimal(
                animal,
                hit.collider
            );

            return;
        }

        // =====================================================
        // NORMAL GROUND TAP
        // =====================================================

        MoveToGround(
            hit.point
        );
    }

    // =========================================================
    // TALK TO ANIMAL
    // =========================================================

    private void TryTalkToAnimal(
        AnimalExhibit animal,
        Collider clickedCollider
    )
    {
        if (animal == null)
            return;

        float distance =
            GetDistanceToAnimal(
                animal
            );

        // Only open the conversation if we're close enough.
        if (
            distance <=
            animal.InteractionRadius
        )
        {
            // Completely clear current movement.
            if (agent != null)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            if (hud != null)
            {
                hud.StartConversation(
                    animal
                );
            }
        }

        // IMPORTANT:
        // If we're too far away, do NOTHING.
        //
        // We deliberately do not create a NavMesh path
        // toward the animal here.
        //
        // This removes the old "pending animal" state
        // that was causing the player to become stuck.
    }

    // =========================================================
    // DISTANCE TO ACTUAL ANIMAL BODY
    // =========================================================

    private float GetDistanceToAnimal(
        AnimalExhibit animal
    )
    {
        Collider[] colliders =
            animal.GetComponentsInChildren<Collider>();

        if (
            colliders == null ||
            colliders.Length == 0
        )
        {
            Vector3 playerPosition =
                transform.position;

            Vector3 animalPosition =
                animal.transform.position;

            playerPosition.y = 0f;
            animalPosition.y = 0f;

            return Vector3.Distance(
                playerPosition,
                animalPosition
            );
        }

        float nearestDistance =
            float.MaxValue;

        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;

            if (!col.enabled)
                continue;

            Vector3 playerPosition =
                transform.position;

            Vector3 closestPoint =
                col.ClosestPoint(
                    playerPosition
                );

            playerPosition.y = 0f;
            closestPoint.y = 0f;

            float distance =
                Vector3.Distance(
                    playerPosition,
                    closestPoint
                );

            if (distance < nearestDistance)
            {
                nearestDistance =
                    distance;
            }
        }

        return nearestDistance;
    }

    // =========================================================
    // NORMAL MOVEMENT
    // =========================================================

    private void MoveToGround(
        Vector3 worldPosition
    )
    {
        if (agent == null)
            return;

        if (
            !NavMesh.SamplePosition(
                worldPosition,
                out NavMeshHit navHit,
                navMeshSampleDistance,
                NavMesh.AllAreas
            )
        )
        {
            return;
        }

        NavMeshPath path =
            new NavMeshPath();

        bool foundPath =
            agent.CalculatePath(
                navHit.position,
                path
            );

        if (!foundPath)
            return;

        if (
            path.status !=
            NavMeshPathStatus.PathComplete
        )
        {
            return;
        }

        // Always start ground movement from a clean state.
        agent.ResetPath();
        agent.isStopped = false;

        agent.SetPath(
            path
        );
    }

    // =========================================================
    // RESET AFTER CONVERSATION
    // =========================================================

    public void ResetInteractionState()
    {
        // Clear unfinished click/touch state.
        mousePressActive = false;
        touchPressActive = false;

        mouseMaxMovement = 0f;
        touchMaxMovement = 0f;

        if (
            agent != null &&
            agent.enabled
        )
        {
            // Completely remove anything left from
            // the previous conversation.
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = false;
        }
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        if (!animator.isActiveAndEnabled)
            return;

        bool moving =
            agent != null &&
            agent.enabled &&
            !agent.isStopped &&
            agent.velocity.sqrMagnitude >
            0.05f;

        if (moving)
        {
            animator.SetFloat(
                SpeedParameter,
                1f
            );

            animator.CrossFade(
                MoveState,
                0.15f
            );
        }
        else
        {
            animator.SetFloat(
                SpeedParameter,
                0f
            );

            animator.CrossFade(
                IdleState,
                0.15f
            );
        }
    }
}