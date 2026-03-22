using UnityEngine;

public class Table : Interactable
{
    [Header("Table Settings")]
    [SerializeField] private Disk currentDisk;
    [SerializeField] private Transform diskSlot;

    [Header("Processing Settings")]
    [SerializeField] private GameObject processingTimerPrefab;
    [SerializeField] private Color processingHighlightColor = new Color(1f, 0.3f, 0f); // Red/Orange

    [Header("UI References")]
    [SerializeField] private TimerSelectionUI timerSelectionUI;

    [Header("CPU")]
    [SerializeField] private CPUController cpuController;

    private DiskHoldingSystem holdingSystem;
    private TableProcessingTimer activeTimer;
    private bool isProcessing = false;
    private bool isCurrentlyHighlighted = false;
    private float processingEndTime = -1f;

    public bool HasDisk => currentDisk != null;
    public bool IsProcessing => isProcessing;

    protected override void Start()
    {
        base.Start();
        // Find the DiskHoldingSystem on the player
        holdingSystem = FindFirstObjectByType<DiskHoldingSystem>();

        if (holdingSystem == null)
        {
            Debug.LogError("Table: Could not find DiskHoldingSystem in scene");
        }

        if (diskSlot == null)
        {
            Debug.LogWarning("Table: diskSlot not assigned");
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
        // Processing tables should always be highlighted (to show red/orange color)
        if (isProcessing) return true;

        // Non-processing tables should only be highlighted if they can be interacted with
        return CanInteract();
    }

    public override bool CanInteract()
    {
        if (holdingSystem == null) return false;

        // Cannot interact while processing
        if (isProcessing) return false;

        // Can pick up if player is NOT holding and this table HAS a disk
        if (!holdingSystem.IsHoldingDisk() && HasDisk)
        {
            return true;
        }

        // Can place if player IS holding and this table has NO disk
        if (holdingSystem.IsHoldingDisk() && !HasDisk)
        {
            return true;
        }

        return false;
    }

    public override void OnInteract()
    {
        if (holdingSystem == null) return;

        if (HasDisk)
        {
            // Pickup flow 
            Disk diskToPickup = RemoveDisk();
            holdingSystem.PickUpDisk(diskToPickup);

            cpuController.GetALUOutput();
        }
        else
        {
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

        // Place disk first; only start processing if placement succeeded.
        bool placed = holdingSystem.PlaceDisk(this, duration);
        if (!placed)
        {
            Debug.LogWarning("Table: Failed to place disk, skipping processing start");
            return;
        }

        // Start processing state
        StartProcessing(duration);
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

        // Update highlight to processing color
        RefreshHighlight();
    }

    private void OnProcessingComplete()
    {
        if (!isProcessing) return;

        isProcessing = false;
        processingEndTime = -1f;

        if (activeTimer != null)
        {
            Destroy(activeTimer.gameObject);
            activeTimer = null;
        }

        // Restore normal highlight
        RefreshHighlight();
    }

    public void PlaceDisk(Disk disk)
    {
        if (disk == null) return;

        currentDisk = disk;
        disk.SetParentTable(this);

        cpuController.TickCPU();
    }

    public Disk RemoveDisk()
    {
        Disk disk = currentDisk;
        currentDisk = null;
        return disk;
    }

    public override void SetHighlighted(bool highlighted)
    {
        isCurrentlyHighlighted = highlighted;

        if (isProcessing)
        {
            // Use red/orange processing color
            Color emission = highlighted ? processingHighlightColor * highlightIntersity : Color.black;
            objectRenderer.material.SetColor("_EmissionColor", emission);
        }
        else
        {
            // Normal white highlight
            base.SetHighlighted(highlighted);
        }

        // Keep disk highlight in lockstep with the table state/color.
        if (currentDisk != null)
        {
            currentDisk.SetHighlightColor(isProcessing ? processingHighlightColor : Color.white);
            currentDisk.SetHighlighted(highlighted);
        }
    }

    private void RefreshHighlight()
    {
        // Re-apply the current highlight state with updated processing/disk behavior.
        SetHighlighted(isCurrentlyHighlighted);
    }

    public Transform GetDiskSlot()
    {
        return diskSlot;
    }
}
