using UnityEngine;

public class RotatableSwitch : MonoBehaviour
{
    public int lightIndex;
    public LightSequenceManager sequenceManager;

    public void OnFullyRotated()
    {
        sequenceManager.ActivateLight(lightIndex);
    }
}

