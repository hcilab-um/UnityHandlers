using UnityEngine;
using System.Collections;

public class CommToAndroid : MonoBehaviour
{
  public enum DisplayMode { Display2D, Display3D };

  public DisplayMode DisplayMode2D3D = DisplayMode.Display2D;

  private static CommToAndroid instance = null;
  public static CommToAndroid Instace
  {
    get
    {
      if (instance == null)
        instance = new CommToAndroid();
      return instance;
    }
  }

  public CommToAndroid()
  {
    instance = this;
  }

  // Use this for initialization
  void Start()
  {
    try
    {
      DisplayMode currentMode = (DisplayMode)System.Enum.Parse(typeof(DisplayMode), CallAndroidMethod<string>("GetDisplayMode"));
      if (currentMode != DisplayMode2D3D)
        CallAndroidMethod("SetDisplayMode", DisplayMode2D3D.ToString());
    }
    catch (System.Exception ex)
    {
      Debug.LogException(ex);
    }
  }

  // Update is called once per frame
  void Update()
  {

  }

  public static void CallAndroidMethod(string methodName)
  {
	CallAndroidMethod(methodName, null);
  }

  public static void CallAndroidMethod(string methodName, string parameters)
  {
    try
    {
      using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
      {
        using (AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity"))
        {
          jo.Call(methodName, parameters);
        }
      }
    }
    catch (System.Exception ex)
    {
      Debug.LogException(ex);
    }
  }

  public static ReturnType CallAndroidMethod<ReturnType>(string methodName)
  {
	return CallAndroidMethod<ReturnType>(methodName, null);
  }

  public static ReturnType CallAndroidMethod<ReturnType>(string methodName, string parameters)
  {
    try
    {
      using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
      {
        using (AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity"))
        {
          return jo.Call<ReturnType>(methodName, parameters);
        }
      }
    }
    catch (System.Exception ex)
    {
      Debug.LogException(ex);
    }
    return default(ReturnType);
  }

}
