using UnityEngine;

public class TopDownThirdPersonCamera : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 15f, -8f);
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private bool rotateWithTarget = false;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDamping = 12f;

    float shakeTimeRemaining;
    float shakeMagnitude;
    Vector3 shakeOffset;

    private void LateUpdate()
    {
        if (!target) return;

        // stable smoothing factor
        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

        // --- BASE CAMERA POSITION ---
        Vector3 desiredPos;
        Quaternion desiredRot = transform.rotation;

        if (rotateWithTarget)
        {
            desiredPos = target.position + target.rotation * offset;
            desiredRot = Quaternion.LookRotation(target.position - desiredPos, Vector3.up);

            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, t);
        }
        else
        {
            desiredPos = target.position + offset;
        }

        // --- CAMERA SHAKE ---
        if (shakeTimeRemaining > 0f)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeTimeRemaining -= Time.deltaTime * shakeDamping;
        }
        else
        {
            shakeTimeRemaining = 0f;
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * shakeDamping);
        }

        // --- APPLY POSITION ---
        Vector3 finalPos = desiredPos + shakeOffset;
        transform.position = Vector3.Lerp(transform.position, finalPos, t);

        if (!rotateWithTarget)
        {
            // use finalPos (intended position), not transform.position (lagging/feedback)
            transform.rotation = Quaternion.LookRotation(target.position - finalPos, Vector3.up);
        }
    }

    public void Shake(float duration, float magnitude)
    {
        shakeTimeRemaining = duration;
        shakeMagnitude = magnitude;
    }
}
