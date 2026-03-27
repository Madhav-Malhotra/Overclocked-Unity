using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Bob Settings")]
    [SerializeField] private Transform playerModel;
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobFrequency = 1.8f;

    private Rigidbody rb;
    private Vector2 inputValue;
    private Vector3 moveDirection;
    private float _bobPhase;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void OnDisable()
    {
        StopMovement();
    }

void Update()
    {
        inputValue = Keyboard.current != null
        ? new Vector2(
            (Keyboard.current.dKey.isPressed ? 1f : 0f ) - (Keyboard.current.aKey.isPressed ? 1f : 0f ),
            (Keyboard.current.wKey.isPressed ? 1f : 0f ) - (Keyboard.current.sKey.isPressed ? 1f : 0f )
        ) : Vector2.zero;

        moveDirection = new Vector3(inputValue.x, 0f, inputValue.y).normalized;

        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Bob the model child on Y — faster and larger while moving, gentle idle bob while still
        bool isMoving = moveDirection.magnitude > 0.1f;
        float freq = isMoving ? bobFrequency : bobFrequency * 0.4f;
        float amp  = bobAmplitude;
        _bobPhase += Time.deltaTime * freq * Mathf.PI * 2f;
        if (playerModel != null)
        {
            Vector3 localPos = playerModel.localPosition;
            localPos.y = Mathf.Sin(_bobPhase) * amp;
            playerModel.localPosition = localPos;
        }
    }

void FixedUpdate()
    {
        Vector3 targetVelocity = new Vector3(
            moveDirection.x * moveSpeed,
            rb.linearVelocity.y,
            moveDirection.z * moveSpeed
        );
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, 10f * Time.fixedDeltaTime);
    }

    public void StopMovement()
    {
        inputValue = Vector2.zero;
        moveDirection = Vector3.zero;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }
    }

public bool IsMoving => moveDirection.magnitude > 0.1f;

}
