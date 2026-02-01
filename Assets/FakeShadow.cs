using UnityEngine;

public class AirborneShadow : MonoBehaviour
{
    [Header("Refs")]
    public GameObject shadowPrefab;
    public Transform shadowInstance;

    [Header("Ground Check")]
    public LayerMask groundMask = ~0;
    public float raycastHeight = 2f;
    public float maxDistance = 20f;

    [Header("Scaling")]
    public float minScale = 0.4f;   // highest point
    public float maxScale = 1.2f;   // near ground
    public float scaleHeightRange = 6f;

    [Header("Offset")]
    public float surfaceOffset = 0.02f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (!shadowPrefab)
        {
            Debug.LogError("AirborneShadow: No shadow prefab assigned.");
            enabled = false;
            return;
        }

        // Spawn shadow
        var obj = Instantiate(shadowPrefab);
        shadowInstance = obj.transform;
        shadowInstance.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        bool airborne = rb && Mathf.Abs(rb.velocity.y) > 0.1f;

        if (!airborne)
        {
            if (shadowInstance.gameObject.activeSelf)
                shadowInstance.gameObject.SetActive(false);
            return;
        }

        // Cast downward ray
        Vector3 origin = transform.position + Vector3.up * raycastHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (!shadowInstance.gameObject.activeSelf)
                shadowInstance.gameObject.SetActive(true);

            // Position shadow
            shadowInstance.position = hit.point + hit.normal * surfaceOffset;

            // Align to ground
            //shadowInstance.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            // Scale based on height
            float height = transform.position.y - hit.point.y;
            float t = Mathf.Clamp01(height / scaleHeightRange);
            float scale = Mathf.Lerp(maxScale, minScale, t);
            shadowInstance.localScale = new Vector3(scale, scale, scale);
        }
        else
        {
            shadowInstance.gameObject.SetActive(false);
        }
    }
}
