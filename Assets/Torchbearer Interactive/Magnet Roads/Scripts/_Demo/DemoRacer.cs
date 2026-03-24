using System.Collections;
using System.Collections.Generic;
using MagnetRoads;
using UnityEngine;

public class DemoRacer : MonoBehaviour
{
    public MagnetRoad roadToFollow;
    public int laneToFollow;
    public float speed;

    private int targetPoint;
    private Vector3[] path;

    protected void Start()
    {
        path = roadToFollow.GetLaneWaypoints(laneToFollow);
        transform.position = path[path.Length - 1];
        targetPoint = 0;
    }

	protected void Update()
    {
	    transform.LookAt(path[targetPoint]);
        if (Vector3.Distance(path[targetPoint], transform.position) < 0.25f)
        {
            targetPoint += 2;
            if (targetPoint > path.Length - 1) targetPoint = 0;
        }
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
	}
}
