using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureVisualizer : MonoBehaviour
{
    public Renderer SphereObject;
    public Color HandColor;

    void Start()
    {
        SphereObject.material.color = HandColor;
    }
}
