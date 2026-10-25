using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

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

    private Vector2 mousePressStart;
    private float mouseMaxMovement;
    private bool mousePressActive;

    private Vector2 touchPressStart;
    private float touchMaxMovement;
    private bool touchPressActive;

    private static readonly int IdleState =
        Animator.StringToHash("Base Layer.idle");

    private static readonly int MoveState =
        Animator.StringToHash("Base Layer.move");

    private static readonly int SpeedParameter =
        Animator.StringToHash("Speed");

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

    private void Update()
    {
        HandleMouse();
        HandleTouch();
        UpdateAnimation();
    }

    private void HandleMouse()
    {
        if (Mouse.current == null)
            return;

        Vector2 position =
            Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mousePressStart = position;
            mouseMaxMovement = 0f;
            mousePressActive = true;
        }

        if (
            mousePressActive &&
            Mouse.current.leftButton.isPressed
        )
        {
            float movement =
                Vector2.Distance(
                    mousePressStart,
                    position
                );

            mouseMaxMovement =
                Mathf.Max(
                    mouseMaxMovement,
                    movement
                );
        }

        if (
            mousePressActive &&
            Mouse.current.leftButton.wasReleasedThisFrame
        )
        {
            if (mouseMaxMovement < tapThresholdPixels)
            {
                TryMove(position);
            }

            mousePressActive = false;
        }
    }

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
            return;

        var touch =
            Touchscreen.current.primaryTouch;

        Vector2 position =
            touch.position.ReadValue();

        if (touch.press.wasPressedThisFrame)
        {
            touchPressStart = position;
            touchMaxMovement = 0f;
            touchPressActive = true;
        }

        if (
            touchPressActive &&
            touch.press.isPressed
        )
        {
            float movement =
                Vector2.Distance(
                    touchPressStart,
                    position
                );

            touchMaxMovement =
                Mathf.Max(
                    touchMaxMovement,
                    movement
                );
        }

        if (
            touchPressActive &&
            touch.press.wasReleasedThisFrame
        )
        {
            if (touchMaxMovement < tapThresholdPixels)
            {
                TryMove(position);
            }

            touchPressActive = false;
        }
    }

    private void TryMove(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return;

        // While talking to an animal, ground taps
        // should not move the player away.
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

        // Ignore clicks on the player's own hierarchy.
        if (
            hit.transform == transform ||
            hit.transform.IsChildOf(transform)
        )
        {
            return;
        }

        // Did we tap an animal?
        AnimalExhibit tappedExhibit =
            hit.collider
                .GetComponentInParent<AnimalExhibit>();

        if (tappedExhibit != null)
        {
            // Already close enough:
            // start the conversation.
            if (tappedExhibit.PlayerInRange)
            {
                if (hud != null)
                {
                    hud.StartConversation(
                        tappedExhibit
                    );
                }

                return;
            }

            // Animal is farther away:
            // walk toward it.
            if (
                NavMesh.SamplePosition(
                    hit.point,
                    out NavMeshHit animalNavHit,
                    navMeshSampleDistance + 2f,
                    NavMesh.AllAreas
                )
            )
            {
                agent.isStopped = false;

                agent.SetDestination(
                    animalNavHit.position
                );

                return;
            }
        }

        // Normal ground tap.
        if (
            NavMesh.SamplePosition(
                hit.point,
                out NavMeshHit navHit,
                navMeshSampleDistance,
                NavMesh.AllAreas
            )
        )
        {
            agent.isStopped = false;

            agent.SetDestination(
                navHit.position
            );
        }
    }

    private void UpdateAnimation()
    {
        if (
            animator == null ||
            !animator.gameObject.activeInHierarchy ||
            !animator.isActiveAndEnabled
        )
        {
            return;
        }

        bool moving =
            agent.velocity.sqrMagnitude > 0.05f;

        if (moving)
        {
            animator.SetFloat(
                SpeedParameter,
                1f
            );

            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);

            if (
                state.fullPathHash != MoveState &&
                !animator.IsInTransition(0)
            )
            {
                animator.CrossFade(
                    MoveState,
                    0.12f,
                    0,
                    0
                );
            }
        }
        else
        {
            animator.SetFloat(
                SpeedParameter,
                0f
            );

            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);

            if (
                state.fullPathHash != IdleState &&
                !animator.IsInTransition(0)
            )
            {
                animator.CrossFade(
                    IdleState,
                    0.2f,
                    0,
                    0
                );
            }
        }
    }
}