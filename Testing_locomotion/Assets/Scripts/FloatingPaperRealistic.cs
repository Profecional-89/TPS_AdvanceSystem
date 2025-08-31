// FloatingPaperRealisticOptimized.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class FloatingPaperRealistic : MonoBehaviour
{
    private Rigidbody rb;
    private bool isGrounded;

    // semillas para Perlin Noise
    private float seedX;
    private float seedY;
    private float seedZ;

    [Header("Caída lenta")]
    [Range(0f, 1f)] public float gravityFactor = 0.2f;
    public float terminalVelocity = 2f;
    public float linearDrag = 6f;
    public float angularDrag = 5f;

    [Header("Giro suave en el aire")]
    public float maxTorque = 0.05f;
    public float noiseFrequency = 0.5f;

    [Header("Ground Layers")]
    public LayerMask groundLayers;
    private static List<Collider> allPaperColliders = new List<Collider>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.mass = 0.005f;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
        rb.maxAngularVelocity = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(0f, 100f);
        seedZ = Random.Range(0f, 100f);

        // Ignora colisiones con otros papeles
        Collider myCollider = GetComponent<Collider>();
        foreach (var other in allPaperColliders)
        {
            Physics.IgnoreCollision(myCollider, other);
        }
        allPaperColliders.Add(myCollider);
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return;

        rb.AddForce(Physics.gravity * gravityFactor, ForceMode.Acceleration);

        if (rb.linearVelocity.y < -terminalVelocity)
        {
            var v = rb.linearVelocity;
            v.y = -terminalVelocity;
            rb.linearVelocity = v;
        }

        if (!isGrounded)
        {
            float t = Time.time * noiseFrequency;
            Vector3 noiseVec = new Vector3(
                Mathf.PerlinNoise(seedX, t) - 0.5f,
                Mathf.PerlinNoise(seedY, t) - 0.5f,
                Mathf.PerlinNoise(seedZ, t) - 0.5f
            );
            rb.AddTorque(noiseVec * maxTorque, ForceMode.Acceleration);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (((1 << col.gameObject.layer) & groundLayers) != 0)
            isGrounded = true;
    }

    void OnCollisionExit(Collision col)
    {
        if (((1 << col.gameObject.layer) & groundLayers) != 0)
            isGrounded = false;
    }

    public void Release()
    {
        if (!rb.isKinematic) return;
        isGrounded = false;
        rb.isKinematic = false;
        rb.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
    }
} 