using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour 
{
	public float playerSpeed = 0.0f;
	public GUIText countText;
	public GUIText winText;

	private int count = 0;

	void Start ()
	{
		Screen.orientation = ScreenOrientation.LandscapeLeft;
		Screen.fullScreen = false;
		count = 0;
		SetCountText ();
		winText.text = "";
	}

	void FixedUpdate ()
	{
		float moveHorizontal = Input.GetAxis("Horizontal");
		float moveVertical = Input.GetAxis("Vertical");
		Vector3 movement = new Vector3 (moveHorizontal * playerSpeed, 
		                                0.0f, moveVertical * playerSpeed);
		print(string.Format("Movement: {0}", movement));
		rigidbody.AddForce (movement);
	}

	void OnTriggerEnter (Collider other)
	{
		Destroy (other.gameObject);
		if (other.gameObject.tag == "Pickup") 
		{
			other.gameObject.SetActive (false);
			count++;
			SetCountText ();
		}
	}

	void SetCountText ()
	{
		countText.text = "Count: " + count.ToString ();
		if (count >= 8) 
		{
			winText.text = "You Win!";
		}
	}
}