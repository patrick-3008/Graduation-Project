using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubZeroMove : MonoBehaviour
{
    public CharacterController charController;
    Animator anim;
    public new Camera camera;

    private float gravityY = 0.0f;
    public float mass = 1.0f;
    bool isJumping;
    bool isGrounded;
    public float timer = 360.0f;
    bool isRunning = false;
    bool isCrouching = false;
    float lerpRun = 0.0f;
    float lerpCrouch = 0.0f;

    private Vector3 jumpMomentum = Vector3.zero; // Store momentum for jumping

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
        else
            UnityEngine.Cursor.lockState = CursorLockMode.None;
    }

    void Start()
    {
        charController = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        isGrounded = true;
        timer = 360.0f;
        anim.SetBool("Lost", false);
        anim.SetBool("Win", false);
    }

    void Update()
    {
        float dX = Input.GetAxis("Horizontal");
        float dY = Input.GetAxis("Vertical");

        Vector3 movementVector = new Vector3(dX, 0, dY);
        movementVector = Quaternion.AngleAxis(camera.transform.eulerAngles.y, Vector3.up) * movementVector;
        movementVector.Normalize();

        gravityY += Physics.gravity.y * mass * Time.deltaTime;

        if (charController.isGrounded)
        {
            gravityY = -0.5f;
            isJumping = false;
            anim.SetBool("IsJumping", false);
            isGrounded = true;
            anim.SetBool("IsGrounded", true);
            anim.SetBool("IsFalling", false);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                isJumping = true;
                anim.SetBool("IsJumping", true);
                gravityY = 6.0f; // Initial jump force

                // Store current movement as jump momentum
                jumpMomentum = movementVector * (isRunning ? 4.0f : 2.0f); // Scale based on running or walking
            }
        }
        else
        {
            isGrounded = false;
            anim.SetBool("IsGrounded", false);

            if (isJumping && gravityY < 0 || gravityY < -4)
                anim.SetBool("IsFalling", true);
        }

        // Use momentum in the air
        Vector3 newMoveVector = isGrounded ? movementVector : jumpMomentum;
        newMoveVector.y = gravityY;

        Physics.SyncTransforms();

        // Crouch Logic
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            anim.SetBool("IsCrouching", true);
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            anim.SetBool("IsCrouching", false);
        }

        if (isCrouching)
        {
            // Smoothly transition crouch-walk blend value
            float crouchSpeed = movementVector.magnitude > 0 ? 1.0f : 0.0f;
            lerpCrouch = Mathf.Lerp(lerpCrouch, crouchSpeed, 2.0f * Time.deltaTime);
            anim.SetFloat("Crouch_Walk", lerpCrouch);
        }

        // Walk/Run Logic
        if (!isCrouching)
        {
            if (isRunning)
            {
                float delta = Mathf.Lerp(0, 1, lerpRun);
                if (lerpRun < 1.0f)
                    lerpRun += 2.0f * Time.deltaTime;
                anim.SetFloat("Walk_Run", delta);
            }
            else
            {
                float targetWalkValue = 0.5f;
                float delta = Mathf.Lerp(anim.GetFloat("Walk_Run"), targetWalkValue, 1.0f * Time.deltaTime);
                anim.SetFloat("Walk_Run", delta);
            }
        }

        // Character Rotation
        if (movementVector != Vector3.zero)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                isRunning = true;
                lerpRun = 0.0f;
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                isRunning = false;
                lerpRun = 0.0f;
            }
            Quaternion rotationDirection = Quaternion.LookRotation(movementVector, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationDirection, 360 * Time.deltaTime);

            anim.SetBool("Walking", true);
        }
        else
        {
            anim.SetBool("Walking", false);
        }

        charController.Move(newMoveVector * Time.deltaTime);
    }

    private void OnAnimatorMove()
    {
        Vector3 _move = anim.deltaPosition;
        _move.y = gravityY * Time.deltaTime;
        charController.Move(_move);
    }
}
