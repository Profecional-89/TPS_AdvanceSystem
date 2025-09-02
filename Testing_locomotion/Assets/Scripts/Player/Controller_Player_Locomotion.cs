using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 10f;
    public float deceleration = 15f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public float airVelocity = 3f;
    public float rotationSpeed = 3f;
    public float rotationThreshold = 35f;

    [Header("Ground Check (CharacterController integrado)")]
    public Vector3 spherePosition;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;
    public float jumpCooldown = 0.5f;
    private float nextJumpTime;

    [Header("References")]
    public Transform cameraTransform;

    [Header("Sounds")]
    public List<AudioClip> FootSteps;
    public List<AudioClip> Jumps;

    private CharacterController controller;
    public AudioSource Audio;
    public AudioSource Audio2;
    private Animator anim;
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private Vector3 playerInput;

    private bool isGrounded;
    private bool wasGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        GroundCheck();
        InputHandler();
        JumpHandler();
        MovementHandler();
        RootMotionHandler();
        AnimatorHandler();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + spherePosition, groundCheckDistance);
    }

    void GroundCheck()
    {
        // Usamos directamente el sistema integrado del CharacterController
        isGrounded = controller.isGrounded;
    }

    void InputHandler()
    {
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
    }

    void JumpHandler()
    {
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        if (isGrounded && Input.GetButtonDown("Jump") && Time.time >= nextJumpTime)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            anim.SetTrigger("Jump");
            nextJumpTime = Time.time + jumpCooldown;

            int lastJumpIndex = -1;
            if (Jumps.Count == 0) return;

            int newIndex;
            do
            {
                newIndex = Random.Range(0, Jumps.Count);
            } while (newIndex == lastJumpIndex && Jumps.Count > 1);

            lastJumpIndex = newIndex;
            Audio2.PlayOneShot(Jumps[newIndex]);
        }
    }

    void MovementHandler()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 targetDirection = (forward * playerInput.y + right * playerInput.x).normalized;
        velocity.y += gravity * Time.deltaTime;

        Vector3 move;

        if (isGrounded)
        {
            float effectiveSpeed = moveSpeed;
            Vector3 targetVelocity = targetDirection * effectiveSpeed;

            if (targetDirection.magnitude > 0.1f)
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, acceleration * Time.deltaTime);
            else
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, Vector3.zero, deceleration * Time.deltaTime);

            move = currentMoveVelocity + new Vector3(0, velocity.y, 0);
        }
        else
        {
            currentMoveVelocity = targetDirection * airVelocity;
            move = currentMoveVelocity + new Vector3(0, velocity.y, 0);
        }

        controller.Move(move * Time.deltaTime);

        if (cameraTransform != null)
        {
            Vector3 camForwardFlat = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
            float angleFromCurrent = Vector3.SignedAngle(transform.forward, camForwardFlat, Vector3.up);

            if (playerInput.sqrMagnitude > 0.01f || !isGrounded)
            {
                Quaternion targetRotation = Quaternion.LookRotation(camForwardFlat);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                anim.SetBool("TurnLeft", false);
                anim.SetBool("TurnRight", false);
            }
            else if (isGrounded)
            {
                if (Mathf.Abs(angleFromCurrent) > rotationThreshold)
                {
                    if (angleFromCurrent > 0f)
                    {
                        anim.SetBool("TurnRight", true);
                        anim.SetBool("TurnLeft", false);
                    }
                    else
                    {
                        anim.SetBool("TurnLeft", true);
                        anim.SetBool("TurnRight", false);
                    }

                    Quaternion targetRotation = Quaternion.LookRotation(camForwardFlat);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
                else
                {
                    anim.SetBool("TurnLeft", false);
                    anim.SetBool("TurnRight", false);
                }
            }
        }

        wasGrounded = isGrounded;
    }

    void RootMotionHandler()
    {
        // Root motion siempre activado en tierra, desactivado en el aire
        anim.applyRootMotion = isGrounded;
    }

    void AnimatorHandler()
    {
        anim.SetFloat("X", playerInput.x, 0.07f, Time.deltaTime);
        anim.SetFloat("Y", playerInput.y, 0.07f, Time.deltaTime);
        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("VerticalVelocity", velocity.y);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb != null && !rb.isKinematic)
        {
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            rb.AddForce(pushDir * moveSpeed, ForceMode.Impulse);
        }
    }

    public void PlayFootStep()
    {
        int lastStepIndex = -1;
        if (FootSteps.Count == 0) return;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, FootSteps.Count);
        } while (newIndex == lastStepIndex && FootSteps.Count > 1);

        lastStepIndex = newIndex;
        Audio.PlayOneShot(FootSteps[newIndex]);
    }
}
