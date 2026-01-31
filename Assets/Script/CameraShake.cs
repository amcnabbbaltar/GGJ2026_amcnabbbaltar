using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 0.12f;
    public float damping = 10f;

    Vector3 originalLocalPos;
    float shakeTimeRemaining;

    void Awake()
    {
        originalLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (shakeTimeRemaining > 0f)
        {
            Vector3 offset = Random.insideUnitSphere * shakeMagnitude;
            transform.localPosition = originalLocalPos + offset;

            shakeTimeRemaining -= Time.deltaTime * damping;
        }
        else
        {
            shakeTimeRemaining = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalLocalPos,
                Time.deltaTime * damping
            );
        }
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
        shakeTimeRemaining = duration;
    }
}
