using System.Collections;
using UnityEngine;

public class EndPlatform : Table
{
    [SerializeField] private Color depositHighlightColor = new Color(0f, 1f, 0.4f);
    [SerializeField] private float brickDestroyDelay = 1.0f;

    public override bool CanBeHighlighted()
    {
        return CanInteract();
    }

    public override bool CanInteract()
    {
        if (HoldingSystem == null) return false;

        if (!HoldingSystem.IsHoldingBrick()) return false;
        if (HasBrick) return false;

        InstructionBrick held = HoldingSystem.GetHeldBrick();
        return held != null && held.CurrentStage == PipelineStage.Writeback;
    }

public override void OnInteract()
    {
        if (!TryPlaceHeldBrick())
        {
            Debug.LogWarning("EndPlatform: Failed to place brick");
            return;
        }

        LevelManager.Instance?.OnInstructionCompleted();
        StartCoroutine(DestroyBrickAfterDelay());
    }

    public override void SetHighlighted(bool highlighted)
    {
        base.SetHighlighted(highlighted);

        if (highlighted && objectRenderer != null && objectRenderer.material.HasProperty("_EmissionColor"))
        {
            objectRenderer.material.EnableKeyword("_EMISSION");
            objectRenderer.material.SetColor("_EmissionColor", depositHighlightColor * 0.6f);
        }
    }

    private IEnumerator DestroyBrickAfterDelay()
    {
        yield return new WaitForSeconds(brickDestroyDelay);

        InstructionBrick brick = CurrentBrick;
        RemoveBrick();
        if (brick != null)
            Destroy(brick.gameObject);
    }
}
