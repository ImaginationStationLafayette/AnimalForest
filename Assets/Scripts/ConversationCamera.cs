using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ConversationCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ThirdPersonCamera normalCamera;
    [SerializeField] private Transform firstPersonAnchor;

    [Header("Adaptive Animal Framing")]

    [Tooltip("Closest the camera is allowed to get to an animal.")]
    [SerializeField] private float minimumCameraDistance = 2.5f;

    [Tooltip("Maximum distance for very large animals.")]
    [SerializeField] private float maximumCameraDistance = 7f;

    [Tooltip("Controls how strongly animal size affects camera distance.")]
    [SerializeField] private float sizeDistanceMultiplier = 1.15f;

    [Range(0.5f, 1f)]
    [Tooltip("Where vertically to aim inside the animal bounds. 0.8 is near the head.")]
    [SerializeField] private float lookHeightPercent = 0.80f;

    [Tooltip("How strongly the animal is pushed toward the RIGHT side of the screen.")]
    [SerializeField] private float rightSideFraming = 0.55f;

    [Header("Transition")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float returnSpeed = 6f;

    private AnimalExhibit activeAnimal;

    private bool conversationActive;
    private bool returningToPlayer;

    private Quaternion savedExplorationRotation;

    private Vector3 targetConversationPosition;
    private Quaternion targetConversationRotation;

    // =========================================================
    // START
    // =========================================================

    private void Awake()
    {
        FindReferences();
    }

    // =========================================================
    // FIND REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (normalCamera == null)
        {
            normalCamera =
                GetComponent<ThirdPersonCamera>();
        }

        if (firstPersonAnchor == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                firstPersonAnchor =
                    player.transform.Find(
                        "FirstPersonAnchor"
                    );
            }
        }
    }

    // =========================================================
    // BEGIN CONVERSATION
    // =========================================================

    public void BeginConversation(
        AnimalExhibit animal
    )
    {
        if (animal == null)
            return;

        FindReferences();

        activeAnimal = animal;

        conversationActive = true;
        returningToPlayer = false;

        // Remember the direction the child was looking
        // before talking to the animal.
        savedExplorationRotation =
            transform.rotation;

        // IMPORTANT:
        // ThirdPersonCamera no longer touches the camera.
        if (normalCamera != null)
        {
            normalCamera.PauseCameraControl();
        }

        CalculateConversationView();
    }

    // =========================================================
    // END CONVERSATION
    // =========================================================

    public void EndConversation()
    {
        conversationActive = false;
        returningToPlayer = true;

        activeAnimal = null;

        // DO NOT resume ThirdPersonCamera yet.
        //
        // ConversationCamera first smoothly returns
        // to the player's eye position.
    }

    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        // -----------------------------------------------------
        // CONVERSATION VIEW
        // -----------------------------------------------------

        if (
            conversationActive &&
            activeAnimal != null
        )
        {
            CalculateConversationView();

            float t =
                1f -
                Mathf.Exp(
                    -moveSpeed *
                    Time.deltaTime
                );

            transform.position =
                Vector3.Lerp(
                    transform.position,
                    targetConversationPosition,
                    t
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetConversationRotation,
                    t
                );

            return;
        }

        // -----------------------------------------------------
        // RETURN TO FIRST PERSON
        // -----------------------------------------------------

        if (returningToPlayer)
        {
            ReturnToPlayer();
        }
    }

    // =========================================================
    // RETURN TO PLAYER
    // =========================================================

    private void ReturnToPlayer()
    {
        if (firstPersonAnchor == null)
        {
            FindReferences();
        }

        if (firstPersonAnchor == null)
        {
            FinishReturn();
            return;
        }

        Vector3 targetPosition =
            firstPersonAnchor.position;

        Quaternion targetRotation =
            savedExplorationRotation;

        float t =
            1f -
            Mathf.Exp(
                -returnSpeed *
                Time.deltaTime
            );

        transform.position =
            Vector3.Lerp(
                transform.position,
                targetPosition,
                t
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                t
            );

        float positionDistance =
            Vector3.Distance(
                transform.position,
                targetPosition
            );

        float rotationDistance =
            Quaternion.Angle(
                transform.rotation,
                targetRotation
            );

        if (
            positionDistance < 0.03f &&
            rotationDistance < 0.5f
        )
        {
            transform.position =
                targetPosition;

            transform.rotation =
                targetRotation;

            FinishReturn();
        }
    }

    // =========================================================
    // FINISH RETURN
    // =========================================================

    private void FinishReturn()
    {
        returningToPlayer = false;

        if (normalCamera != null)
        {
            // Tell ThirdPersonCamera:
            // "This is now your current yaw/pitch."
            normalCamera.SyncLookToCurrentCamera();

            normalCamera.ResumeCameraControl();
        }
    }

    // =========================================================
    // CALCULATE CONVERSATION CAMERA
    // =========================================================

    private void CalculateConversationView()
    {
        if (activeAnimal == null)
            return;

        Bounds bounds =
            GetVisualBounds(
                activeAnimal
            );

        float height =
            Mathf.Max(
                bounds.size.y,
                0.5f
            );

        float horizontalSize =
            Mathf.Max(
                bounds.size.x,
                bounds.size.z,
                0.5f
            );

        // -----------------------------------------------------
        // LOOK POINT
        //
        // Instead of looking at collider center/belly,
        // aim near the upper part of the animal.
        // -----------------------------------------------------

        Vector3 lookPoint =
            bounds.center;

        lookPoint.y =
            Mathf.Lerp(
                bounds.min.y,
                bounds.max.y,
                lookHeightPercent
            );

        // -----------------------------------------------------
        // DIRECTION
        //
        // Put camera on approximately the same side of
        // the animal that the player was already viewing from.
        // -----------------------------------------------------

        Vector3 directionFromAnimal =
            transform.position -
            bounds.center;

        directionFromAnimal.y = 0f;

        if (
            directionFromAnimal.sqrMagnitude <
            0.01f
        )
        {
            directionFromAnimal =
                -activeAnimal.transform.forward;

            directionFromAnimal.y = 0f;
        }

        directionFromAnimal.Normalize();

        // -----------------------------------------------------
        // ADAPTIVE DISTANCE
        //
        // Horse -> farther away
        // Chicken -> closer
        // Cat/Dog -> middle
        // -----------------------------------------------------

        float sizeBasedDistance =
            Mathf.Max(
                height * 0.75f,
                horizontalSize * 1.6f
            );

        float cameraDistance =
            Mathf.Clamp(
                sizeBasedDistance *
                sizeDistanceMultiplier,
                minimumCameraDistance,
                maximumCameraDistance
            );

        // Slight vertical camera adjustment based
        // on animal height.
        float cameraVerticalOffset =
            Mathf.Clamp(
                height * 0.08f,
                0.15f,
                0.75f
            );

        targetConversationPosition =
            lookPoint +
            directionFromAnimal *
            cameraDistance +
            Vector3.up *
            cameraVerticalOffset;

        // -----------------------------------------------------
        // PUT ANIMAL ON RIGHT SIDE
        // -----------------------------------------------------

        Vector3 directForward =
            (
                lookPoint -
                targetConversationPosition
            ).normalized;

        Vector3 cameraRight =
            Vector3.Cross(
                Vector3.up,
                directForward
            ).normalized;

        float sideOffset =
            Mathf.Max(
                0.4f,
                horizontalSize *
                rightSideFraming
            );

        // Aim to the LEFT of the animal.
        // Therefore the animal appears on the RIGHT.
        Vector3 shiftedLookPoint =
            lookPoint -
            cameraRight *
            sideOffset;

        Vector3 lookDirection =
            shiftedLookPoint -
            targetConversationPosition;

        targetConversationRotation =
            Quaternion.LookRotation(
                lookDirection.normalized,
                Vector3.up
            );
    }

    // =========================================================
    // GET VISUAL BOUNDS
    // =========================================================

    private Bounds GetVisualBounds(
        AnimalExhibit animal
    )
    {
        // Renderer bounds are better here than collider bounds.
        //
        // We care about what the animal LOOKS like,
        // not how large its physics collider happens to be.

        Renderer[] renderers =
            animal.GetComponentsInChildren<Renderer>(
                true
            );

        bool foundRenderer =
            false;

        Bounds combinedBounds =
            new Bounds(
                animal.transform.position,
                Vector3.one
            );

        foreach (
            Renderer renderer
            in renderers
        )
        {
            if (renderer == null)
                continue;

            if (!renderer.enabled)
                continue;

            if (!foundRenderer)
            {
                combinedBounds =
                    renderer.bounds;

                foundRenderer =
                    true;
            }
            else
            {
                combinedBounds.Encapsulate(
                    renderer.bounds
                );
            }
        }

        // Fallback if animal somehow has no renderer.
        if (!foundRenderer)
        {
            Collider[] colliders =
                animal.GetComponentsInChildren<Collider>();

            bool foundCollider =
                false;

            foreach (
                Collider col
                in colliders
            )
            {
                if (
                    col == null ||
                    !col.enabled
                )
                {
                    continue;
                }

                if (!foundCollider)
                {
                    combinedBounds =
                        col.bounds;

                    foundCollider =
                        true;
                }
                else
                {
                    combinedBounds.Encapsulate(
                        col.bounds
                    );
                }
            }
        }

        return combinedBounds;
    }
}