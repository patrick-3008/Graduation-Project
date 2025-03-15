using System.Collections;
using UnityEngine;

public class EnemyyController : MonoBehaviour
{
    // Player detection
    public Transform Player; // Assign the player's Transform in the Inspector
    public Transform detectionOrigin; // Detection origin for both movement and shooting
    public float detectionRadius = 10f; // The radius of the detection sphere
    public float viewAngle = 120f; // The view angle in degrees
    public string playerTag = "Player"; // Tag for the player
    public LayerMask layerToIgnore; // Layers to ignore in raycasts

    // Movement
    public float rotationSpeed = 10f; // Default rotation speed
    private bool playerInSight = false;

    // Shooting
    public GameObject arrowPrefab; // The arrow prefab to instantiate
    public Transform arrowSpawnPoint; // Point where the arrow is spawned
    public float arrowSpeed = 1f; // Speed of the arrow
    public float shootCooldown = 2f; // Time between shots
    private float nextShootTime = 0f;

    // Debug
    public bool showDebugVisuals = true;
    public int sphereSegments = 36;

    // Components
    private Animator animator;
    private Transform playerTransform;
    private Animator playerAnimator;

    void Start()
    {
        // Get animator
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on this GameObject!");
        }

        // Check required references
        if (detectionOrigin == null)
        {
            detectionOrigin = transform;
            Debug.LogWarning("Detection origin not set, using this transform instead");
        }

        if (arrowSpawnPoint == null)
        {
            arrowSpawnPoint = transform;
            Debug.LogWarning("Arrow spawn point not set, using this transform instead");
        }

