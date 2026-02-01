using UnityEngine;

public class MaskAimUI : MonoBehaviour
{
    [Header("References")]
    public Transform orbitCenter;
    public Camera aimCamera;

    [Header("Aim Line")]
    public LineRenderer aimLine;
    public float lineStartHeight = 1.0f;
    public float lineEndHeightOffset = 0.02f;
    public LayerMask aimLayerMask = ~0;
    public float maxAimDistance = 50f;
    public bool showLineWhenNoMasks = false;

    [Header("Optional Reticle")]
    public GameObject aimIndicatorPrefab;
    GameObject aimIndicatorInstance;

    void Awake()
    {
        if (orbitCenter == null) orbitCenter = transform;
        if (aimCamera == null) aimCamera = Camera.main;

        if (aimIndicatorPrefab != null)
            aimIndicatorInstance = Instantiate(aimIndicatorPrefab);

        if (aimLine != null)
        {
            aimLine.positionCount = 2;
            aimLine.enabled = false;
        }
    }

    public Vector3 UpdateAim(bool hasMasks)
    {
        bool shouldShow = showLineWhenNoMasks || hasMasks;
        Vector3 start = orbitCenter.position + Vector3.up * lineStartHeight;

        Vector3 aimDirection = transform.forward;

        if (aimCamera == null)
        {
            SetLine(shouldShow, start, start + aimDirection * maxAimDistance);
            SetReticle(false, Vector3.zero);
            return aimDirection;
        }

        Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 end;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayerMask))
        {
            end = hit.point + Vector3.up * lineEndHeightOffset;

            Vector3 dir = (hit.point - orbitCenter.position);
            dir.y = 0f;
            aimDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;

            SetReticle(true, end);
        }
        else
        {
            Vector3 forward = aimCamera.transform.forward;
            forward.y = 0f;
            aimDirection = forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;

            end = start + aimDirection * maxAimDistance;
            SetReticle(false, Vector3.zero);
        }

        SetLine(shouldShow, start, end);
        return aimDirection;
    }

    void SetLine(bool enabled, Vector3 start, Vector3 end)
    {
        if (aimLine == null) return;

        aimLine.enabled = enabled;
        if (!enabled) return;

        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    void SetReticle(bool enabled, Vector3 position)
    {
        if (aimIndicatorInstance == null) return;

        aimIndicatorInstance.SetActive(enabled);
        if (enabled) aimIndicatorInstance.transform.position = position;
    }
}
