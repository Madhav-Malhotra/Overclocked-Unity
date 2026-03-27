using UnityEngine;
using UnityEngine.Serialization;

public class InstructionBrickHoldingSystem : MonoBehaviour
{
    [Header("Hold Settings")]
    [SerializeField] private Transform holdPosition;
    [SerializeField] private float heldScale = 0.5f;

    [FormerlySerializedAs("heldDisk")]
    [SerializeField] private InstructionBrick heldBrick;

    public bool IsHoldingBrick()
    {
        return heldBrick != null;
    }

    public InstructionBrick GetHeldBrick()
    {
        return heldBrick;
    }

    public void PickUpBrick(InstructionBrick brick)
    {
        if (brick == null)
        {
            Debug.LogWarning("InstructionBrickHoldingSystem: Attempted to pick up null brick");
            return;
        }

        if (holdPosition == null)
        {
            Debug.LogError("InstructionBrickHoldingSystem: holdPosition not assigned");
            return;
        }

        // Store reference
        heldBrick = brick;

        // Parent to hold position
        brick.transform.SetParent(holdPosition);
        brick.transform.localPosition = Vector3.zero;
        brick.transform.localRotation = Quaternion.identity;

        // Scale down
        Vector3 originalScale = brick.GetOriginalScale();
        brick.transform.localScale = originalScale * heldScale;

        // Disable physics
        brick.EnablePhysics(false);

        // Clear parent table reference
        brick.SetParentTable(null);

        // Turn off highlight while held
        brick.SetHighlighted(false);
    }

    // Overload for timer-based placement
    public bool PlaceBrick(Table targetTable, float processingDuration)
    {
        // Call existing PlaceBrick logic
        return PlaceBrick(targetTable);
        // Processing is managed by Table.StartProcessing()
    }

    public bool PlaceBrick(Table targetTable)
    {
        if (heldBrick == null)
        {
            Debug.LogWarning("InstructionBrickHoldingSystem: No brick to place");
            return false;
        }

        if (targetTable == null)
        {
            Debug.LogWarning("InstructionBrickHoldingSystem: Target table is null");
            return false;
        }

        Transform brickSlot = targetTable.GetBrickSlot();
        if (brickSlot == null)
        {
            Debug.LogError("InstructionBrickHoldingSystem: Target table has no brick slot");
            return false;
        }

        // Position at brick slot
        heldBrick.transform.SetParent(targetTable.transform);
        heldBrick.transform.position = brickSlot.position;
        heldBrick.transform.rotation = brickSlot.rotation;

        // Restore original scale
        heldBrick.transform.localScale = heldBrick.GetOriginalScale();

        // Enable physics
        heldBrick.EnablePhysics(true);

        // Update references
        targetTable.PlaceBrick(heldBrick);
        heldBrick.SetParentTable(targetTable);

        // Clear held brick
        heldBrick = null;
        return true;
    }
}
