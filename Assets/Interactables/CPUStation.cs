using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class CPUStation : Table
{
    [Header("CPU Stage")]
    [SerializeField] private PipelineStage assignedStage = PipelineStage.Unprocessed;

    [Header("Processing Settings")]
    [SerializeField] private bool requiresProcessing = true;
    [SerializeField] private GameObject processingTimerPrefab;

    [Header("CPU")]
    [SerializeField] private CPUController cpuController;

    [Header("Outline")]
    [SerializeField] private Color interactableOutlineColor = Color.white;
    [FormerlySerializedAs("processingOutlineColor")]
    [SerializeField] private Color blockedOutlineColor = Color.red;
    [SerializeField] private float outlineWidth = 2f;

    private const string OutlineMaskShaderName = "Overclocked/CPUStationOutlineMask";
    private static readonly HashSet<CPUStation> ActiveStations = new();

    private readonly Dictionary<Renderer, Renderer> outlineRendererBySource = new();
    private readonly HashSet<Renderer> outlineRenderers = new();

    private Material outlineMaskMaterial;
    private MaterialPropertyBlock outlinePropertyBlock;
    private bool isOutlineVisible;
    private TableProcessingTimer activeTimer;
    private bool isProcessing;
    private float processingEndTime = -1f;

    private bool IsProcessing => isProcessing;
    private bool RequiresProcessing => requiresProcessing;
    private CPUController CpuController => cpuController;

    protected override void Start()
    {
        base.Start();
        EnsureOutlineMaterial();
        SyncOutlineRenderers();
        ApplyOutlineVisibility(false);
    }

    void Update()
    {
        UpdateProcessing();
    }

    void OnEnable()
    {
        ActiveStations.Add(this);
    }

    void OnDisable()
    {
        ActiveStations.Remove(this);
    }

    void LateUpdate()
    {
        if (!isOutlineVisible)
        {
            return;
        }

        SyncOutlineRenderers();
        ApplyOutlineColor(GetOutlineColor());
        Shader.SetGlobalFloat("_CPUStationOutlineThickness", outlineWidth);

        if (CurrentBrick != null)
        {
            CurrentBrick.SetHighlighted(false);
        }
    }

    void OnDestroy()
    {
        foreach (KeyValuePair<Renderer, Renderer> pair in outlineRendererBySource)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }

        outlineRendererBySource.Clear();
        outlineRenderers.Clear();

        if (outlineMaskMaterial != null)
        {
            Destroy(outlineMaskMaterial);
        }
    }

    public override bool CanBeHighlighted()
    {
        return true;
    }

    public override bool CanInteract()
    {
        if (!base.CanInteract())
        {
            return false;
        }

        if (HasBrick)
        {
            return true;
        }

        return !IsInvalidPlacementForHeldBrick();
    }

    public override void SetHighlighted(bool highlighted)
    {
        if (objectRenderer != null && objectRenderer.material.HasProperty("_EmissionColor"))
        {
            objectRenderer.material.SetColor("_EmissionColor", Color.black);
        }

        if (CurrentBrick != null)
        {
            CurrentBrick.SetHighlighted(false);
        }

        isOutlineVisible = highlighted;
        SyncOutlineRenderers();
        ApplyOutlineVisibility(highlighted);

        if (!highlighted)
        {
            return;
        }

        ApplyOutlineColor(GetOutlineColor());
        Shader.SetGlobalFloat("_CPUStationOutlineThickness", outlineWidth);
    }

    public override void OnInteract()
    {
        if (HoldingSystem == null) return;

        if (HasBrick)
        {
            InstructionBrick brickToPickup = RemoveBrick();
            HoldingSystem.PickUpBrick(brickToPickup);
            CpuController?.GetALUOutput();
            return;
        }

        if (!RequiresProcessing)
        {
            if (!TryPlaceHeldBrick())
                Debug.LogWarning("CPUStation: Failed to place brick");
            return;
        }

        if (!TryPlaceHeldBrick())
        {
            Debug.LogWarning("CPUStation: Failed to place brick, skipping processing start");
            return;
        }

        StartProcessing(1f);
    }

    public override void PlaceBrick(InstructionBrick brick)
    {
        base.PlaceBrick(brick);
        CpuController?.TickCPU();
    }

    private void StartProcessing(float duration)
    {
        isProcessing = true;
        processingEndTime = Time.time + Mathf.Max(0f, duration);

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
                Debug.LogWarning("CPUStation: processingTimerPrefab is missing TableProcessingTimer component");
            }
        }
        else
        {
            Debug.LogWarning("CPUStation: processingTimerPrefab is not assigned. Processing will continue without UI.");
        }
    }

    private void OnProcessingComplete()
    {
        if (!isProcessing)
        {
            return;
        }

        isProcessing = false;
        processingEndTime = -1f;

        if (activeTimer != null)
        {
            Destroy(activeTimer.gameObject);
            activeTimer = null;
        }

        if (!RequiresProcessing || assignedStage == PipelineStage.Unprocessed)
        {
            return;
        }

        if (CurrentBrick != null)
        {
            CurrentBrick.SetStage(assignedStage);
        }
    }

    private void UpdateProcessing()
    {
        if (isProcessing && Time.time >= processingEndTime)
        {
            OnProcessingComplete();
        }
    }

    private void EnsureOutlineMaterial()
    {
        if (outlineMaskMaterial != null)
        {
            return;
        }

        Shader outlineMaskShader = Shader.Find(OutlineMaskShaderName);
        if (outlineMaskShader == null)
        {
            Debug.LogError($"CPUStation: Could not find outline mask shader '{OutlineMaskShaderName}'.");
            return;
        }

        outlineMaskMaterial = new Material(outlineMaskShader)
        {
            name = "CPUStationOutlineMaskMaterial"
        };

        outlinePropertyBlock ??= new MaterialPropertyBlock();
    }

    private void SyncOutlineRenderers()
    {
        EnsureOutlineMaterial();
        if (outlineMaskMaterial == null)
        {
            return;
        }

        Renderer[] sourceRenderers = GetComponentsInChildren<Renderer>(true);
        HashSet<Renderer> activeSources = new();

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            Renderer sourceRenderer = sourceRenderers[i];
            if (!ShouldOutline(sourceRenderer))
            {
                continue;
            }

            activeSources.Add(sourceRenderer);

            if (!outlineRendererBySource.ContainsKey(sourceRenderer))
            {
                CreateOutlineClone(sourceRenderer);
            }
        }

        List<Renderer> staleSources = new();
        foreach (KeyValuePair<Renderer, Renderer> pair in outlineRendererBySource)
        {
            if (pair.Key == null || !activeSources.Contains(pair.Key))
            {
                staleSources.Add(pair.Key);
            }
        }

        for (int i = 0; i < staleSources.Count; i++)
        {
            RemoveOutlineClone(staleSources[i]);
        }
    }

    private bool ShouldOutline(Renderer sourceRenderer)
    {
        if (sourceRenderer == null)
        {
            return false;
        }

        if (outlineRenderers.Contains(sourceRenderer))
        {
            return false;
        }

        if (sourceRenderer is MeshRenderer)
        {
            return sourceRenderer.GetComponent<MeshFilter>() != null;
        }

        return sourceRenderer is SkinnedMeshRenderer;
    }

    private void CreateOutlineClone(Renderer sourceRenderer)
    {
        GameObject cloneObject = new GameObject($"{sourceRenderer.gameObject.name}_OutlineMask");
        cloneObject.layer = sourceRenderer.gameObject.layer;
        cloneObject.transform.SetParent(sourceRenderer.transform, false);
        cloneObject.transform.localPosition = Vector3.zero;
        cloneObject.transform.localRotation = Quaternion.identity;
        cloneObject.transform.localScale = Vector3.one;

        Renderer outlineRenderer = null;

        if (sourceRenderer is MeshRenderer meshRenderer)
        {
            MeshFilter sourceFilter = meshRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                Destroy(cloneObject);
                return;
            }

            MeshFilter outlineFilter = cloneObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer cloneRenderer = cloneObject.AddComponent<MeshRenderer>();
            ConfigureOutlineRenderer(cloneRenderer, meshRenderer);
            outlineRenderer = cloneRenderer;
        }
        else if (sourceRenderer is SkinnedMeshRenderer skinnedRenderer)
        {
            if (skinnedRenderer.sharedMesh == null)
            {
                Destroy(cloneObject);
                return;
            }

            SkinnedMeshRenderer cloneRenderer = cloneObject.AddComponent<SkinnedMeshRenderer>();
            cloneRenderer.sharedMesh = skinnedRenderer.sharedMesh;
            cloneRenderer.rootBone = skinnedRenderer.rootBone;
            cloneRenderer.bones = skinnedRenderer.bones;
            cloneRenderer.localBounds = skinnedRenderer.localBounds;
            cloneRenderer.updateWhenOffscreen = true;
            ConfigureOutlineRenderer(cloneRenderer, skinnedRenderer);
            outlineRenderer = cloneRenderer;
        }

        if (outlineRenderer == null)
        {
            Destroy(cloneObject);
            return;
        }

        outlineRenderers.Add(outlineRenderer);
        outlineRendererBySource[sourceRenderer] = outlineRenderer;
        outlineRenderer.enabled = isOutlineVisible;
    }

    private void ConfigureOutlineRenderer(Renderer outlineRenderer, Renderer sourceRenderer)
    {
        outlineRenderer.sharedMaterial = outlineMaskMaterial;
        outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        outlineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        outlineRenderer.allowOcclusionWhenDynamic = false;
        outlineRenderer.renderingLayerMask = sourceRenderer.renderingLayerMask;
    }

    private void RemoveOutlineClone(Renderer sourceRenderer)
    {
        if (!outlineRendererBySource.TryGetValue(sourceRenderer, out Renderer outlineRenderer))
        {
            return;
        }

        if (outlineRenderer != null)
        {
            outlineRenderers.Remove(outlineRenderer);
            Destroy(outlineRenderer.gameObject);
        }

        outlineRendererBySource.Remove(sourceRenderer);
    }

    private void ApplyOutlineVisibility(bool visible)
    {
        foreach (KeyValuePair<Renderer, Renderer> pair in outlineRendererBySource)
        {
            if (pair.Value != null)
            {
                pair.Value.enabled = visible;
            }
        }
    }

    private void ApplyOutlineColor(Color color)
    {
        outlinePropertyBlock ??= new MaterialPropertyBlock();
        outlinePropertyBlock.SetColor("_MaskColor", color);

        foreach (KeyValuePair<Renderer, Renderer> pair in outlineRendererBySource)
        {
            if (pair.Value != null)
            {
                pair.Value.SetPropertyBlock(outlinePropertyBlock);
            }
        }
    }

    internal static void DrawVisibleOutlineMasks(CommandBuffer cmd)
    {
        foreach (CPUStation station in ActiveStations)
        {
            station?.DrawOutlineMasks(cmd);
        }
    }

    internal static void DrawVisibleOutlineMasks(RasterCommandBuffer cmd)
    {
        foreach (CPUStation station in ActiveStations)
        {
            station?.DrawOutlineMasks(cmd);
        }
    }

    private void DrawOutlineMasks(CommandBuffer cmd)
    {
        if (!isOutlineVisible || outlineMaskMaterial == null)
        {
            return;
        }

        SyncOutlineRenderers();

        foreach (KeyValuePair<Renderer, Renderer> pair in outlineRendererBySource)
        {
            Renderer outlineRenderer = pair.Value;
            if (outlineRenderer == null || !outlineRenderer.enabled || !outlineRenderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            cmd.DrawRenderer(outlineRenderer, outlineMaskMaterial);
        }
    }

    private void DrawOutlineMasks(RasterCommandBuffer cmd)
    {
        if (!isOutlineVisible || outlineMaskMaterial == null)
        {
            return;
        }

        SyncOutlineRenderers();

        foreach (KeyValuePair<Renderer, Renderer> pair in outlineRendererBySource)
        {
            Renderer outlineRenderer = pair.Value;
            if (outlineRenderer == null || !outlineRenderer.enabled || !outlineRenderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            cmd.DrawRenderer(outlineRenderer, outlineMaskMaterial);
        }
    }

    private Color GetOutlineColor()
    {
        return IsProcessing || IsInvalidPlacementForHeldBrick()
            ? blockedOutlineColor
            : interactableOutlineColor;
    }

    private bool IsInvalidPlacementForHeldBrick()
    {
        if (HoldingSystem == null || !HoldingSystem.IsHoldingBrick() || HasBrick)
        {
            return false;
        }

        if (assignedStage == PipelineStage.Unprocessed)
        {
            return false;
        }

        InstructionBrick heldBrick = HoldingSystem.GetHeldBrick();
        if (heldBrick == null)
        {
            return false;
        }

        return heldBrick.CurrentStage != GetRequiredInputStage(assignedStage);
    }

    private static PipelineStage GetRequiredInputStage(PipelineStage outputStage)
    {
        return outputStage switch
        {
            PipelineStage.Fetch => PipelineStage.Unprocessed,
            PipelineStage.Decode => PipelineStage.Fetch,
            PipelineStage.Execute => PipelineStage.Decode,
            PipelineStage.Memory => PipelineStage.Execute,
            PipelineStage.Writeback => PipelineStage.Memory,
            _ => PipelineStage.Unprocessed
        };
    }
}
