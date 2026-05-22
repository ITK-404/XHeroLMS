#define BUILTIN // If you are not using Builtin then comment out this line to stop errors about post processing

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

#if BUILTIN
using UnityEngine.Rendering.PostProcessing;
#endif

namespace MegaBook
{
	[ExecuteInEditMode]
	public class BookCamera : MonoBehaviour
	{
		public GameObject target;
		public float distance = 10.0f;
		public float xSpeed = 2500.0f;
		public float ySpeed = 1200.0f;
		public float zSpeed = 10.0f;
		public float yMinLimit = -20.0f;
		public float yMaxLimit = 80.0f;
		public float xMinLimit = -20.0f;
		public float xMaxLimit = 20.0f;
		public Vector3 offset;
		public float trantime = 4.0f;
		public float nx = 0.0f;
		public float ny = 0.0f;
		public float nz = 0.0f;
		public float delay = 0.24f;
		public float delayz = 0.24f;

		float x = 0.0f;
		float y = 0.0f;
		float vx = 0.0f;
		float vy = 0.0f;
		float vz = 0.0f;
		float t = 0.0f;

		Vector3 tpos = new Vector3();

		MeshRenderer render;
		SkinnedMeshRenderer srender;
		MeshFilter filter;
		CameraShake shake;

		public float shakeAmt = 1.0f;

#if BUILTIN
		DepthOfField dof;
		public PostProcessProfile postprocess;
		bool havedof;
#endif

		float cdofdist;
		GameObject[] targets;
		int currentIndex;
		Vector3 newpos = Vector3.zero;

		public AnimationCurve crv = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

		float currentDistance;
		float dz;
		float dofdistance;

		public Gradient coltest;
		public Material groundMat;
		public Color[] colors;
		public float ctime = 1.0f;
		public float ct = 10.0f;
		public Color currentCol;
		public bool slowfade;

		[Range(0, 1)]
		public float fogLerp = 0.25f;

		[Range(0, 1)]
		public float ambientLerp = 0.75f;

		public float fadetime = 20.0f;

		int lastindex;
		public int colIndex;
		public bool doRaycast;
		public float speed = 1.0f;
		public bool lookat;

		float fov;
		float cfov;
		float fovvel;
		float startfov;
		Vector3 mvel;
		bool firstrun;

		public float dofdamp = 0.25f;
		public GameObject controlsOn;
		public GameObject controlsOff;

[Header("Unity 6 / Simulator Input")]
public bool rotateWithRightMouse = true;
public bool rotateWithLeftMouse = true;
public bool rotateWithTouch = true;
public bool ignorePointerOverUI = true;

[Header("Double Tap Rotate")]
public bool requireDoubleTapToRotate = true;

// Khoảng cách thời gian tối đa giữa 2 lần chạm.
// Nếu chạm lần 2 sau thời gian này thì reset, không xoay.
public float doubleTapMaxDelay = 0.35f;

// Nếu muốn tránh double tap cách nhau quá xa trên màn hình.
public float doubleTapMaxScreenDistance = 80.0f;

// Sau khi double tap thành công, thả tay/chuột ra thì khóa lại.
// Lần sau muốn xoay phải double tap lại.
public bool lockAgainWhenPointerReleased = true;

bool rotateUnlocked;
float lastTapTime = -999.0f;
Vector2 lastTapPosition;
bool wasPointerPressed;

		public float minDistance = 0.5f;
		public float maxDistance = 10.0f;

		public float minAperture = 12.0f;
		public float maxAperture = 5.0f;
		public float minFocalLength = 200.0f;
		public float maxFocalLength = 100.0f;

		static public int Compare(GameObject o1, GameObject o2)
		{
			if ( o1.transform.position.x < o2.transform.position.x )
				return -1;

			if ( o1.transform.position.x > o2.transform.position.x )
				return 1;

			return 0;
		}