        if (Player != null)
        {
            playerAnimator = Player.GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogWarning("Player does not have an Animator component!");
            }
            playerTransform = Player; // Set player transform immediately
        }
    }

    void Update()
    {
        // Try to detect player
        DetectPlayer();

        // Rotate towards the player if detected but not in direct sight
        if (playerTransform != null && !playerInSight)
        {
            RotateTowardsPlayer();
        }

        // Check if we can shoot (based on cooldown)
        if (playerInSight && Time.time >= nextShootTime)
        {
            TryShoot();
        }

        // Debug visualization
        if (showDebugVisuals)
        {
            DrawViewCone();
        }
    }

    private void DetectPlayer()
    {
        // Set player reference if not already set
        if (Player != null && playerTransform == null)
        {
            playerTransform = Player;
        }

        // Skip detection if we don't have a player reference
        if (playerTransform == null) return;

        // Optionally, add an offset if needed (e.g., detectionOrigin.position + new Vector3(0, 1.5f, 0))
        Vector3 detectionPos = detectionOrigin.position;

        // Check for direct line of sight
        Vector3 directionToTarget = (playerTransform.position - detectionPos).normalized;
        float angle = Vector3.Angle(detectionOrigin.forward, directionToTarget);
        float distanceToPlayer = Vector3.Distance(detectionPos, playerTransform.position);

        // Reset player in sight
        playerInSight = false;

        // Check if player is within detection radius and view angle
        if (distanceToPlayer <= detectionRadius && angle < viewAngle / 2)
        {
            RaycastHit hit;
            if (Physics.Raycast(detectionPos, directionToTarget, out hit, detectionRadius, ~layerToIgnore))
            {
                if (hit.collider.CompareTag(playerTag))
                {
                    playerInSight = true;

                    // Activate aiming animation
                    if (animator != null)
                    {
                        animator.SetBool("is_walking", false);
                        animator.SetBool("is_aiming", true);
                        Debug.Log("Started aiming at player");
                    }

                    // Draw debug line to player
                    if (showDebugVisuals)
                    {
                        Debug.DrawLine(detectionPos, playerTransform.position, Color.green);
                    }
                }
                else
                {
                    // Something is blocking the view; draw debug line
                    Debug.DrawLine(detectionPos, hit.point, Color.red);
                }
            }
        }

        // If the player is not in sight, stop aiming
        if (!playerInSight)
        {
            StopAiming();
        }
    }

    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;

        // Calculate direction to the player, ignoring the Y axis
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0;

        if (directionToPlayer.magnitude > 0.1f)
        {
            // Optionally adjust rotation speed based on player state (e.g., running)
            float currentRotationSpeed = rotationSpeed;
            if (playerAnimator != null && playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("run"))
            {
                currentRotationSpeed = rotationSpeed * 2; // Double speed when player is running
            }

            // Determine target rotation and smoothly rotate
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotationSpeed * Time.deltaTime);

            // Set walking animation
            if (animator != null)
            {
                animator.SetBool("is_walking", true);
                animator.SetBool("is_aiming", false);
            }
        }
    }

    private void TryShoot()
    {
        // Check if conditions are met for shooting
        if (!playerInSight || playerTransform == null) return;

        // Removed jump check to allow shooting regardless of player's state
        Debug.Log("TryShoot passed all checks, shooting now!");
        Shoot(playerTransform.position);

        // Set cooldown
        nextShootTime = Time.time + shootCooldown;
    }

    public void TriggerShoot()
    {
        // This method can be called from an Animation Event
        if (playerInSight && playerTransform != null)
        {
            Shoot(playerTransform.position);
            nextShootTime = Time.time + shootCooldown;
        }
        else
        {
            Debug.LogWarning("TriggerShoot called but conditions not met: playerInSight=" + playerInSight + ", playerTransform=" + (playerTransform != null));
        }
    }

    private void Shoot(Vector3 targetPosition)
    {
        Debug.Log("Shooting arrow at player");

        // Aim at the player's chest level
        targetPosition += new Vector3(0, 0f, 0);

        // Define the arrow spawn position
        Vector3 arrowPos = arrowSpawnPoint.position + new Vector3(0, 1.5f, 0);
        Vector3 directionToTarget = (targetPosition - arrowPos).normalized;

        if (arrowPrefab != null)
        {
            GameObject arrow = Instantiate(arrowPrefab, arrowPos, Quaternion.LookRotation(directionToTarget));
            arrow.transform.eulerAngles = new Vector3(90, arrow.transform.eulerAngles.y, arrow.transform.eulerAngles.z);

            // Move the arrow smoothly using Lerp
            StartCoroutine(MoveArrow(arrow, targetPosition));
        }
        else
        {
            Debug.LogError("Arrow prefab is not assigned!");
        }
    }

    private IEnumerator MoveArrow(GameObject arrow, Vector3 targetPosition)
    {
        Vector3 startPosition = arrow.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            // Update target position to continually aim at the player's chest
            if (playerTransform != null)
            {
                targetPosition = playerTransform.position + new Vector3(0, 1.5f, 0);
            }

            elapsedTime += Time.deltaTime * arrowSpeed;
            arrow.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime);

            if (Vector3.Distance(arrow.transform.position, targetPosition) < 0.1f)
            {
                break;
            }

            yield return null;
        }

        Debug.Log("Arrow reached the target!");
        Destroy(arrow, 2f);
    }

    private void StopAiming()
    {
        if (animator != null)
        {
            animator.SetBool("is_aiming", false);
            if (playerTransform != null)
            {
                animator.SetBool("is_walking", true);
            }
        }
    }

    private void DrawViewCone()
    {
        float halfFOV = viewAngle / 2.0f;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * detectionOrigin.forward;
        Vector3 rightRayDirection = rightRayRotation * detectionOrigin.forward;

        Debug.DrawRay(detectionOrigin.position, leftRayDirection * detectionRadius, Color.blue);
        Debug.DrawRay(detectionOrigin.position, rightRayDirection * detectionRadius, Color.blue);

        // Draw the detection sphere wireframe
        Vector3 prevPos = detectionOrigin.position + detectionOrigin.forward * detectionRadius;
        for (int i = 0; i <= sphereSegments; i++)
        {
            float angle = i * viewAngle / sphereSegments - viewAngle / 2;
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 pos = detectionOrigin.position + rot * detectionOrigin.forward * detectionRadius;

            if (i > 0)
                Debug.DrawLine(prevPos, pos, Color.blue);

            prevPos = pos;
        }
    }

    // Optional: Visualization of the detection sphere in the editor
    void OnDrawGizmosSelected()
    {
        if (detectionOrigin != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(detectionOrigin.position, detectionRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(detectionOrigin.position, detectionOrigin.forward * detectionRadius);

            float halfFOV = viewAngle / 2.0f;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.up);
            Vector3 leftRayDirection = leftRayRotation * detectionOrigin.forward;
            Vector3 rightRayDirection = rightRayRotation * detectionOrigin.forward;

            Gizmos.DrawRay(detectionOrigin.position, leftRayDirection * detectionRadius);
            Gizmos.DrawRay(detectionOrigin.position, rightRayDirection * detectionRadius);
        }
    }
}
