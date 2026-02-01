using System.Collections.Generic;
using UnityEngine;

public class MaskInventory : MonoBehaviour
{
    [Header("References")]
    public GameObject maskPrefab;
    public Transform orbitCenter;

    [Header("Orbit Levels")]
    public float baseRadius = 1.2f;
    public float radiusStep = 0.5f;
    public float baseHeight = 1.0f;
    public float heightStep = 0.35f;
    public float orbitSpeed = 120f;
    public int masksPerLevel = 3;

    [Header("Limit")]
    public int maxMasks = 9;
    public bool replaceOldestWhenFull = false;

    readonly List<OrbitingMask> masks = new();
    int spawnIndex;

    int lastPreviewCount = -1;

    public int Count
    {
        get { Cleanup(); return masks.Count; }
    }

    public bool HasMasks => Count > 0;

    void Awake()
    {
        if (orbitCenter == null) orbitCenter = transform;
    }

    public void SpawnMany(int count)
    {
        for (int i = 0; i < count; i++)
            SpawnOne();
    }

    public void SpawnOne()
    {
        if (maskPrefab == null) return;

        Cleanup();

        if (masks.Count >= maxMasks)
        {
            if (!replaceOldestWhenFull) return;

            var oldest = masks[0];
            masks.RemoveAt(0);
            if (oldest) Destroy(oldest.gameObject);
        }

        var obj = Instantiate(maskPrefab);
        var mask = obj.GetComponent<OrbitingMask>() ?? obj.AddComponent<OrbitingMask>();

        int level = spawnIndex / Mathf.Max(1, masksPerLevel);

        mask.center = orbitCenter;
        mask.radius = baseRadius + radiusStep * level;
        mask.heightOffset = baseHeight + heightStep * level;
        mask.speed = orbitSpeed;

        masks.Add(mask);
        spawnIndex++;

        RecalculateAnglesPerLevel();
        lastPreviewCount = -1; // invalidate preview cache
    }

    public void Lose(int amount)
    {
        Cleanup();
        if (amount <= 0) return;

        for (int i = 0; i < amount; i++)
        {
            if (masks.Count == 0) break;

            int idx = masks.Count - 1;
            var m = masks[idx];
            masks.RemoveAt(idx);
            if (m) Destroy(m.gameObject);
        }

        RecalculateAnglesPerLevel();
        lastPreviewCount = -1;
    }

    public OrbitingMask PopLast()
    {
        Cleanup();
        if (masks.Count == 0) return null;

        int idx = masks.Count - 1;
        var m = masks[idx];
        masks.RemoveAt(idx);

        RecalculateAnglesPerLevel();
        lastPreviewCount = -1;

        return m;
    }

    public List<OrbitingMask> PopAll()
    {
        Cleanup();
        var all = new List<OrbitingMask>(masks);
        masks.Clear();

        lastPreviewCount = -1;
        return all;
    }

    public Transform GetCastFrom(Transform castPoint)
    {
        return castPoint != null ? castPoint : orbitCenter;
    }

    // ---- NEW: preview feedback ----
    public void PreviewThrowCount(int count)
    {
        Cleanup();

        count = Mathf.Clamp(count, 0, masks.Count);
        if (count == lastPreviewCount) return;

        // Clear all first
        for (int i = 0; i < masks.Count; i++)
            if (masks[i] != null) masks[i].SetSelectedForThrow(false);

        // Select last N (matches PopLast order)
        for (int i = masks.Count - count; i < masks.Count; i++)
            if (i >= 0 && masks[i] != null) masks[i].SetSelectedForThrow(true);

        lastPreviewCount = count;
    }

    public void ClearPreview()
    {
        PreviewThrowCount(0);
    }

    void Cleanup()
    {
        masks.RemoveAll(m => m == null);
    }

    void RecalculateAnglesPerLevel()
    {
        var levelMap = new Dictionary<int, List<OrbitingMask>>();

        foreach (var mask in masks)
        {
            if (mask == null) continue;
            int level = Mathf.RoundToInt((mask.radius - baseRadius) / Mathf.Max(0.0001f, radiusStep));
            if (!levelMap.ContainsKey(level)) levelMap[level] = new List<OrbitingMask>();
            levelMap[level].Add(mask);
        }

        foreach (var kvp in levelMap)
        {
            float step = 360f / kvp.Value.Count;
            for (int i = 0; i < kvp.Value.Count; i++)
                kvp.Value[i].baseAngle = step * i;
        }
    }
}
