using UnityEngine;

public class RotateAround : MonoBehaviour
{
    public Vector3 RotationMask = new Vector3(0, 1, 0); //which axes to rotate around
    public float RotationSpeed = 5.0f; //degrees per second
    public Transform RotateAroundObject;

    void Update()
    {
        if (RotateAroundObject != null)
        {
            transform.RotateAround(RotateAroundObject.transform.position,
                RotationMask, RotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.Rotate(new Vector3(
                RotationMask.x * RotationSpeed * Time.deltaTime,
                RotationMask.y * RotationSpeed * Time.deltaTime,
                RotationMask.z * RotationSpeed * Time.deltaTime));
        }
    }
}
