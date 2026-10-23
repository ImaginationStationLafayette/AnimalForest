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

        if (mousePressActive &&
            Mouse.current.leftButton.isPressed)
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

        if (mousePressActive &&
            Mouse.current.leftButton.wasReleasedThisFrame)
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

        if (touchPressActive &&
            touch.press.isPressed)
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

        if (touchPressActive &&
            touch.press.wasReleasedThisFrame)
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

        // Check if ExhibitHUD is present and click is directly on the active mic icon
        ExhibitHUD hud = GetComponent<ExhibitHUD>();
        if (hud != null && hud.IsScreenPointOverMic(screenPosition))
        {
            // Tapped on the microphone itself - can trigger mic activation/feedback
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                1000f,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // If raycast hit the player or its children, ignore
        if (hit.transform.IsChildOf(transform) || hit.transform == transform)
        {
            return;
        }

        // Check if user tapped directly on an animal exhibit
        AnimalExhibit tappedExhibit = hit.collider.GetComponentInParent<AnimalExhibit>();
        if (tappedExhibit != null)
        {
            if (tappedExhibit.PlayerInRange)
            {
                // Player is already close enough to talk to this animal: pop up microphone
                if (hud != null)
                {
                    hud.OpenMicrophone(tappedExhibit);
                }
                return;
            }
            else
            {
                // Player tapped an animal that is farther away: move towards it
                if (NavMesh.SamplePosition(
                    hit.point,
                    out NavMeshHit animalNavHit,
                    navMeshSampleDistance + 2f,
                    NavMesh.AllAreas))
                {
                    agent.SetDestination(animalNavHit.position);
                    if (hud != null && hud.IsMicActive)
                    {
                        hud.CloseMicrophone();
                    }
                    return;
                }
            }
        }

        // Tapped outside or on regular ground: close microphone if open, and move to target
        if (hud != null && hud.IsMicActive)
        {
            hud.CloseMicrophone();
        }

        if (NavMesh.SamplePosition(
            hit.point,
            out NavMeshHit navHit,
            navMeshSampleDistance,
            NavMesh.AllAreas))
        {
            agent.SetDestination(
                navHit.position
            );
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null || !animator.gameObject.activeInHierarchy || !animator.isActiveAndEnabled)
            return;

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

            if (state.fullPathHash != MoveState &&
                !animator.IsInTransition(0))
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

            if (state.fullPathHash != IdleState &&
                !animator.IsInTransition(0))
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