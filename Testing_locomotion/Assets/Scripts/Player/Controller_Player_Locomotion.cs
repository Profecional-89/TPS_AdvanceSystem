using System.Collections.Generic;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("General Control")]
    public bool HasControl = true; // 👈 Si está en false, el jugador queda totalmente congelado

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 10f;
    public float deceleration = 15f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public float airVelocity = 3f;
    public float airAcceleration = 6f;
    public float airDeceleration = 8f;
    public float rotationSpeed = 3f;
    public float rotationThreshold = 35f;

    [Header("Ground Check Sphere (RED)")]
    public Vector3 groundSphereOffset = new Vector3(0, -0.1f, 0f);
    public float groundSphereRadius = 0.2f;
    public LayerMask groundLayer;
    public float jumpCooldown = 0.5f;
    private float nextJumpTime;

    [Header("Root Motion Cooldown")]
    public float rootMotionCooldown = 0.3f;
    private float rootMotionTimer;
    private bool rootMotionCooldownActive;
    private bool rootMotionActive = true;

    [Header("Step Offset Sphere (GREEN)")]
    public Vector3 stepSphereOffset = new Vector3(0, 0.2f, 0.5f);
    public float stepSphereRadius = 0.25f;
    public LayerMask stepLayerMask; 
    private float defaultStepOffset;

    [Header("References")]
    public Transform cameraTransform;

    [Header("Sounds")]
    public List<AudioClip> FootSteps;
    public List<AudioClip> Jumps;

    [Header("Push Settings")]
    public float pushForce = 5f;

    public CharacterController controller;
    public Animator anim;
    private ParkourController parkourController;
    public AudioSource Audio;
    public AudioSource Audio2;

    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private Vector3 playerInput;

    private bool isGrounded;
    private bool wasGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        parkourController = GetComponent<ParkourController>();
        anim = GetComponent<Animator>();
        defaultStepOffset = controller.stepOffset; 
    }

    void Update()
    {
        GroundCheck();

        if (HasControl)
        {
            InputHandler();
            JumpHandler();
            MovementHandler();
        }
        else
        {
            playerInput = Vector3.zero;
            currentMoveVelocity = Vector3.zero;
        }

        RootMotionHandler();
        AnimatorHandler();
        StepOffsetHandler();
    }

    void StepOffsetHandler()
    {
        Vector3 worldPos = transform.position + transform.TransformDirection(stepSphereOffset);
        bool hit = Physics.CheckSphere(worldPos, stepSphereRadius, stepLayerMask);
        controller.stepOffset = hit ? defaultStepOffset : 0f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.TransformDirection(groundSphereOffset), groundSphereRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + transform.TransformDirection(stepSphereOffset), stepSphereRadius);
    }

    void GroundCheck()
    {
        Vector3 worldPos = transform.position + transform.TransformDirection(groundSphereOffset);
        isGrounded = Physics.CheckSphere(worldPos, groundSphereRadius, groundLayer);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = Mathf.Lerp(velocity.y, -0.5f, Time.deltaTime * 6f);
        }
    }

    void InputHandler()
    {
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
    }

    void JumpHandler()
    {
        if (isGrounded && Input.GetButtonDown("Jump") && Time.time >= nextJumpTime)
        {
            StartCoroutine(DelayedJumpCheck());
        }
    }

    IEnumerator DelayedJumpCheck()
    {
        // Espera unos milisegundos (10 ms = 0.01f)
        yield return new WaitForSeconds(0.01f);

        if (!parkourController.inAction) // ✅ Verifica de nuevo antes de saltar
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            anim.SetTrigger("Jump");
            rootMotionActive = false;
            rootMotionCooldownActive = false;
            nextJumpTime = Time.time + jumpCooldown;

            int lastJumpIndex = -1;
            if (Jumps.Count > 0)
            {
                int newIndex;
                do
                {
                    newIndex = Random.Range(0, Jumps.Count);
                } while (newIndex == lastJumpIndex && Jumps.Count > 1);

                lastJumpIndex = newIndex;
                Audio2.PlayOneShot(Jumps[newIndex]);
            }
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
            if (!wasGrounded)
            {
                velocity.y = Mathf.Min(velocity.y, -0.5f);
                rootMotionCooldownActive = true;
                rootMotionTimer = 0f;
                rootMotionActive = false;
            }

            float effectiveSpeed = rootMotionCooldownActive ? airVelocity : moveSpeed;
            Vector3 targetVelocity = targetDirection * effectiveSpeed;

            if (targetDirection.magnitude > 0.1f)
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, acceleration * Time.deltaTime);
            else
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, Vector3.zero, deceleration * Time.deltaTime);

            move = currentMoveVelocity + new Vector3(0, velocity.y, 0);
        }
        else
        {
            Vector3 targetVelocity = targetDirection * airVelocity;

            if (targetDirection.magnitude > 0.1f)
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, airAcceleration * Time.deltaTime);
            else
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, Vector3.zero, airDeceleration * Time.deltaTime);

            move = currentMoveVelocity + new Vector3(0, velocity.y, 0);
            rootMotionActive = false;
        }

        controller.Move(move * Time.deltaTime);

        if (cameraTransform != null)
        {
            Vector3 camForwardFlat = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
            float angleFromCurrent = Vector3.SignedAngle(transform.forward, camForwardFlat, Vector3.up);

            anim.SetFloat("CameraAngle", angleFromCurrent * 1.9f);

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
        if (parkourController != null && parkourController.inAction)
        {
            rootMotionActive = true;
            anim.applyRootMotion = true;
            return;
        }

        if (rootMotionCooldownActive)
        {
            rootMotionTimer += Time.deltaTime;
            if (rootMotionTimer >= rootMotionCooldown)
            {
                rootMotionCooldownActive = false;
                rootMotionActive = true;
            }
        }

        rootMotionActive = rootMotionActive && isGrounded;
        anim.applyRootMotion = rootMotionActive;
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
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z).normalized;
            rb.AddForce(pushDir * pushForce, ForceMode.Impulse);
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
