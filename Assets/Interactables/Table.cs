using UnityEngine;
using UnityEngine.Serialization;

public class Table : Interactable
{
    [Header("Table Settings")]
    [FormerlySerializedAs("currentDisk")]
    [SerializeField] private InstructionBrick currentBrick;
    [FormerlySerializedAs("diskSlot")]
    [SerializeField] public Transform brickSlot;

    private InstructionBrickHoldingSystem holdingSystem;

    public bool HasBrick => currentBrick != null;
    public InstructionBrick CurrentBrick => currentBrick;
    protected InstructionBrickHoldingSystem HoldingSystem => holdingSystem;

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

        // Initialise any brick already placed on this table in the scene (e.g. Start station).
        if (currentBrick != null)
        {
            currentBrick.SetParentTable(this);
        }
    }

    public override bool CanBeHighlighted()
    {
        return CanInteract();
    }

    public override bool CanInteract()
    {
        if (holdingSystem == null) return false;

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
            InstructionBrick brickToPickup = RemoveBrick();
            holdingSystem.PickUpBrick(brickToPickup);
            return;
        }

        if (!TryPlaceHeldBrick())
        {
            Debug.LogWarning("Table: Failed to place brick");
        }
    }

    protected bool TryPlaceHeldBrick()
    {
        if (holdingSystem == null)
        {
            return false;
        }

        return holdingSystem.PlaceBrick(this);
    }

    public virtual void PlaceBrick(InstructionBrick brick)
    {
        if (brick == null) return;

        currentBrick = brick;
        brick.SetParentTable(this);
    }

public InstructionBrick RemoveBrick()
    {
        InstructionBrick brick = currentBrick;
        currentBrick = null;
        OnBrickRemoved();
        return brick;
    }

protected virtual void OnBrickRemoved() { }


    public override void SetHighlighted(bool highlighted)
    {
        base.SetHighlighted(highlighted);
    }

    public Transform GetBrickSlot()
    {
        return brickSlot;
    }

public void SetBrickSlot(Transform slot) { brickSlot = slot; }

}