		void Start()
		{
			newpos = transform.position;

			if ( Camera.main )
			{
				fov = Camera.main.fieldOfView;
				cfov = fov;
				startfov = fov;
			}

			lastindex = 0;

			if ( colors != null && colors.Length > 0 )
			{
				colIndex = Mathf.Clamp(colIndex, 0, colors.Length - 1);
				currentCol = colors[colIndex];
			}

			targets = GameObject.FindGameObjectsWithTag("LookAt");

			if ( targets != null )
				System.Array.Sort(targets, Compare);

			for ( int i = 0; i < targets.Length; i++ )
			{
				if ( targets[i] == target )
				{
					currentIndex = i;
					break;
				}
			}

			shake = GetComponent<CameraShake>();

			if ( target )
				NewTarget(target);

			if ( target )
				tpos = target.transform.position;
			else
			{
				Vector3 angles = transform.eulerAngles;
				x = angles.y;
				y = angles.x;
			}

			vx = vy = vz = 0.0f;
			x = nx;
			y = ny;
			distance = nz;

			cdofdist = nz;
			currentDistance = nz;
			dofdistance = nz;

#if BUILTIN
			if ( postprocess )
				havedof = postprocess.TryGetSettings<DepthOfField>(out dof);
#endif

			t = 8.0f;

			Application.targetFrameRate = -1;
		}

		bool KeyDown(Key key)
		{
			return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
		}

		bool KeyHeld(Key key)
		{
			return Keyboard.current != null && Keyboard.current[key].isPressed;
		}

bool IsPointerOverUI()
{
	if ( !ignorePointerOverUI || EventSystem.current == null )
		return false;

	if ( Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed )
	{
		int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();

		if ( EventSystem.current.IsPointerOverGameObject(touchId) )
			return true;
	}

	if ( Mouse.current != null && Mouse.current.leftButton.isPressed )
	{
		if ( EventSystem.current.IsPointerOverGameObject() )
			return true;
	}

	return false;
}

bool IsTouchPressed()
{
	return rotateWithTouch
		&& Touchscreen.current != null
		&& Touchscreen.current.primaryTouch.press.isPressed;
}

bool WasTouchPressedThisFrame()
{
	return rotateWithTouch
		&& Touchscreen.current != null
		&& Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
}

bool IsMousePressed()
{
	if ( Mouse.current == null )
		return false;

	if ( rotateWithRightMouse && Mouse.current.rightButton.isPressed )
		return true;

	if ( rotateWithLeftMouse && Mouse.current.leftButton.isPressed )
		return true;

	return false;
}

bool WasMousePressedThisFrame()
{
	if ( Mouse.current == null )
		return false;

	if ( rotateWithRightMouse && Mouse.current.rightButton.wasPressedThisFrame )
		return true;

	if ( rotateWithLeftMouse && Mouse.current.leftButton.wasPressedThisFrame )
		return true;

	return false;
}

bool IsPointerPressed()
{
	return IsTouchPressed() || IsMousePressed();
}

bool WasPointerPressedThisFrame()
{
	return WasTouchPressedThisFrame() || WasMousePressedThisFrame();
}

Vector2 PointerPosition()
{
	if ( Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed )
		return Touchscreen.current.primaryTouch.position.ReadValue();

	if ( Mouse.current != null )
		return Mouse.current.position.ReadValue();

	return Vector2.zero;
}

Vector2 PointerDelta()
{
	if ( Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed )
		return Touchscreen.current.primaryTouch.delta.ReadValue();

	if ( Mouse.current != null )
		return Mouse.current.delta.ReadValue();

	return Vector2.zero;
}

void UpdateDoubleTapRotateState()
{
	if ( !requireDoubleTapToRotate )
	{
		rotateUnlocked = true;
		return;
	}

	bool pointerPressed = IsPointerPressed();

	if ( !pointerPressed )
	{
		if ( lockAgainWhenPointerReleased )
			rotateUnlocked = false;

		wasPointerPressed = false;
		return;
	}

	if ( WasPointerPressedThisFrame() && !IsPointerOverUI() )
	{
		float now = Time.unscaledTime;
		Vector2 currentTapPosition = PointerPosition();

		bool validTime = now - lastTapTime <= doubleTapMaxDelay;
		bool validDistance = Vector2.Distance(currentTapPosition, lastTapPosition) <= doubleTapMaxScreenDistance;

		if ( validTime && validDistance )
		{
			rotateUnlocked = true;
			lastTapTime = -999.0f;
		}
		else
		{
			rotateUnlocked = false;
			lastTapTime = now;
			lastTapPosition = currentTapPosition;
		}
	}

	if ( Time.unscaledTime - lastTapTime > doubleTapMaxDelay )
	{
		lastTapTime = -999.0f;
	}

	wasPointerPressed = pointerPressed;
}

bool RightMouseHeld()
{
	if ( IsPointerOverUI() )
		return false;

	UpdateDoubleTapRotateState();

	if ( requireDoubleTapToRotate && !rotateUnlocked )
		return false;

	return IsPointerPressed();
}

float MouseX()
{
	return PointerDelta().x;
}

float MouseY()
{
	return PointerDelta().y;
}

float MouseScroll()
{
	if ( Mouse.current == null )
		return 0.0f;

	return Mouse.current.scroll.ReadValue().y / 1200.0f;
}

