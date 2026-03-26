using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InteractableDetector detector;
    [SerializeField] private InstructionBrickHoldingSystem holdingSystem;
    [SerializeField] private InteractionUIManager uiManager;

    private InputAction interactAction;

    void Awake()
    {
        // Get component references if not assigned
        if (detector == null)
            detector = GetComponent<InteractableDetector>();

        if (holdingSystem == null)
            holdingSystem = GetComponent<InstructionBrickHoldingSystem>();

        // Set up input action reference
        // Note: Assumes InputSystem_Actions asset has C# class generation enabled
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
        }
        else
        {
            Debug.LogWarning("PlayerInteractionHandler: PlayerInput component not found. Using direct InputSystem reference.");
            // Fallback: create action directly from input system
            interactAction = new InputAction("Interact", binding: "<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonNorth");
        }
    }

    void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.performed += OnInteractPerformed;
            interactAction.Enable();
        }
    }

    void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction.Disable();
        }
    }

    void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (detector == null) return;

        Interactable currentHighlighted = detector.GetCurrentHighlighted();

        // Check if there's a valid interactable
        if (currentHighlighted == null || !currentHighlighted.CanInteract())
            return;

        // Trigger the interaction
        currentHighlighted.OnInteract();
    }

    void Update()
    {
        if (detector == null || holdingSystem == null || uiManager == null)
            return;

        Interactable currentHighlighted = detector.GetCurrentHighlighted();
        bool canInteract = currentHighlighted != null && currentHighlighted.CanInteract();

        if (canInteract)
        {
            bool isHolding = holdingSystem.IsHoldingBrick();
            uiManager.UpdatePrompt(isHolding, true);
        }
        else
        {
            uiManager.UpdatePrompt(false, false);
        }
    }
}
