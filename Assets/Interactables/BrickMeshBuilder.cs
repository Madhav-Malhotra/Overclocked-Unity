using UnityEngine;

/// <summary>
/// Builds a procedural box mesh with two submeshes:
///   submesh 0 = top face   (for the binary/dark-blue material)
///   submesh 1 = all other faces (sides + bottom, for the stage-colour material)
/// Dimensions default to a Lego-2x4-like flat brick (width > depth > height).
/// Call BuildMesh() or let Awake do it automatically.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BrickMeshBuilder : MonoBehaviour
{
    [Header("Brick Dimensions (local units)")]
    [SerializeField] private float width  = 1.0f;
    [SerializeField] private float height = 0.35f;
    [SerializeField] private float depth  = 0.6f;

    private void Awake()
    {
        BuildMesh();
    }

    private void OnValidate()
    {
        // Rebuild in editor when values change in the Inspector
        BuildMesh();
    }

    public void BuildMesh()
    {
        float hw = width  * 0.5f;
        float hh = height * 0.5f;
        float hd = depth  * 0.5f;

        // 8 corners
        Vector3 p000 = new Vector3(-hw, -hh, -hd);
        Vector3 p100 = new Vector3( hw, -hh, -hd);
        Vector3 p110 = new Vector3( hw,  hh, -hd);
        Vector3 p010 = new Vector3(-hw,  hh, -hd);
        Vector3 p001 = new Vector3(-hw, -hh,  hd);
        Vector3 p101 = new Vector3( hw, -hh,  hd);
        Vector3 p111 = new Vector3( hw,  hh,  hd);
        Vector3 p011 = new Vector3(-hw,  hh,  hd);

        // ---- Submesh 0: TOP face ----
        // Verts: p010=(-hw,+hh,-hd), p110=(+hw,+hh,-hd), p111=(+hw,+hh,+hd), p011=(-hw,+hh,+hd)
        // Map U along Z (depth, the long axis) and V along X (width, the short axis)
        // so a landscape texture reads along the long side of the brick.
        // p010: Z=-hd→U=0, X=-hw→V=0
        // p110: Z=-hd→U=0, X=+hw→V=1
        // p111: Z=+hd→U=1, X=+hw→V=1
        // p011: Z=+hd→U=1, X=-hw→V=0
        Vector3[] topVerts = { p010, p110, p111, p011 };
        Vector2[] topUVs   = { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
        Vector3[] topNorms = { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        int[]     topTris  = { 0, 2, 1,  0, 3, 2 };

        // ---- Submesh 1: BOTTOM + 4 SIDES ----
        // Bottom (-Y): p000, p100, p101, p001
        // Front  (-Z): p000, p010, p110, p100
        // Back   (+Z): p101, p111, p011, p001
        // Left   (-X): p001, p011, p010, p000
        // Right  (+X): p100, p110, p111, p101

        Vector3[] sideVerts = new Vector3[20];
        Vector2[] sideUVs   = new Vector2[20];
        Vector3[] sideNorms = new Vector3[20];

        // bottom
        sideVerts[0]  = p000; sideVerts[1]  = p100; sideVerts[2]  = p101; sideVerts[3]  = p001;
        sideNorms[0]  = Vector3.down; sideNorms[1] = Vector3.down; sideNorms[2] = Vector3.down; sideNorms[3] = Vector3.down;
        // front
        sideVerts[4]  = p000; sideVerts[5]  = p010; sideVerts[6]  = p110; sideVerts[7]  = p100;
        sideNorms[4]  = Vector3.back; sideNorms[5] = Vector3.back; sideNorms[6] = Vector3.back; sideNorms[7] = Vector3.back;
        // back
        sideVerts[8]  = p101; sideVerts[9]  = p111; sideVerts[10] = p011; sideVerts[11] = p001;
        sideNorms[8]  = Vector3.forward; sideNorms[9] = Vector3.forward; sideNorms[10] = Vector3.forward; sideNorms[11] = Vector3.forward;
        // left
        sideVerts[12] = p001; sideVerts[13] = p011; sideVerts[14] = p010; sideVerts[15] = p000;
        sideNorms[12] = Vector3.left; sideNorms[13] = Vector3.left; sideNorms[14] = Vector3.left; sideNorms[15] = Vector3.left;
        // right
        sideVerts[16] = p100; sideVerts[17] = p110; sideVerts[18] = p111; sideVerts[19] = p101;
        sideNorms[16] = Vector3.right; sideNorms[17] = Vector3.right; sideNorms[18] = Vector3.right; sideNorms[19] = Vector3.right;

        for (int i = 0; i < 20; i++)
        {
            int face = i / 4;
            int corner = i % 4;
            float u = (corner == 1 || corner == 2) ? 1f : 0f;
            float v = (corner == 2 || corner == 3) ? 1f : 0f;
            sideUVs[i] = new Vector2(u, v);
        }

        // Each face: verts i*4+0..3, tris: 0,2,1 / 0,3,2
        int[] sideTris = new int[30]; // 5 faces * 2 tris * 3 verts
        for (int face = 0; face < 5; face++)
        {
            int b = face * 4;
            int t = face * 6;
            sideTris[t+0] = b+0; sideTris[t+1] = b+2; sideTris[t+2] = b+1;
            sideTris[t+3] = b+0; sideTris[t+4] = b+3; sideTris[t+5] = b+2;
        }

        // Combine all vertices
        int topCount  = topVerts.Length;
        int sideCount = sideVerts.Length;
        Vector3[] allVerts = new Vector3[topCount + sideCount];
        Vector2[] allUVs   = new Vector2[topCount + sideCount];
        Vector3[] allNorms = new Vector3[topCount + sideCount];
        System.Array.Copy(topVerts,  0, allVerts, 0,        topCount);
        System.Array.Copy(sideVerts, 0, allVerts, topCount, sideCount);
        System.Array.Copy(topUVs,    0, allUVs,   0,        topCount);
        System.Array.Copy(sideUVs,   0, allUVs,   topCount, sideCount);
        System.Array.Copy(topNorms,  0, allNorms,  0,        topCount);
        System.Array.Copy(sideNorms, 0, allNorms,  topCount, sideCount);

        // Offset side triangle indices by topCount
        for (int i = 0; i < sideTris.Length; i++)
            sideTris[i] += topCount;

        UnityEngine.Mesh mesh = new UnityEngine.Mesh();
        mesh.name = "BrickMesh";
        mesh.vertices  = allVerts;
        mesh.uv        = allUVs;
        mesh.normals   = allNorms;
        mesh.subMeshCount = 2;
        mesh.SetTriangles(topTris,  0);
        mesh.SetTriangles(sideTris, 1);
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;

        // Resize the BoxCollider to match
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            bc.center = Vector3.zero;
            bc.size   = new Vector3(width, height, depth);
        }
    }
}