		public void NewTarget(GameObject targ)
		{
			if ( targ == null )
				return;

			target = targ;
			t = 0.0f;
			tpos = newpos;

			ChangeCol();

			if ( targ.transform.parent )
			{
				MegaBookBuilder newbook = targ.transform.parent.GetComponentInChildren<MegaBookBuilder>();

				if ( newbook )
				{
					MegaBookControl con = GetComponent<MegaBookControl>();

					if ( con )
						con.SetTarget(newbook);
				}
			}
		}

		void DoColor()
		{
			if ( colors == null || colors.Length == 0 )
				return;

			if ( Application.isPlaying || slowfade )
			{
				if ( slowfade )
				{
					if ( Application.isPlaying )
					{
						ct += Time.deltaTime * speed;

						if ( ct > fadetime )
							ct = 0.0f;
					}

					if ( coltest != null && fadetime > 0.0f )
						currentCol = coltest.Evaluate(ct / fadetime);
				}
				else
				{
					if ( ct < ctime )
					{
						ct += Time.deltaTime;
						currentCol = Color.Lerp(colors[lastindex], colors[colIndex], ct / ctime);
					}
				}
			}

			RenderSettings.fogColor = Color.Lerp(currentCol, Color.white, fogLerp);
			RenderSettings.ambientLight = Color.Lerp(currentCol, Color.white, ambientLerp);

			if ( groundMat )
				groundMat.color = currentCol;
		}

		void ChangeCol()
		{
			if ( colors == null || colors.Length == 0 )
				return;

			if ( !slowfade )
			{
				lastindex = colIndex;
				colIndex++;

				if ( colIndex >= colors.Length )
					colIndex = 0;

				ct = 0.0f;
			}
		}

		public void NextTarget()
		{
			if ( targets == null || targets.Length == 0 )
				return;

			currentIndex++;

			if ( currentIndex >= targets.Length )
				currentIndex = targets.Length - 1;

			NewTarget(targets[currentIndex]);
		}

		public void PrevTarget()
		{
			if ( targets == null || targets.Length == 0 )
				return;

			currentIndex--;

			if ( currentIndex < 0 )
				currentIndex = 0;

			NewTarget(targets[currentIndex]);
		}

