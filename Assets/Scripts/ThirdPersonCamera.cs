using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Tooltip("Anchor positioned around the player's head/eye level.")]
    [SerializeField] private Transform firstPersonAnchor;

    [Tooltip("Visible child model that will be hidden in first-person mode.")]
    [SerializeField] private GameObject characterModel;

    [Header("Intro")]
    [SerializeField] private float introDuration = 5f;

    [Tooltip("Fixed camera offset during the intro. This does NOT rotate with the player.")]
    [SerializeField] private Vector3 introOffset =
        new Vector3(0f, 2.0f, -4.0f);

    [Tooltip("Point above the Player that the intro camera looks at.")]
    [SerializeField] private Vector3 introLookOffset =
        new Vector3(0f, 1.0f, 0f);

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 1.5f;

    [Header("First Person Look")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float touchSensitivity = 0.12f;

    [SerializeField] private float minPitch = -60f;
    [SerializeField] private float maxPitch = 70f;

    [Tooltip("Finger/mouse movement needed before input becomes a camera drag.")]
    [SerializeField] private float dragThresholdPixels = 18f;

    private enum CameraMode
    {
        Intro,
        Transition,
        FirstPerson
    }

    private CameraMode mode = CameraMode.Intro;

    private float introTimer;
    private float transitionTimer;

    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation;

    private float yaw;
    private float pitch;

    private bool dragging;
    private Vector2 pointerStart;

    private void Start()
    {
        FindReferences();

        if (player == null)
        {
            Debug.LogError("Camera could not find Player.");
            enabled = false;
            return;
        }

        if (firstPersonAnchor == null)
        {
            Debug.LogError("FirstPersonAnchor is not assigned.");
            enabled = false;
            return;
        }

        if (characterModel != null)
        {
            characterModel.SetActive(true);
        }

        SetIntroCameraImmediately();
    }

    private void LateUpdate()
    {
        switch (mode)
        {
            case CameraMode.Intro:
                UpdateIntro();
                break;

            case CameraMode.Transition:
                UpdateTransition();
                break;

            case CameraMode.FirstPerson:
                UpdateFirstPerson();
                break;
        }
    }

    // ---------------------------------------------------------
    // REFERENCES
    // ---------------------------------------------------------

    private void FindReferences()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player != null && firstPersonAnchor == null)
        {
            Transform foundAnchor =
                player.Find("FirstPersonAnchor");

            if (foundAnchor != null)
            {
                firstPersonAnchor = foundAnchor;
            }
        }
    }

    // ---------------------------------------------------------
    // INTRO CAMERA
    // ---------------------------------------------------------

    private void SetIntroCameraImmediately()
    {
        Vector3 cameraPosition =
            player.position + introOffset;

        Vector3 lookPoint =
            player.position + introLookOffset;

        transform.position = cameraPosition;
        transform.LookAt(lookPoint);
    }

    private void UpdateIntro()
    {
        introTimer += Time.deltaTime;

        // Follow ONLY Player position.
        // Do not use Player rotation.
        Vector3 desiredPosition =
            player.position + introOffset;

        Vector3 lookPoint =
            player.position + introLookOffset;

        transform.position = desiredPosition;
        transform.LookAt(lookPoint);

        if (introTimer >= introDuration)
        {
            BeginTransition();
        }
    }

    // ---------------------------------------------------------
    // TRANSITION
    // ---------------------------------------------------------

    private void BeginTransition()
    {
        mode = CameraMode.Transition;

        transitionTimer = 0f;

        transitionStartPosition =
            transform.position;

        transitionStartRotation =
            transform.rotation;
    }

    private void UpdateTransition()
    {
        transitionTimer += Time.deltaTime;

        float t =
            Mathf.Clamp01(
                transitionTimer /
                transitionDuration
            );

        // Smoothstep
        t =
            t * t *
            (3f - 2f * t);

        Vector3 targetPosition =
            firstPersonAnchor.position;

        Quaternion targetRotation =
            firstPersonAnchor.rotation;

        transform.position =
            Vector3.Lerp(
                transitionStartPosition,
                targetPosition,
                t
            );

        transform.rotation =
            Quaternion.Slerp(
                transitionStartRotation,
                targetRotation,
                t
            );

        if (t >= 1f)
        {
            EnterFirstPerson();
        }
    }

    // ---------------------------------------------------------
    // FIRST PERSON
    // ---------------------------------------------------------

    private void EnterFirstPerson()
    {
        mode = CameraMode.FirstPerson;

        if (characterModel != null)
        {
            characterModel.SetActive(false);
        }

        Vector3 angles =
            transform.eulerAngles;

        yaw = angles.y;

        pitch = angles.x;

        if (pitch > 180f)
        {
            pitch -= 360f;
        }
    }

    private void UpdateFirstPerson()
    {
        HandleMouseLook();
        HandleTouchLook();

        // Camera position always follows eye anchor.
        transform.position =
            firstPersonAnchor.position;

        transform.rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );
    }

    // ---------------------------------------------------------
    // MOUSE LOOK
    // ---------------------------------------------------------

    private void HandleMouseLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 position =
            Mouse.current.position.ReadValue();

        if (
            Mouse.current.leftButton
                .wasPressedThisFrame
        )
        {
            pointerStart = position;
            dragging = false;
        }

        if (
            Mouse.current.leftButton
                .isPressed
        )
        {
            if (!dragging)
            {
                float distanceMoved =
                    Vector2.Distance(
                        pointerStart,
                        position
                    );

                if (
                    distanceMoved >=
                    dragThresholdPixels
                )
                {
                    dragging = true;
                }
            }

            if (dragging)
            {
                Vector2 delta =
                    Mouse.current.delta.ReadValue();

                yaw +=
                    delta.x *
                    mouseSensitivity;

                pitch -=
                    delta.y *
                    mouseSensitivity;

                pitch =
                    Mathf.Clamp(
                        pitch,
                        minPitch,
                        maxPitch
                    );
            }
        }

        if (
            Mouse.current.leftButton
                .wasReleasedThisFrame
        )
        {
            dragging = false;
        }

        // Right mouse always looks around.
        if (
            Mouse.current.rightButton
                .isPressed
        )
        {
            Vector2 delta =
                Mouse.current.delta.ReadValue();

            yaw +=
                delta.x *
                mouseSensitivity;

            pitch -=
                delta.y *
                mouseSensitivity;

            pitch =
                Mathf.Clamp(
                    pitch,
                    minPitch,
                    maxPitch
                );
        }
    }

    // ---------------------------------------------------------
    // TOUCH LOOK
    // ---------------------------------------------------------

    private void HandleTouchLook()
    {
        if (
            Touchscreen.current ==
            null
        )
        {
            return;
        }

        var touch =
            Touchscreen.current
                .primaryTouch;

        Vector2 position =
            touch.position.ReadValue();

        if (
            touch.press
                .wasPressedThisFrame
        )
        {
            pointerStart = position;
            dragging = false;
        }

        if (
            touch.press.isPressed
        )
        {
            if (!dragging)
            {
                float distanceMoved =
                    Vector2.Distance(
                        pointerStart,
                        position
                    );

                if (
                    distanceMoved >=
                    dragThresholdPixels
                )
                {
                    dragging = true;
                }
            }

            if (dragging)
            {
                Vector2 delta =
                    touch.delta.ReadValue();

                yaw +=
                    delta.x *
                    touchSensitivity;

                pitch -=
                    delta.y *
                    touchSensitivity;

                pitch =
                    Mathf.Clamp(
                        pitch,
                        minPitch,
                        maxPitch
                    );
            }
        }

        if (
            touch.press
                .wasReleasedThisFrame
        )
        {
            dragging = false;
        }
    }
}