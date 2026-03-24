using System.Collections;
using System.Collections.Generic;
using MagnetRoads;
using UnityEngine;
using UnityEngine.UI;

public class DemoLogic : MonoBehaviour
{
    public List<Camera> cameras;
    public List<string> cameraText;
    public Text textToEdit;
    public Button nextButton;

    private int camIndex = 0;

    protected void Start()
    {
        var button = nextButton.GetComponent<Button>();
        button.onClick.AddListener(MoveToNextCamera);
        UpdateCameras();
    }

    private void MoveToNextCamera()
    {
        if (camIndex < cameras.Count - 1) camIndex++;
        else camIndex = 0;
        UpdateCameras();
    }

    private void UpdateCameras()
    {
        for (var i = 0; i < cameras.Count; i++)
        {
            cameras[camIndex].enabled = true;
            textToEdit.text = cameraText[camIndex];
            if (camIndex != i && cameras[i].enabled)
            {
                cameras[i].enabled = false;
            }
        }
    }
}
