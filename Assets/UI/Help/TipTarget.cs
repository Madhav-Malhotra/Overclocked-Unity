using System.Collections.Generic;
using UnityEngine;

// Marks a scene object that level tips can point at. Add it to the object and give it a unique `id`;
// then use that id as `target` in a level's tips (see Assets/UI/Help/README.md).
// Ids must be unique within a scene - duplicates are logged as errors.
public class TipTarget : MonoBehaviour
{
    public string id;

    static readonly Dictionary<string, TipTarget> registry = new();

    public static TipTarget Find(string id) =>
        id != null && registry.TryGetValue(id, out var t) && t != null ? t : null;

    void OnEnable()
    {
        if (string.IsNullOrEmpty(id)) return;
        if (registry.TryGetValue(id, out var other) && other != null && other != this)
            Debug.LogError($"TipTarget: duplicate id '{id}' on '{name}' and '{other.name}'.", this);
        registry[id] = this;
    }

    void OnDisable()
    {
        if (id != null && registry.TryGetValue(id, out var t) && t == this) registry.Remove(id);
    }

    // World-space bounds of every renderer under this object.
    public Bounds GetWorldBounds()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(transform.position, Vector3.one * 0.5f);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
