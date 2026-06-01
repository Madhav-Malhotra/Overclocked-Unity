using UnityEngine;

/// <summary>
/// Spawns all circuit trace lines between CPU pipeline stations at startup.
/// Finds stations by name in the scene — no manual wiring required.
/// </summary>
public class CircuitTraceSpawner : MonoBehaviour
{
    [SerializeField] private Material traceMaterial;
    [SerializeField] private float traceYOffset = 0.02f;
    [SerializeField] private float lineWidth = 0.28f;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseMinIntensity = 0.6f;
    [SerializeField] private float pulseMaxIntensity = 2.5f;

    private static readonly string[] StationOrder =
    {
        "Start", "Fetch", "Decode", "Execute", "Memory", "Writeback", "End"
    };

    private void Awake()
    {
        if (traceMaterial == null)
        {
            Debug.LogError("[CircuitTraceSpawner] traceMaterial is not assigned in the Inspector.");
            return;
        }

        for (int i = 0; i < StationOrder.Length - 1; i++)
        {
            string fromName = StationOrder[i];
            string toName   = StationOrder[i + 1];

            GameObject fromGO = GameObject.Find(fromName);
            GameObject toGO   = GameObject.Find(toName);

            if (fromGO == null || toGO == null)
            {
                Debug.LogWarning($"[CircuitTraceSpawner] Could not find '{fromName}' or '{toName}' in scene.");
                continue;
            }

            GameObject go = new GameObject($"{fromName}_to_{toName}");
            go.transform.SetParent(transform, false);

            CircuitTrace trace = go.AddComponent<CircuitTrace>();
            trace.traceMaterial     = traceMaterial;
            trace.lineWidth         = lineWidth;
            trace.pulseSpeed        = pulseSpeed;
            trace.pulseMinIntensity = pulseMinIntensity;
            trace.pulseMaxIntensity = pulseMaxIntensity;

            Vector3 fromPos = fromGO.transform.position;
            Vector3 toPos   = toGO.transform.position;
            fromPos.y = traceYOffset;
            toPos.y   = traceYOffset;

            trace.Setup(fromPos, toPos);
        }
    }
}
