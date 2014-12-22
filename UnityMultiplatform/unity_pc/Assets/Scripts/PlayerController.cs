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
		count = 0;
		SetCountText ();
		winText.text = "";
	}

	void FixedUpdate ()
	{
		float horizontalMove = Input.GetAxis ("Horizontal");
		float verticalMove = Input.GetAxis ("Vertical");
		Vector3 movement = new Vector3 (horizontalMove * playerSpeed, 0.0f, verticalMove* playerSpeed);
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