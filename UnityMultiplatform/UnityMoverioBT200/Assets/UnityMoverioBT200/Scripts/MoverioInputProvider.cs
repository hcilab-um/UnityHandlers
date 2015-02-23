using UnityEngine;
using System.Collections;

/**
 * Make sure the execution time of this script is later than all others
 */
using System;
using System.Collections.Generic;

namespace UnityMoverioBT200.Scripts
{

  public class MoverioInputProvider : MonoBehaviour
  {

    private enum InputType { Touch, Mouse };

    private InputType TouchInputType { get; set; }

    public int TapTimeOut = 250;
    public int FingerUpTimeOut = 500;
		public bool TreatMovementAsTouch = true;

    /**
     * IMPORTANT
     * 
     * FOr some reason it needs the Synergy app to be running and connected :(
     **/
    private System.DateTime timeLastReset;

    [Header("ALERT: Experimental, needs ROOT, Synergy app running and connected :(")]
    [Tooltip("ALERT: Experimental, needs ROOT, Synergy app running and connected :(")]
    public bool resetMousePointerOnTouchEnded = false;

    private bool isAndroid = false;
    private bool isJNIInitialized = false;

    private static MoverioInputProvider instance;
    public static MoverioInputProvider Instance
    {
      get
      {
        if (instance == null)
          instance = new MoverioInputProvider();
        return instance;
      }
    }

    public MoverioInputProvider()
    {
      instance = this;
    }

    ~MoverioInputProvider()
    {
      Debug.Log("Destroying the MoverioInputProvider");

      if (isAndroid && resetMousePointerOnTouchEnded && isJNIInitialized)
      {
        CommToAndroid.CallAndroidMethod("closeMouseCursor");
      }
    }

    private List<GameObject> targets;

    // Use this for initialization
    void Start()
    {
      LoadScripts();

      //2- sets the right touch input type
      //The EPSON Moverio works with a mouse simulator, not with a touch interface
      TouchInputType = InputType.Mouse;
      if (SystemInfo.deviceModel.CompareTo("samsung SGH-I257M") == 0)
        TouchInputType = InputType.Touch;

      if (SystemInfo.deviceType == DeviceType.Handheld)
      {
        isAndroid = true;
        if (!isJNIInitialized && resetMousePointerOnTouchEnded)
          CommToAndroid.CallAndroidMethod("setPermissionsForInputDevice");
      }
    }

    public void LoadScripts()
    {
      //1- gets all the scripts from the scene
      targets = new List<GameObject>();
      targets.AddRange((GameObject[])FindObjectsOfType<GameObject>());
    }

    void CallMethodInAllScripts(string message, MoverioInputEventArgs args)
    {
      foreach (GameObject target in targets)
      {
        if (!target.activeInHierarchy)
          continue;

        target.SendMessage(message, args, SendMessageOptions.DontRequireReceiver);
      }
    }

    void OnGUI()
    {
      //GUILayout.BeginArea(new Rect(200, 0, 100, 25));
      //GUILayout.Label(touchLastPosition.ToString(), GUILayout.Width(100), GUILayout.Height(20));
      //GUILayout.EndArea();
    }

    // Update is called once per frame
    void Update()
    {
      if (Network.isClient)
        return;

      if (TouchInputType == InputType.Touch)
        ProcessInputForPhone();
      else if (TouchInputType == InputType.Mouse)//this includes the Moverio BT-200
      {
				if(TreatMovementAsTouch)
				{
					//The moverio has the problem that sometimes it doesn't register a tap as a mouse click, 
					// but rather as a 1px displacement. The TreatMovementAsTouch flag allows us to look at those 
					// cases (1px displacement) as well as the mouse events.
					if (!ProcessInputFromMouseEvents())
	          ProcessInputFromMovement();
				}
				else
				{
					ProcessInputFromMouseEvents();
				}
      }
    }

    void ProcessInputForPhone()
    {
      for (int i = 0; i < Input.touchCount; i++)
      {
        Touch touch = Input.GetTouch(i);

        //moves the finger, takes only movement in Y
        if (touch.phase == TouchPhase.Began)
        {
          timeTouchOrigin = System.DateTime.Now;
          touchOrigin = touch.position;
          mipTouchStarted(touchOrigin);
        }
        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
        {
          mipTouchMoved(touch.position, touchOrigin, touchLastPosition);
          touchLastPosition = touch.position;
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
          System.DateTime timeTouchEnded = System.DateTime.Now;
          mipTouchEnded(touch.position, touchOrigin, timeTouchEnded - timeTouchOrigin);

          if ((timeTouchEnded - timeTouchOrigin).TotalMilliseconds < TapTimeOut)
            mipTap(touchOrigin);
        }
      }
    }

    /** 
     * The moverio device works with a touch pad, meaning that it locates a cursor and then you trigger a selection.
     */
    bool isFingerDown = false;
    System.DateTime timeTouchOrigin, timeTouchLastMove;
    Vector3 touchOrigin, touchLastPosition;

