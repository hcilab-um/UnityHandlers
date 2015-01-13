using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class CubeController : MonoBehaviour
{

  public GameObject UIController;

  private GameObject CubeRotation { get; set; }

  public CubeController()
  {
  }

  // Use this for initialization 
  void Start()
  {
    CubeRotation = GameObject.Find("CubeRotation");
  }

  // Update is called once per frame
  void Update()
  {
    ProcessOrientationUpdate();
  }


  void ProcessOrientationUpdate()
  {
    CubeRotation.transform.localRotation = RotationProvider.Instance.Rotation;
  }

  void TouchStarted(MoverioInputEventArgs args)
  {

  }

  void TouchMoved(MoverioInputEventArgs args)
  {

  }

  void TouchEnded(MoverioInputEventArgs args)
  {

  }
}
