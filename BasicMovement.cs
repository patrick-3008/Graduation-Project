using UnityEngine;
using System.Collections;


[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class BasicMovement : MonoBehaviour
{
    public float speed = 2.0f;
    public float runSpeed = 5.0f;
    public float rotationSpeed = 10.0f; 

    public float jumpHeight = 1.5f;

    public float flySpeed = 5.0f; 
    public string ledgeLayerName = "Ledge";
    private LayerMask ledgeLayerMask;
    public float gravity = -9.81f; 

    private CharacterController controller;
    private Animator animator;
    private Transform cameraTransform; 

    private Vector3 velocity; 
    private bool isGrounded;
    private bool isRunning;
    private bool canFly = false;
    private bool falling = false;

    public float wallCheckDistance = 2f;
    public float stepSize = 0.1f;
    public float maxWallHeight = 20f;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        ledgeLayerMask = LayerMask.GetMask(ledgeLayerName);

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Main Camera not found. Camera-relative movement will not work correctly.");
           
        }

       
    }

    void Update()
    {
        HandleGroundedState();

        HandleMovementInput();

        HandleJumpInput();

        HandleActionInputs();

        HandleFlying(); 

        ApplyVerticalVelocity();

        UpdateAnimatorStates();
        CheckWallHeightAndTrigger();
    }

    void HandleGroundedState()
    {
        isGrounded = controller.isGrounded;


        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            falling = false;

        }
    }

    void HandleMovementInput()
    {
        if (cameraTransform == null) return; 

        float horizontal = Input.GetAxis("Horizontal"); 
        float vertical = Input.GetAxis("Vertical");    

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0; 
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = right * horizontal + forward * vertical;
        moveDirection.Normalize(); 

        isRunning = Input.GetKey(KeyCode.T); 
        float currentSpeed = isRunning ? runSpeed : speed;

        if (moveDirection.magnitude >= 0.1f)
        {
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }


        animator.SetBool("is_walking", moveDirection.magnitude >= 0.1f && !isRunning);
        animator.SetBool("is_running", moveDirection.magnitude >= 0.1f && isRunning);
    }

    void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetBool("is_jumping", true); 
        }

        
        if (isGrounded && !Input.GetButtonDown("Jump")) 
        {
            animator.SetBool("is_jumping", false);
        }
        else if (!isGrounded)
        {
           
        }
    }

    void HandleActionInputs()
    {
        animator.SetBool("is_aiming", Input.GetKey(KeyCode.R)); 
        animator.SetBool("is_boxing", Input.GetKey(KeyCode.F)); 
        animator.SetBool("is_kicking", Input.GetKey(KeyCode.G)); 
    }

    void HandleFlying()
    {
        bool wantsToFly = canFly && Input.GetKey(KeyCode.C);
        bool is_ledgingstate = animator.GetBool("is_ledging");
        if (animator.GetBool("is_ledging"))
        {
            velocity.y = 0;
            StartCoroutine(DisableLedge());



        }
        if (wantsToFly&&!falling&&is_ledgingstate==false)
        {
            animator.SetBool("is_jumping", false);
            animator.SetBool("is_climbing", true);
            

            velocity.y = flySpeed;

        }
        else
        {
           
            if (!isGrounded&&is_ledgingstate == false)
            {
                velocity.y += gravity * Time.deltaTime;
                animator.SetBool("is_climbing", false);
                falling = true;
            }
            

        }
    }

   
    void ApplyVerticalVelocity()
    {
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateAnimatorStates()
    {

        if (isGrounded && animator.GetBool("is_jumping") && velocity.y <= 0)
        {
            animator.SetBool("is_jumping", false);
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(ledgeLayerName))
        {
            canFly = true;
            Debug.Log("Entered Ledge Trigger Zone. Climbing enabled.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(ledgeLayerName))
        {
            canFly = false;
            Debug.Log("Exited Ledge Trigger Zone. Climbing disabled.");
            // Optional: If the player exits the trigger while climbing, maybe stop the climb?
             if (animator.GetBool("is_climbing"))
             {
                animator.SetBool("is_climbing", false);
                velocity.y += gravity * Time.deltaTime;
                falling = true;
            }
        }
    }
    void CheckWallHeightAndTrigger()
    {
        RaycastHit hit;

        // Cast forward from the player to find the wall
        if (Physics.Raycast(transform.position, transform.forward, out hit, wallCheckDistance, ledgeLayerMask))
        {
            Debug.DrawRay(transform.position, transform.forward * wallCheckDistance, Color.red);

            Vector3 wallHitPoint = hit.point;
            float wallBaseY = wallHitPoint.y;
            float scanHeight = 0f;
            float wallTopY = wallBaseY;

            // Scan upwards from wall base to find where the wall ends
            while (scanHeight < maxWallHeight)
            {
                Vector3 scanPoint = wallHitPoint + Vector3.up * scanHeight;

                // If there's no more wall in front, we've reached the top
                if (!Physics.Raycast(scanPoint, transform.forward, 0.1f, ledgeLayerMask))
                {
                    wallTopY = scanPoint.y;
                    break;
                }

                scanHeight += stepSize;
            }

            // Now compare player's height to wall top
            float playerY = transform.position.y;
            float distanceToTop = wallTopY - playerY;

            Debug.Log($"Distance to wall top: {distanceToTop:F2}m");

            if (distanceToTop <= 2.5f && distanceToTop >= 0f)
            {
                TriggerActionNearTop(); // You're close to the top — time to grab/climb/jump etc.
            }
        }
    }
    IEnumerator LedgeClimb(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;
        controller.enabled = false;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        animator.SetBool("is_ledging", false); // end ledge animation
        controller.enabled = true;

    }
    void TriggerActionNearTop()
    {
        //Debug.Log("You're within 1 meter of the wall's top! Performing action...");
        animator.SetBool("is_ledging", true);
        //Vector3 targetPosition = transform.position + new Vector3(2f, 0f, 0f);
        //StartCoroutine(LedgeClimb(targetPosition, 0.5f));


    }
    IEnumerator DisableLedge()
    {
        yield return new WaitForSeconds(4f);

        animator.SetBool("is_ledging", false);
        velocity.y = -2f;

        // Calculate target position
        Vector3 targetPosition = transform.position + new Vector3(2f, 0f, 2f);

        // Use your existing smooth movement coroutine
        //StartCoroutine(LedgeClimb(targetPosition, 0.5f));
    }
}
   
    


