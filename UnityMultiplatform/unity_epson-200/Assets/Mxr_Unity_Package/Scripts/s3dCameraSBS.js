/* s3dCameraSBS.js - revised 2/12/13
 * Please direct any bugs/comments/suggestions to hoberman@usc.edu.
 * FOV2GO for Unity Copyright (c) 2011-13 Perry Hoberman & MxR Lab. All rights reserved.
 * Usage: Attach to camera. Creates, manages and renders side-by-side stereoscopic view.
 * NOTE: interaxial is measured in millimeters; zeroPrlxDist is measured in meters 
 * Has companion Editor script (s3dCameraSBSEditor.js) for custom inspector 
 */

#pragma strict
// Cameras
public var leftCam : GameObject; // left view camera
public var rightCam : GameObject; // right view camera
public var maskCam : GameObject; // black mask for mobile formats
// Stereo Parameters
public var interaxial : float = 65; // Distance (in millimeters) between cameras
public var zeroPrlxDist : float = 3.0; // Distance (in meters) at which left and right images overlap exactly
// View Parameters
enum cams_3D {LeftRight, LeftOnly, RightOnly, RightLeft}
public var cameraSelect = cams_3D.LeftRight; // View order - swap cameras for cross-eyed free-viewing
public var H_I_T: float = 0; // Horizontal Image Transform - shift left and right image horizontally
public var offAxisFrustum : float = 0; // Esoteric parameter
// Side by Side Parameters
public var sideBySideSqueezed : boolean = false; // 50% horizontal scale for 3DTVs
public var usePhoneMask : boolean = true; // Mask for mobile formats
public var leftViewRect : Vector4 = Vector4(0,0,0.5,1); // left, bottom, width, height
public var rightViewRect : Vector4 = Vector4(0.5,0,1,1);
// Initialization
private var initialized : boolean = false;

@script AddComponentMenu ("Stereoskopix/s3d Camera S3D")
@script ExecuteInEditMode()

function Awake () {
	initStereoCamera();
}

function initStereoCamera () {
	SetupCameras();
	SetStereoFormat();
}

function SetupCameras() {
	var lcam = transform.Find("leftCam"); // check if we've already created a leftCam
	if (lcam) {
		leftCam = lcam.gameObject;
		leftCam.camera.CopyFrom (camera);
	} else {
		leftCam = new GameObject ("leftCam", Camera);
		leftCam.AddComponent(GUILayer);
		leftCam.camera.CopyFrom (camera);
		leftCam.transform.parent = transform;
	}

	var rcam = transform.Find("rightCam"); // check if we've already created a rightCam
	if (rcam) {
		rightCam = rcam.gameObject;
		rightCam.camera.CopyFrom (camera);
	} else {
		rightCam = new GameObject("rightCam", Camera);
		rightCam.AddComponent(GUILayer);
		rightCam.camera.CopyFrom (camera);
		rightCam.transform.parent = transform;
	}
	
	var mcam = transform.Find("maskCam"); // check if we've already created a maskCam
	if (mcam) {
		maskCam = mcam.gameObject;
	} else {
		maskCam = new GameObject("maskCam", Camera);
		maskCam.AddComponent(GUILayer);
		maskCam.camera.CopyFrom (camera);
		maskCam.transform.parent = transform;
	}
		
	camera.depth = -2; // rendering order (back to front): Main Camera/maskCam/leftCam/rightCam
	maskCam.camera.depth = camera.depth + 1;
	leftCam.camera.depth = camera.depth  + 2;
	rightCam.camera.depth = camera.depth + 3;
	
	maskCam.camera.cullingMask = 0; // nothing shows in mask layer
	maskCam.camera.clearFlags = CameraClearFlags.SolidColor;
	maskCam.camera.backgroundColor = Color.black;	
}	

function SetStereoFormat() {
	if (!usePhoneMask) {
		leftCam.camera.rect = Rect(0,0,0.5,1);
		rightCam.camera.rect = Rect(0.5,0,0.5,1);
	} else {
		leftCam.camera.rect = Vector4toRect(leftViewRect);
		rightCam.camera.rect = Vector4toRect(rightViewRect);
	}
	leftViewRect = RectToVector4(leftCam.camera.rect);
	rightViewRect = RectToVector4(rightCam.camera.rect);
	fixCameraAspect();
	maskCam.camera.enabled = usePhoneMask;
}

function fixCameraAspect() {
	camera.ResetAspect();
	camera.aspect *= leftCam.camera.rect.width*2/leftCam.camera.rect.height;
	leftCam.camera.aspect = camera.aspect;
	rightCam.camera.aspect = camera.aspect;
}

function Vector4toRect(v : Vector4) {
	var r : Rect =  Rect(v.x,v.y,v.z,v.w);
	return r;
}
	
function RectToVector4(r : Rect) {
	var v : Vector4 = Vector4(r.x,r.y,r.width,r.height);
	return v;
}
	
function Update() {
	#if UNITY_EDITOR
	if (EditorApplication.isPlaying) {
		camera.enabled = false;
	} else {
		camera.enabled = true; // need camera enabled when in edit mode
	}
	#endif
	if (Application.isPlaying) {
		if (!initialized) {
			initialized = true;
		}
	} else {
		initialized = false;
		SetStereoFormat();
	}
	UpdateView();
}

