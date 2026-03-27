using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Renders a circuit-board-style trace between two world-space points.
/// The path uses 45-degree diagonal segments (like PCB traces).
/// The line pulses with a cyan glow using URP emission.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class CircuitTrace : MonoBehaviour
{
    public float lineWidth = 0.28f;
    public float pulseSpeed = 1.5f;
    public float pulseMinIntensity = 0.6f;
    public float pulseMaxIntensity = 2.5f;

    // Assigned by CircuitTraceSpawner — must be a project asset so the shader is included in builds
    public Material traceMaterial;

    private Vector3 _startPoint;
    private Vector3 _endPoint;
    private LineRenderer _lineRenderer;
    private Material _instanceMaterial;

    private static readonly Color CyanBase = new Color(0f, 1f, 1f, 1f);

public void Setup(Vector3 from, Vector3 to)
    {
        _startPoint = from;
        _endPoint = to;
        if (_lineRenderer == null)
            _lineRenderer = GetComponent<LineRenderer>();
        BuildMaterial();
        BuildPath();
    }

private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        // BuildMaterial is called by Setup() after traceMaterial is assigned by the spawner
    }

    private void BuildMaterial()
    {
        if (traceMaterial == null)
        {
            Debug.LogError("[CircuitTrace] traceMaterial is not assigned. Assign a URP/Unlit material asset.");
            return;
        }

        // Instance the material so each trace can have its own emission value
        _instanceMaterial = new Material(traceMaterial);
        _instanceMaterial.SetColor("_BaseColor", CyanBase);

        _lineRenderer.material = _instanceMaterial;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.numCornerVertices = 4;
        _lineRenderer.numCapVertices = 4;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;
    }

    private void BuildPath()
    {
        List<Vector3> points = ComputeCircuitPath(_startPoint, _endPoint);
        _lineRenderer.positionCount = points.Count;
        _lineRenderer.SetPositions(points.ToArray());
    }

    private static List<Vector3> ComputeCircuitPath(Vector3 from, Vector3 to)
    {
        float y = from.y;
        float dx = to.x - from.x;
        float dz = to.z - from.z;
        float diag = Mathf.Min(Mathf.Abs(dx), Mathf.Abs(dz));

        var pts = new List<Vector3>();
        pts.Add(from);

        if (Mathf.Abs(dx) >= Mathf.Abs(dz))
        {
            float signX = Mathf.Sign(dx);
            float signZ = Mathf.Sign(dz);
            pts.Add(new Vector3(from.x + (dx - signX * diag), y, from.z));
            pts.Add(new Vector3(to.x, y, from.z + signZ * diag));
        }
        else
        {
            float signX = Mathf.Sign(dx);
            float signZ = Mathf.Sign(dz);
            pts.Add(new Vector3(from.x, y, from.z + (dz - signZ * diag)));
            pts.Add(new Vector3(from.x + signX * diag, y, to.z));
        }

        pts.Add(to);
        return pts;
    }

private void Update()
    {
        if (_instanceMaterial == null) return;
        // Sin gives a smooth wave; pow biases toward dark so bright peak looks symmetric
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        t = Mathf.Pow(t, 3f);
        float intensity = Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity, t);
        Color emissive = CyanBase * intensity;
        _instanceMaterial.SetColor("_BaseColor", emissive);
    }
}