    bool ProcessInputFromMouseEvents()
    {
      bool eventFound = false;
      if (Input.GetMouseButtonDown(0) && !isFingerDown)
      {
        eventFound = true;
        isFingerDown = true;
        timeTouchOrigin = System.DateTime.Now;
        touchOrigin = Input.mousePosition;
        mipTouchStarted(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
      }

      if (Input.GetMouseButton(0))
      {
        eventFound = true;
        Vector3 position = Input.mousePosition;
        mipTouchMoved(new Vector2(position.x, position.y), touchOrigin, touchLastPosition);
        touchLastPosition = position;
      }

      if (Input.GetMouseButtonUp(0))
      {
        eventFound = true;
        isFingerDown = false;
        System.DateTime timeTouchEnded = System.DateTime.Now;
        mipTouchEnded(new Vector2(Input.mousePosition.x, Input.mousePosition.y), touchOrigin, timeTouchEnded - timeTouchOrigin);

        if ((timeTouchEnded - timeTouchOrigin).TotalMilliseconds < TapTimeOut)
          mipTap(touchOrigin);
      }

      return eventFound;
    }

    void ProcessInputFromMovement()
    {
      bool fingerMoved = !(touchLastPosition == Input.mousePosition);
      if (isFingerDown)
      {
        if (fingerMoved)
        {
          Vector3 position = Input.mousePosition;
          mipTouchMoved(new Vector2(position.x, position.y), touchOrigin, touchLastPosition);
          timeTouchLastMove = System.DateTime.Now;
          touchLastPosition = Input.mousePosition;

          return;
        }
        else
        {
          if ((System.DateTime.Now - timeTouchLastMove).TotalMilliseconds < FingerUpTimeOut)
            return;

          mipTouchEnded(Input.mousePosition, touchOrigin, System.DateTime.Now - timeTouchOrigin);
          timeTouchLastMove = System.DateTime.Now;
          touchLastPosition = Input.mousePosition;
          isFingerDown = false;

          if ((System.DateTime.Now - timeTouchOrigin).TotalMilliseconds < TapTimeOut)
            mipTap(touchOrigin);

          return;
        }
      }
      else if (fingerMoved)
      {
        isFingerDown = true;
        mipTouchStarted(Input.mousePosition);

        touchOrigin = Input.mousePosition;
        timeTouchOrigin = System.DateTime.Now;

        touchLastPosition = touchOrigin;
        timeTouchLastMove = timeTouchOrigin;
      }
    }

    void mipTouchStarted(Vector2 position)
    {
      if ((System.DateTime.Now - timeLastReset).TotalMilliseconds < FingerUpTimeOut)
        return;

      MoverioInputEventArgs args = new MoverioInputEventArgs(MoverioInputEventType.TouchStarted);
      args.Position = position;
      args.Origin = position;
      args.Last = position;

      CallMethodInAllScripts("OnTouchStarted", args.Prepare());
    }

    void mipTouchMoved(Vector2 position, Vector2 origin, Vector2 last)
    {
      MoverioInputEventArgs args = new MoverioInputEventArgs(MoverioInputEventType.TouchMoved);
      args.Position = position;
      args.Origin = origin;
      args.Last = last;

      CallMethodInAllScripts("OnTouchMoved", args.Prepare());
    }

    void mipTouchEnded(Vector2 position, Vector2 origin, System.TimeSpan lenght)
    {
      if ((System.DateTime.Now - timeLastReset).TotalMilliseconds < FingerUpTimeOut)
        return;

      MoverioInputEventArgs args = new MoverioInputEventArgs(MoverioInputEventType.TouchEnded);
      args.Position = position;
      args.Origin = origin;
      args.Duration = lenght;

      CallMethodInAllScripts("OnTouchEnded", args.Prepare());

      if (isAndroid && resetMousePointerOnTouchEnded)
      {
        if (!isJNIInitialized)
        {
          CommToAndroid.CallAndroidMethod("setDisplaySize", Screen.width, Screen.height);
          CommToAndroid.CallAndroidMethod("openMouseCursor", "UnityMoverioBT200");

          isJNIInitialized = true;
        }
        else
        {
          CommToAndroid.CallAndroidMethod("mouseMove", -1, -1);
          CommToAndroid.CallAndroidMethod("mouseMove", 180, 100);
        }

        touchOrigin = Input.mousePosition;
        touchLastPosition = Input.mousePosition;
        timeLastReset = System.DateTime.Now;
      }
    }

    private System.DateTime lastTapTime = System.DateTime.MinValue;

    void mipTap(Vector2 position)
    {
      if ((System.DateTime.Now - timeLastReset).TotalMilliseconds < FingerUpTimeOut)
        return;

      MoverioInputEventArgs args = new MoverioInputEventArgs(MoverioInputEventType.Tap);
      args.Position = position;
      args.Origin = position;
      args.Last = position;
      CallMethodInAllScripts("OnTap", args.Prepare());

      System.DateTime tapTime = System.DateTime.Now;
      if ((tapTime - lastTapTime).TotalMilliseconds < TapTimeOut)
      {
        args = new MoverioInputEventArgs(MoverioInputEventType.DoubleTap);
        args.Position = position;
        args.Origin = position;
        args.Last = position;
        CallMethodInAllScripts("OnDoubleTap", args.Prepare());
      }
      lastTapTime = tapTime;
    }
  }

}