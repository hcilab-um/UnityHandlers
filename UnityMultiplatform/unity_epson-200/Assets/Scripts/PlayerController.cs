using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour 
{
	public float playerSpeed = 0.0f;

	private int count = 0;

	void Start ()
	{
		Screen.orientation = ScreenOrientation.LandscapeLeft;
		count = 0;
		SetCountText ();
	}

	void FixedUpdate ()
	{
		if (Input.touchCount > 0) 
		{
			Vector2 touchDeltaPosition = Input.GetTouch (0).deltaPosition;
			Vector3 movement = new Vector3 (touchDeltaPosition.x * playerSpeed, 
			                                0.0f, 
			                                touchDeltaPosition.y * playerSpeed);
			print(string.Format("Movement: {0}", movement));
			rigidbody.AddForce (movement);
		}
	}

	void OnTriggerEnter (Collider other)
	{
		Destroy (other.gameObject);
		if (other.gameObject.tag == "Pickup") 
		{
			other.gameObject.SetActive (false);
			count++;
		}
	}

	void SetCountText ()
	{
	}
}