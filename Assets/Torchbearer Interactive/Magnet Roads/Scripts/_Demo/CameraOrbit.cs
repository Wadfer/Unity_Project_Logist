using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    public Transform orbitTarget;

	protected void Update()
    {
        transform.LookAt(orbitTarget);
        transform.Translate(Vector3.right / 2 * Time.deltaTime);
    }
}