		void LateUpdate()
		{
			if ( KeyDown(Key.F1) )
			{
				if ( controlsOn && controlsOff )
				{
					if ( controlsOn.activeInHierarchy )
					{
						controlsOn.SetActive(false);
						controlsOff.SetActive(true);
					}
					else
					{
						controlsOn.SetActive(true);
						controlsOff.SetActive(false);
					}
				}
			}

			if ( KeyDown(Key.Escape) )
				Application.Quit();

			DoColor();

			if ( KeyDown(Key.F4) )
				lookat = !lookat;

			if ( KeyDown(Key.A) || KeyDown(Key.LeftArrow) )
			{
				if ( targets != null && targets.Length > 0 )
				{
					currentIndex++;

					if ( currentIndex >= targets.Length )
						currentIndex = targets.Length - 1;

					NewTarget(targets[currentIndex]);
				}
			}

			if ( KeyDown(Key.D) || KeyDown(Key.RightArrow) )
			{
				if ( targets != null && targets.Length > 0 )
				{
					currentIndex--;

					if ( currentIndex < 0 )
						currentIndex = 0;

					NewTarget(targets[currentIndex]);
				}
			}

			if ( target )
			{
				if ( !lookat )
				{
					if ( RightMouseHeld() )
					{
						nx = x + MouseX() * xSpeed * 0.02f;
						ny = y - MouseY() * ySpeed * 0.02f;
					}

					float scroll = MouseScroll();

					if ( KeyHeld(Key.V) )
						fov -= scroll * zSpeed * 0.5f;
					else
						nz = nz - (scroll * zSpeed);

					if ( Application.isPlaying )
					{
						x = Mathf.SmoothDamp(x, nx, ref vx, delay);
						y = Mathf.SmoothDamp(y, ny, ref vy, delay);
						distance = Mathf.SmoothDamp(distance, nz, ref vz, delayz);
					}
					else
					{
						x = nx;
						y = ny;
						distance = nz;
					}

					y = ClampAngle(y, yMinLimit, yMaxLimit);

					if ( distance < minDistance )
					{
						distance = minDistance;
						nz = minDistance;
					}

					if ( distance > maxDistance )
					{
						distance = maxDistance;
						nz = maxDistance;
					}

					Vector3 c = target.transform.position + offset;

					if ( t < trantime )
						t += Time.deltaTime;

					if ( !firstrun )
					{
						firstrun = true;
						newpos = c;
					}

					newpos = Vector3.SmoothDamp(newpos, c, ref mvel, 0.25f);

					Quaternion rotation = Quaternion.Euler(y, x, 0.0f);
					Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + newpos;

					Quaternion srot = Quaternion.identity;

					if ( shake && Application.isPlaying )
					{
						Vector3 rot = Vector3.zero;
						shake.DoUpdate(ref position, ref rot, shakeAmt);
						srot = Quaternion.Euler(rot);
					}

					if ( doRaycast )
					{
						RaycastHit hit;
						Ray ray = new Ray(newpos, (position - newpos).normalized);

						if ( Physics.Raycast(ray, out hit, (position - newpos).magnitude, 1 << 10) )
							position = hit.point + (Vector3.up * 0.2f);
					}
					else
					{
						if ( position.y < 0.2f )
							position.y = 0.2f;
					}

					transform.rotation = rotation * srot;
					transform.position = position;
				}
				else
				{
					float scroll = MouseScroll();

					if ( KeyHeld(Key.V) )
						fov -= scroll * zSpeed * 0.5f;

					Vector3 c = target.transform.position + offset;

					newpos = Vector3.SmoothDamp(newpos, c, ref mvel, 0.25f);

					Quaternion srot = Quaternion.identity;

					if ( shake && Application.isPlaying )
					{
						Vector3 rot = Vector3.zero;
						Vector3 p = Vector3.zero;
						shake.DoUpdate(ref p, ref rot, shakeAmt);
						srot = Quaternion.Euler(rot);
					}

					Quaternion rotation = Quaternion.LookRotation(newpos - transform.position);
					transform.rotation = rotation * srot;
				}

				UpdateDOF();

				fov = Mathf.Clamp(fov, 8.0f, 45.0f);
				cfov = Mathf.SmoothDamp(cfov, fov, ref fovvel, 0.25f);

				if ( Camera.main )
					Camera.main.fieldOfView = cfov;
			}
		}

		static float ClampAngle(float angle, float min, float max)
		{
			if ( angle < -360.0f )
				angle += 360.0f;

			if ( angle > 360.0f )
				angle -= 360.0f;

			return Mathf.Clamp(angle, min, max);
		}

		public void SetDOF(float dist)
		{
#if BUILTIN
			if ( havedof )
			{
				float dofalpha = Mathf.Clamp01((dist - minDistance) / (maxDistance - minDistance));

				dof.focusDistance.value = dist;
				dof.aperture.value = Mathf.Lerp(minAperture, maxAperture, dofalpha);
				dof.focalLength.value = Mathf.Lerp(minFocalLength, maxFocalLength, dofalpha);
			}
#endif
		}

		float GetDOFDist(Ray ray)
		{
			RaycastHit hit;

			if ( doRaycast )
			{
				if ( Physics.Raycast(ray, out hit) )
					dofdistance = hit.distance;

				return dofdistance;
			}
			else
			{
				return distance;
			}
		}

		public void UpdateDOF()
		{
			Ray ray = new Ray();

			ray.origin = transform.position;
			ray.direction = Quaternion.Euler(transform.eulerAngles) * Vector3.forward;

			dofdistance = GetDOFDist(ray);

			currentDistance = Mathf.SmoothDamp(currentDistance, dofdistance, ref dz, dofdamp);

			SetDOF(currentDistance);
		}
	}
}