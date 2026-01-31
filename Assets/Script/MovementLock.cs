using UnityEngine;

public class PlayerMovementLock : MonoBehaviour
{
    public static PlayerMovementLock Instance;

    public bool locked;

    void Awake()
    {
        Instance = this;
    }

    public void LockMovement()
    {
        locked = true;
    }

    public void UnlockMovement()
    {
        locked = false;
    }
}
