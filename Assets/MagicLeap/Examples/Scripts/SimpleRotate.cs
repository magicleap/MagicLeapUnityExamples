using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleRotate : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [SerializeField]
    private Axis rotateAxis;

    [SerializeField]
    private float rate = 1.0f;

    // Update is called once per frame
    void Update()
    {
        Vector3 axis = Vector3.zero;
        switch (rotateAxis)
        {
            case Axis.X:
                axis = Vector3.right;
                break;
            case Axis.Y:
                axis = Vector3.up;
                break;
            case Axis.Z:
                axis = Vector3.forward;
                break;
        }

        transform.RotateAround(transform.position, axis, rate);
    }
}
