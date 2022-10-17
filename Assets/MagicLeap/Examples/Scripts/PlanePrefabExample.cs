using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlanePrefabExample : MonoBehaviour
{
    void Start()
    {
        ColorClassify();
    }

    private void ColorClassify()
    {
        var plane = GetComponent<ARPlane>();
        Color color = Color.gray;
        switch (plane.classification)
        {
            case PlaneClassification.Floor:
                color = Color.green;
                break;
            case PlaneClassification.Ceiling:
                color = Color.blue;
                break;
            case PlaneClassification.Wall:
                color = Color.red;
                break;
        }

        GetComponent<MeshRenderer>().material.color = color;
    }
}
