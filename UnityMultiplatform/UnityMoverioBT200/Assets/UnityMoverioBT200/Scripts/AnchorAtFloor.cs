using UnityEngine;
using System.Collections;

namespace UnityMoverioBT200.Scripts
{

	public class AnchorAtFloor : MonoBehaviour {

		// Use this for initialization
		void Start () {
		
		}
		
		// Update is called once per frame
		void Update () 
		{
			//cancels out any height values in the VirtualBody and Camera, effectively sticking
			// the floorchart to the floor (Y == 0)
			Vector3 pos = transform.localPosition;
			pos.y = -transform.parent.position.y * 1 / transform.parent.localScale.y;
			transform.localPosition = pos;
		}
	}

}
