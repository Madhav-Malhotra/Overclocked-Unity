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

    private const float bobAmplitude  = 0.18f;
    private const float bobFrequency  = 0.6f;
    private const float bobBaseHeight = 1.0f;
    private const float bobXOffset    = 0.0f;

    private Table parentTable;
    private bool  _isPlaced;
    private float _bobPhase;

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

        // If already sitting on a table (e.g. the Start station in the scene),
        // initialise the bob and rotation as if it was just placed.
        if (parentTable != null)
        {
            SetParentTable(parentTable);
        }
    }

private void Update()
    {
        if (!_isPlaced) return;
        _bobPhase += Time.deltaTime * bobFrequency * Mathf.PI * 2f;
        float yOffset = Mathf.Sin(_bobPhase) * bobAmplitude;
        transform.localPosition = new Vector3(bobXOffset, bobBaseHeight + yOffset, 0f);
    }

    
    public uint InstructionPc { get; private set; }
    public void SetInstructionPc(uint pc) { InstructionPc = pc; }
    // Which superscalar way this brick is currently routed to. Unused (always 0) outside the
    // Superscalar scene. Not assigned at spawn — the player routes it when placing at Fetch.
    public int Way { get; private set; }
    public void SetWay(int way) { Way = way; }
    public uint InstructionHex { get; private set; }
    public void SetInstructionHex(uint hex) { InstructionHex = hex; }
    public string InstructionLabel { get; private set; }
    public void SetInstructionLabel(string label) { InstructionLabel = label; }
    public bool HasBeenDecoded { get; private set; }
    public void MarkDecoded() { HasBeenDecoded = true; }
    public byte DestRegAddr { get; private set; }
    public uint DestRegValue { get; private set; }
    public bool HasDestReg { get; private set; }
    public void SetDestReg(byte addr, uint value) { DestRegAddr = addr; DestRegValue = value; HasDestReg = true; }
    public uint CapturedMemAddr { get; private set; }
    public bool HasCapturedMemAddr { get; private set; }
    public void CaptureMemAddr(uint addr) { CapturedMemAddr = addr; HasCapturedMemAddr = true; }
    public bool IsProcessed { get; private set; }
    public void MarkProcessed() { IsProcessed = true; }
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
        _isPlaced = (table != null);

        if (_isPlaced)
        {
            _bobPhase = 0f;
            transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            transform.localPosition = new Vector3(bobXOffset, bobBaseHeight, 0f);
            // Keep kinematic while bobbing so physics doesn't fight the animation
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
        else
        {
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
        }
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
