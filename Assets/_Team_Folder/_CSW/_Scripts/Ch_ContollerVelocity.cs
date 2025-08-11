using UnityEngine;
using UnityEngine.InputSystem;

public class Ch_ContollerVelocity : MonoBehaviour
{
    public InputActionProperty velocityProperty;
    
    public Vector3 velocity;
    
    public float magnitude;

    private void FixedUpdate()
    {
        velocity=velocityProperty.action.ReadValue<Vector3>();
        magnitude=velocity.magnitude;
    }
}
