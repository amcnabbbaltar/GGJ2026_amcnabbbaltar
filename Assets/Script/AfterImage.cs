using System.Collections.Generic;
using UnityEngine;

public class DashAfterImage : MonoBehaviour
{
    [Header("Spawn")]
    public float spawnInterval = 0.03f;     // how often to drop an afterimage
    public float lifeTime = 0.25f;          // how long each image lasts
    public float fadeSpeed = 8f;            // higher = faster fade

    [Header("Look")]
    public Material afterImageMaterial;     // MUST be a transparent material
    public bool useTintColor = true;
    public Color tint = new Color(1f, 1f, 1f, 0.6f);

    [Header("Refs")]
    public Transform visualsRoot;           // your model root (optional). If null, uses this transform.

    SkinnedMeshRenderer[] skinned;
    float timer;
    bool emitting;

    static readonly int ColorProp = Shader.PropertyToID("_Color");

    class Ghost
    {
        public GameObject go;
        public MeshRenderer mr;
        public float t;
    }

    readonly List<Ghost> ghosts = new();

    void Awake()
    {
        if (!visualsRoot) visualsRoot = transform;
        skinned = visualsRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
    }

    void Update()
    {
        if (emitting)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = spawnInterval;
                SpawnGhost();
            }
        }

        // Fade & cleanup
        for (int i = ghosts.Count - 1; i >= 0; i--)
        {
            Ghost g = ghosts[i];
            g.t += Time.deltaTime;

            float a = Mathf.Clamp01(1f - (g.t / lifeTime));
            a = Mathf.Pow(a, fadeSpeed * Time.deltaTime * 2f); // nicer curve

            if (g.mr && g.mr.material)
            {
                if (useTintColor && g.mr.material.HasProperty(ColorProp))
                {
                    Color c = g.mr.material.GetColor(ColorProp);
                    c.a = a * tint.a;
                    g.mr.material.SetColor(ColorProp, c);
                }
            }

            if (g.t >= lifeTime)
            {
                if (g.go) Destroy(g.go);
                ghosts.RemoveAt(i);
            }
        }
    }

    public void StartEmitting()
    {
        emitting = true;
        timer = 0f; // spawn immediately
    }

    public void StopEmitting()
    {
        emitting = false;
    }

    void SpawnGhost()
    {
        if (skinned == null || skinned.Length == 0) return;
        if (!afterImageMaterial) return;

        // Parent holder for this snapshot (keeps it aligned)
        GameObject holder = new GameObject("AfterImage");
        holder.transform.position = visualsRoot.position;
        holder.transform.rotation = visualsRoot.rotation;
        holder.transform.localScale = visualsRoot.lossyScale;

        // For each skinned mesh, bake and draw it once
        foreach (var smr in skinned)
        {
            if (!smr || !smr.enabled) continue;

            Mesh baked = new Mesh();
            smr.BakeMesh(baked);

            GameObject part = new GameObject("GhostPart");
            part.transform.SetParent(holder.transform, false);

            var mf = part.AddComponent<MeshFilter>();
            mf.sharedMesh = baked;

            var mr = part.AddComponent<MeshRenderer>();
            mr.sharedMaterial = afterImageMaterial;

            // Optional tint
            if (useTintColor && mr.material.HasProperty(ColorProp))
                mr.material.SetColor(ColorProp, tint);
        }

        Ghost ghost = new Ghost
        {
            go = holder,
            mr = holder.GetComponentInChildren<MeshRenderer>(),
            t = 0f
        };
        ghosts.Add(ghost);
    }
}
