using System.Collections;
using UnityEngine;

public class MaskThrower : MonoBehaviour
{
    [Header("Refs")]
    public MaskInventory inventory;
    public Animator animator;

    [Header("Cast")]
    public Transform castPoint;

    [Header("Throw Timing")]
    public float throwDelay = 1f;
    public bool blockWhileThrowing = true;

    [Header("Rotation")]
    public Transform rotateRoot;
    public bool snapTurnOnThrow = true;
    public float turnSpeed = 1080f;
    public bool rotateDuringWindup = true;
    public bool lockAimDuringThrow = true;

    [Header("Shotgun")]
    public bool shotgunCone = true;
    public float coneAngle = 30f;
    public bool randomSpread = false;
    public float throwInterval = 0.02f;

    bool isThrowing;
    Coroutine throwRoutine;

    Vector3 queuedDirection;
    int queuedAmount;
    System.Func<Vector3> liveAimProvider;

    void Awake()
    {
        if (rotateRoot == null) rotateRoot = transform;
    }

    public bool CanStartThrow()
    {
        if (inventory == null) return false;
        if (!inventory.HasMasks) return false;
        if (blockWhileThrowing && isThrowing) return false;
        return true;
    }

    public void RequestThrow(Vector3 aimDirection, int amountToThrow, System.Func<Vector3> liveAim = null)
    {
        if (!CanStartThrow()) return;

        queuedDirection = aimDirection;
        queuedAmount = Mathf.Clamp(amountToThrow, 1, inventory.Count);
        liveAimProvider = liveAim;

        // rotate immediately at release time
        TurnTowards(queuedDirection, snapTurnOnThrow);

        if (throwRoutine != null) StopCoroutine(throwRoutine);
        throwRoutine = StartCoroutine(ThrowAfterDelay());
    }

    IEnumerator ThrowAfterDelay()
    {
        isThrowing = true;

        if (animator) animator.SetTrigger("Attack");

        float t = 0f;
        while (t < throwDelay)
        {
            t += Time.deltaTime;

            if (!lockAimDuringThrow && liveAimProvider != null)
                queuedDirection = liveAimProvider();

            if (rotateDuringWindup && !snapTurnOnThrow)
                TurnTowards(queuedDirection, snap: false);

            yield return null;
        }

        yield return ThrowSome(queuedDirection, queuedAmount);

        isThrowing = false;
        throwRoutine = null;
    }

    IEnumerator ThrowSome(Vector3 direction, int amount)
    {
        if (!inventory.HasMasks) yield break;

        amount = Mathf.Clamp(amount, 1, inventory.Count);

        if (amount <= 1)
        {
            ThrowOne(direction);
            yield break;
        }

        if (shotgunCone)
            yield return ThrowSomeShotgun(direction, amount);
        else
            yield return ThrowSomeSameDirection(direction, amount);
    }

    void ThrowOne(Vector3 direction)
    {
        Transform from = inventory.GetCastFrom(castPoint);

        OrbitingMask mask = inventory.PopLast();
        if (mask != null)
            mask.ThrowFrom(from, Flatten(direction));
    }

    IEnumerator ThrowSomeSameDirection(Vector3 direction, int amount)
    {
        Transform from = inventory.GetCastFrom(castPoint);
        Vector3 dir = Flatten(direction);

        for (int i = 0; i < amount; i++)
        {
            OrbitingMask mask = inventory.PopLast();
            if (mask != null)
                mask.ThrowFrom(from, dir);

            if (throwInterval > 0f)
                yield return new WaitForSeconds(throwInterval);
        }
    }

    IEnumerator ThrowSomeShotgun(Vector3 direction, int amount)
    {
        Transform from = inventory.GetCastFrom(castPoint);

        Vector3 baseDir = Flatten(direction);
        float half = coneAngle * 0.5f;

        for (int i = 0; i < amount; i++)
        {
            float angle;
            if (randomSpread) angle = Random.Range(-half, half);
            else
            {
                float tt = (amount == 1) ? 0.5f : (float)i / (amount - 1);
                angle = Mathf.Lerp(-half, half, tt);
            }

            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;

            OrbitingMask mask = inventory.PopLast();
            if (mask != null)
                mask.ThrowFrom(from, dir);

            if (throwInterval > 0f)
                yield return new WaitForSeconds(throwInterval);
        }
    }

    Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        if (v.sqrMagnitude < 0.0001f) return transform.forward;
        return v.normalized;
    }

    void TurnTowards(Vector3 direction, bool snap)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (snap) rotateRoot.rotation = targetRot;
        else
        {
            float step = turnSpeed * Time.deltaTime;
            rotateRoot.rotation = Quaternion.RotateTowards(rotateRoot.rotation, targetRot, step);
        }
    }
}
