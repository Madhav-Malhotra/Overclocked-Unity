using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Highlight Settings")]
    private Color highlightColor = Color.white;

    [Range(0f, 1f)]
    public float highlightIntersity = 0.3f;

    protected Renderer objectRenderer;

    protected virtual void Start()
    {
        objectRenderer = GetComponentInChildren<Renderer>();
        if (objectRenderer != null)
            objectRenderer.material.EnableKeyword("_EMISSION");
    }

    public virtual void SetHighlighted(bool highlighted)
    {
        if (objectRenderer == null) return;
        Color emission = highlighted ? highlightColor * highlightIntersity : Color.black;
        objectRenderer.material.SetColor("_EmissionColor", emission);
    }

    public virtual bool CanBeHighlighted()
    {
        return true;
    }

    public virtual bool CanInteract()
    {
        return true;
    }

    public virtual void OnInteract()
    {
        // Override in subclasses
    }
}
