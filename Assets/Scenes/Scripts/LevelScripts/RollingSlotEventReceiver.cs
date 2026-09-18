using UnityEngine;

public class RollingSlotEventReceiver : MonoBehaviour
{
    public Level1Manager level1Manager;

    public void SlotRollFinished()
    {
        Debug.Log("✅ Animation Event Fired");

        if (level1Manager != null)
        {
            level1Manager.SlotRollFinished();
        }
        else
        {
            Debug.LogError("Level1Manager reference is NULL.");
        }
    }
}