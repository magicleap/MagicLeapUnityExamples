using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeVisualizer : MonoBehaviour
{
    [SerializeField]
    private Transform trackingPoint;

    [SerializeField]
    private float speed;

    [SerializeField]
    private GameObject visual;

    [SerializeField]
    private float trailThreshold = .1f;

    private Vector3 targetPosition;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        var target = trackingPoint.position;
        if ((target - transform.position).magnitude > trailThreshold)
            targetPosition = target;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * speed);
    }

    public void Show(Vector3 position)
    {
        visual.SetActive(true);
        visual.transform.position = position;
        visual.transform.LookAt(mainCamera.transform);
    }

    public void Hide()
    {
        visual.SetActive(false);
    }
}
