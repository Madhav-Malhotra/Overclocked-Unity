using UnityEngine;
using UnityEngine.Serialization;

public class Table : Interactable
{
    [Header("Table Settings")]
    [FormerlySerializedAs("currentDisk")]
    [SerializeField] private InstructionBrick currentBrick;
    [FormerlySerializedAs("diskSlot")]
    [SerializeField] private Transform brickSlot;

    [Header("Processing Settings")]
    [SerializeField] private bool requiresProcessing = true;
    [SerializeField] private GameObject processingTimerPrefab;

    [Header("UI References")]
    [SerializeField] private TimerSelectionUI timerSelectionUI;

    [Header("CPU")]
    [SerializeField] private CPUController cpuController;

    private InstructionBrickHoldingSystem holdingSystem;
    private TableProcessingTimer activeTimer;
    private bool isProcessing = false;
    private float processingEndTime = -1f;

    public bool HasBrick => currentBrick != null;
    public bool IsProcessing => isProcessing;
    public bool RequiresProcessing => requiresProcessing;
    protected InstructionBrick CurrentBrick => currentBrick;

    protected override void Start()
    {
        base.Start();
        // Find the InstructionBrickHoldingSystem on the player
        holdingSystem = FindFirstObjectByType<InstructionBrickHoldingSystem>();

        if (holdingSystem == null)
        {
            Debug.LogError("Table: Could not find InstructionBrickHoldingSystem in scene");
        }

        if (brickSlot == null)
        {
            Debug.LogWarning("Table: brickSlot not assigned");
        }

        if (timerSelectionUI == null)
        {
            timerSelectionUI = FindFirstObjectByType<TimerSelectionUI>();
        }
    }

    void Update()
    {
        if (isProcessing && Time.time >= processingEndTime)
        {
            OnProcessingComplete();
        }
    }

    public override bool CanBeHighlighted()
    {
        return CanInteract();
    }

    public override bool CanInteract()
    {
        if (holdingSystem == null) return false;

        // Cannot interact while processing
        if (isProcessing) return false;

        // Can pick up if player is NOT holding and this table HAS a brick
        if (!holdingSystem.IsHoldingBrick() && HasBrick)
        {
            return true;
        }

        // Can place if player IS holding and this table has NO brick
        if (holdingSystem.IsHoldingBrick() && !HasBrick)
        {
            return true;
        }

        return false;
    }

    public override void OnInteract()
    {
        if (holdingSystem == null) return;

        if (HasBrick)
        {
            // Pickup flow
            InstructionBrick brickToPickup = RemoveBrick();
            holdingSystem.PickUpBrick(brickToPickup);

            cpuController.GetALUOutput();
        }
        else
        {
            if (!requiresProcessing)
            {
                PlaceBrickWithoutProcessing();
                return;
            }

            // Place flow - show timer selection popup
            if (timerSelectionUI != null)
            {
                timerSelectionUI.ShowPopup(OnTimerSelected);
            }
            else
            {
                Debug.LogError("Table: TimerSelectionUI not found");
                // Fallback: place immediately with 3s default
                OnTimerSelected(3f);
            }
        }
    }

    private void OnTimerSelected(float duration)
    {
        if (holdingSystem == null) return;

        // Place brick first; only start processing if placement succeeded.
        bool placed = holdingSystem.PlaceBrick(this, duration);
        if (!placed)
        {
            Debug.LogWarning("Table: Failed to place brick, skipping processing start");
            return;
        }

        // Start processing state
        StartProcessing(duration);
    }

    private void PlaceBrickWithoutProcessing()
    {
        if (holdingSystem == null) return;

        bool placed = holdingSystem.PlaceBrick(this);
        if (!placed)
        {
            Debug.LogWarning("Table: Failed to place brick");
        }
    }

    private void StartProcessing(float duration)
    {
        isProcessing = true;
        processingEndTime = Time.time + Mathf.Max(0f, duration);

        // Spawn timer bar
        if (processingTimerPrefab != null)
        {
            GameObject timerObj = Instantiate(processingTimerPrefab, transform);
            activeTimer = timerObj.GetComponent<TableProcessingTimer>();

            if (activeTimer != null)
            {
                activeTimer.Initialize(duration, transform);
            }
            else
            {
                Debug.LogWarning("Table: processingTimerPrefab is missing TableProcessingTimer component");
            }
        }
        else
        {
            Debug.LogWarning("Table: processingTimerPrefab is not assigned. Processing will continue without UI.");
        }
    }

    protected virtual void OnProcessingComplete()
    {
        if (!isProcessing) return;

        isProcessing = false;
        processingEndTime = -1f;

        if (activeTimer != null)
        {
            Destroy(activeTimer.gameObject);
            activeTimer = null;
        }
    }

    public void PlaceBrick(InstructionBrick brick)
    {
        if (brick == null) return;

        currentBrick = brick;
        brick.SetParentTable(this);

        cpuController.TickCPU();
    }

    public InstructionBrick RemoveBrick()
    {
        InstructionBrick brick = currentBrick;
        currentBrick = null;
        return brick;
    }

    public override void SetHighlighted(bool highlighted)
    {
        base.SetHighlighted(highlighted);
    }

    public Transform GetBrickSlot()
    {
        return brickSlot;
    }
}