function UpdateView() {
	switch (cameraSelect) {
		case cams_3D.LeftRight:
			leftCam.transform.position = transform.position + transform.TransformDirection(-interaxial/2000.0, 0, 0);
			rightCam.transform.position = transform.position + transform.TransformDirection(interaxial/2000.0, 0, 0);
			break;
		case cams_3D.LeftOnly:
			leftCam.transform.position = transform.position + transform.TransformDirection(-interaxial/2000.0, 0, 0);
			rightCam.transform.position = transform.position + transform.TransformDirection(-interaxial/2000.0, 0, 0);
			break;
		case cams_3D.RightOnly:
			leftCam.transform.position = transform.position + transform.TransformDirection(interaxial/2000.0, 0, 0);
			rightCam.transform.position = transform.position + transform.TransformDirection(interaxial/2000.0, 0, 0);
			break;
		case cams_3D.RightLeft:
			leftCam.transform.position = transform.position + transform.TransformDirection(interaxial/2000.0, 0, 0);
			rightCam.transform.position = transform.position + transform.TransformDirection(-interaxial/2000.0, 0, 0);
			break;
	}
	leftCam.transform.rotation = transform.rotation; 
	rightCam.transform.rotation = transform.rotation;
	switch (cameraSelect) {
		case cams_3D.LeftRight:
			leftCam.camera.projectionMatrix = setProjectionMatrix(true);
			rightCam.camera.projectionMatrix = setProjectionMatrix(false);
			break;
		case cams_3D.LeftOnly:
			leftCam.camera.projectionMatrix = setProjectionMatrix(true);
			rightCam.camera.projectionMatrix = setProjectionMatrix(true);
			break;
		case cams_3D.RightOnly:
			leftCam.camera.projectionMatrix = setProjectionMatrix(false);
			rightCam.camera.projectionMatrix = setProjectionMatrix(false);
			break;
		case cams_3D.RightLeft:
			leftCam.camera.projectionMatrix = setProjectionMatrix(false);
			rightCam.camera.projectionMatrix = setProjectionMatrix(true);
			break;
	}
}

function setProjectionMatrix(isLeftCam : boolean) : Matrix4x4 {
	var left : float;
	var right : float;
	var a : float;
	var b : float;
	var FOVrad : float;
	var tempAspect: float = camera.aspect;
	FOVrad = camera.fieldOfView / 180.0 * Mathf.PI;
	if (!sideBySideSqueezed) {
		tempAspect /= 2;	// if side by side unsqueezed, double width
	}
	a = camera.nearClipPlane * Mathf.Tan(FOVrad * 0.5);
	b = camera.nearClipPlane / zeroPrlxDist;
	if (isLeftCam) {
		left  = (-tempAspect * a) + (interaxial/2000.0 * b) + (H_I_T/100) + (offAxisFrustum/100);
		right =	(tempAspect * a) + (interaxial/2000.0 * b) + (H_I_T/100) + (offAxisFrustum/100);
	} else {
		left  = (-tempAspect * a) - (interaxial/2000.0 * b) - (H_I_T/100) + (offAxisFrustum/100);
		right =	(tempAspect * a) - (interaxial/2000.0 * b) - (H_I_T/100) + (offAxisFrustum/100);
	}
	return PerspectiveOffCenter(left, right, -a, a, camera.nearClipPlane, camera.farClipPlane);
} 

function PerspectiveOffCenter(
	left : float, right : float,
	bottom : float, top : float,
	near : float, far : float ) : Matrix4x4 {
	var x =  (2.0 * near) / (right - left);
	var y =  (2.0 * near) / (top - bottom);
	var a =  (right + left) / (right - left);
	var b =  (top + bottom) / (top - bottom);
	var c = -(far + near) / (far - near);
	var d = -(2.0 * far * near) / (far - near);
	var e = -1.0;

	var m : Matrix4x4;
	m[0,0] = x;  m[0,1] = 0;  m[0,2] = a;  m[0,3] = 0;
	m[1,0] = 0;  m[1,1] = y;  m[1,2] = b;  m[1,3] = 0;
	m[2,0] = 0;  m[2,1] = 0;  m[2,2] = c;  m[2,3] = d;
	m[3,0] = 0;  m[3,1] = 0;  m[3,2] = e;  m[3,3] = 0;
	return m;
}

function OnDrawGizmos () {
	var gizmoLeft : Vector3 = transform.position + transform.TransformDirection(-interaxial/2000.0, 0, 0);
	var gizmoRight : Vector3 = transform.position + transform.TransformDirection(interaxial/2000.0, 0, 0);
	var gizmoTarget : Vector3 = transform.position + transform.TransformDirection (Vector3.forward) * zeroPrlxDist;
	Gizmos.color = Color (1,1,1,1);
	Gizmos.DrawLine (gizmoLeft, gizmoTarget);
	Gizmos.DrawLine (gizmoRight, gizmoTarget);
	Gizmos.DrawLine (gizmoLeft, gizmoRight);
	Gizmos.DrawSphere (gizmoLeft, 0.02);
	Gizmos.DrawSphere (gizmoRight, 0.02);
	Gizmos.DrawSphere (gizmoTarget, 0.02);
}

// end s3dCameraSBS.js
