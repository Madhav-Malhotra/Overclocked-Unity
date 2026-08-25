using UnityEngine;

/// <summary>
/// Spawns all circuit trace lines between CPU pipeline stations at startup.
/// Discovers stations via CPUStation.AssignedStage/AssignedWay rather than scene-object
/// naming, so two parallel way lines sharing stage names (Superscalar) don't collide —
/// GameObject.Find-by-name can only ever resolve one "Fetch" object.
/// Start/End platforms are still found by name since they are shared across ways.
/// </summary>
public class CircuitTraceSpawner : MonoBehaviour
{
    [SerializeField] private Material traceMaterial;
    [SerializeField] private float traceYOffset = 0.02f;
    [SerializeField] private float lineWidth = 0.28f;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseMinIntensity = 0.6f;
    [SerializeField] private float pulseMaxIntensity = 2.5f;

    private const string StartName = "Start";
    private const string EndName = "End";

    private static readonly PipelineStage[] StageOrder =
    {
        PipelineStage.Fetch, PipelineStage.Decode, PipelineStage.Execute,
        PipelineStage.Memory, PipelineStage.Writeback,
    };

    private void Awake()
    {
        if (traceMaterial == null)
        {
            Debug.LogError("[CircuitTraceSpawner] traceMaterial is not assigned in the Inspector.");
            return;
        }

        CPUStation[] stations = FindObjectsByType<CPUStation>(FindObjectsSortMode.None);
        int wayCount = 1;
        foreach (var station in stations)
            wayCount = Mathf.Max(wayCount, station.AssignedWay + 1);

        for (int way = 0; way < wayCount; way++)
        {
            GameObject prevGO = GameObject.Find(StartName);
            string prevLabel = StartName;

            foreach (PipelineStage stage in StageOrder)
            {
                CPUStation station = FindStation(stations, stage, way);
                if (station == null)
                {
                    Debug.LogWarning($"[CircuitTraceSpawner] Could not find station for stage {stage}, way {way}.");
                    prevGO = null;
                    continue;
                }

                if (prevGO != null)
                    SpawnTrace($"{prevLabel}_to_{stage}_w{way}", prevGO.transform.position, station.transform.position);

                prevGO = station.gameObject;
                prevLabel = $"{stage}_w{way}";
            }

            GameObject endGO = GameObject.Find(EndName);
            if (prevGO != null && endGO != null)
                SpawnTrace($"{prevLabel}_to_{EndName}", prevGO.transform.position, endGO.transform.position);
        }
    }

    private static CPUStation FindStation(CPUStation[] stations, PipelineStage stage, int way)
    {
        foreach (var station in stations)
        {
            if (station != null && station.AssignedStage == stage && station.AssignedWay == way)
                return station;
        }
        return null;
    }

    private void SpawnTrace(string name, Vector3 fromPos, Vector3 toPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        CircuitTrace trace = go.AddComponent<CircuitTrace>();
        trace.traceMaterial     = traceMaterial;
        trace.lineWidth         = lineWidth;
        trace.pulseSpeed        = pulseSpeed;
        trace.pulseMinIntensity = pulseMinIntensity;
        trace.pulseMaxIntensity = pulseMaxIntensity;

        fromPos.y = traceYOffset;
        toPos.y   = traceYOffset;

        trace.Setup(fromPos, toPos);
    }
}
