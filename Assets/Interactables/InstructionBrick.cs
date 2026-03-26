using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InstructionBrick : Interactable
{
    [Header("Brick Visuals")]
    [SerializeField] private Renderer brickRenderer;
    [SerializeField] private int[] stageMaterialSlots = Array.Empty<int>();
    [SerializeField] private int binaryMaterialSlot = -1;
    [SerializeField] private Material binaryFaceMaterial;
    [SerializeField] private Material[] stageMaterials = Array.Empty<Material>();
    [SerializeField] private PipelineStage currentStage = PipelineStage.Unprocessed;

    private Table parentTable;
    private Collider brickCollider;
    private Vector3 originalScale;

    protected override void Start()
    {
        if (brickRenderer == null)
        {
            brickRenderer = GetComponent<Renderer>();
        }

        base.Start();
        brickCollider = GetComponent<Collider>();
        originalScale = transform.localScale;
        ApplyStageMaterials(currentStage);
    }

    public PipelineStage CurrentStage => currentStage;

    public override bool CanBeHighlighted()
    {
        return false;
    }

    public override bool CanInteract()
    {
        return false;
    }

    public override void SetHighlighted(bool highlighted)
    {
        if (brickRenderer == null)
        {
            return;
        }

        Material[] materials = brickRenderer.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null || !material.HasProperty("_EmissionColor"))
            {
                continue;
            }

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }
    }

    public void SetParentTable(Table table)
    {
        parentTable = table;
    }

    public Table GetParentTable()
    {
        return parentTable;
    }

    public void EnablePhysics(bool enabled)
    {
        if (brickCollider != null)
        {
            brickCollider.enabled = enabled;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !enabled;
        }
    }

    public Vector3 GetOriginalScale()
    {
        return originalScale;
    }

    public void SetStage(PipelineStage stage)
    {
        currentStage = stage;
        ApplyStageMaterials(stage);
    }

    private void ApplyStageMaterials(PipelineStage stage)
    {
        if (brickRenderer == null)
        {
            return;
        }

        Material[] materials = brickRenderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
        {
            return;
        }

        Material stageMaterial = GetStageMaterial(stage);
        if (stageMaterial != null)
        {
            foreach (int slotIndex in GetStageMaterialSlots(materials.Length))
            {
                materials[slotIndex] = stageMaterial;
            }
        }

        if (binaryFaceMaterial != null && binaryMaterialSlot >= 0 && binaryMaterialSlot < materials.Length)
        {
            materials[binaryMaterialSlot] = binaryFaceMaterial;
        }

        brickRenderer.sharedMaterials = materials;
    }

    private Material GetStageMaterial(PipelineStage stage)
    {
        int stageIndex = (int)stage;
        if (stageMaterials == null || stageIndex < 0 || stageIndex >= stageMaterials.Length)
        {
            return null;
        }

        return stageMaterials[stageIndex];
    }

    private int[] GetStageMaterialSlots(int materialCount)
    {
        if (stageMaterialSlots != null && stageMaterialSlots.Length > 0)
        {
            return stageMaterialSlots;
        }

        int stageSlotCount = binaryMaterialSlot >= 0 && binaryMaterialSlot < materialCount
            ? materialCount - 1
            : materialCount;

        if (stageSlotCount <= 0)
        {
            return Array.Empty<int>();
        }

        int[] defaultSlots = new int[stageSlotCount];
        int nextIndex = 0;

        for (int slotIndex = 0; slotIndex < materialCount; slotIndex++)
        {
            if (slotIndex == binaryMaterialSlot)
            {
                continue;
            }

            defaultSlots[nextIndex] = slotIndex;
            nextIndex++;
        }

        return defaultSlots;
    }
}
