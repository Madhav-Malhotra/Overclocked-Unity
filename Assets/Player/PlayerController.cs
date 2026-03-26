using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    private Rigidbody rb;
    private Vector2 inputValue;
    private Vector3 moveDirection;

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
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(
            moveDirection.x * moveSpeed,
            rb.linearVelocity.y,
            moveDirection.z * moveSpeed
        );
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
}
