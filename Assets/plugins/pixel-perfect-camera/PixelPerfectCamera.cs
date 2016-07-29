/** =========================================================================
 *      Pixel Perfect Camera
 *  -------------------------------------------------------------------------
 *    Summary : Set the main camera's dimensions to the desired size (in
 *              pixels) and scale it as much as possible when displaying it.
 *    License : Zlib license
 *    Author  : GFM <gfmandaji@hotmail.com>
 *  -------------------------------------------------------------------------
 *    Usage:
 *      Add this script to an empty game object to restrict the in-game
 *    resolution to a fixed dimension in pixels. This in-game view
 *    (referenced as virtual window/view) is automatically scaled to the
 *    greatest integer factor that fits both width and height, when rendered
 *    to the screen.
 *  -------------------------------------------------------------------------
 *    Attributes:
 *      gameWidth     : Width of the game's virtual window
 *      gameHeight    : Height of the game's virtual window
 *      pixelsToUnits : Define the virtual window's dimensions, in units
 *      baseMaterial  : Template for a new material, used to render the
 *                      virtual window to the screen
 *      targetCamera  : Camera to be made pixel perfect
 *  -------------------------------------------------------------------------
 *    Customization:
 *      It's possible to customize how the virtual camera is rendered to the
 *    screen. To do so, implement your own shader and assign it to the game
 *    object's base material. Then, extend this class and override the
 *    function 'adjustOutput'.
 *      The base implementation must be called, in order to properly set the
 *    new scaling factor and to re-center the displayed image whenever the
 *    window gets resized. After that, the override function may set the
 *    material however needed.
 *      The following read-only attributes may be used on a derived class:
 *        windowOffset   : Offset of the image from the window's center
 *        windowWidth    : Current width of the window
 *        windowHeight   : Current height of the window
 *        scale          : Current scaling factor
 *        outputMaterial : The material used to render the virtual buffer
 *                         to the screen
 *  ========================================================================= */

using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

using ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode;
using ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage;

public class PixelPerfectCamera : MonoBehaviour {

	/** =====================================================================
	 *      Texture default configurations
	 *  ===================================================================== */

	/** Depth used by the render texture */
	private const int depth = 0;
	/** Format of the render texture*/
	private const RenderTextureFormat fmt = RenderTextureFormat.ARGB32;
	/** Name of the created render texture */
	private const string textureName = "(dynamic) virtual window";
	private const RenderTextureReadWrite readWrite =
			RenderTextureReadWrite.Default;
	private const FilterMode filterMode = FilterMode.Point;
	private const int anisoLevel = 0;
	private const int antiAliasing = 1;
	private const TextureWrapMode wrapMode = TextureWrapMode.Clamp;


	/** =====================================================================
	 *      Material default configurations
	 *  ===================================================================== */

	/** Name of the created render texture */
	private const string materialName = "(dynamic) output material";


	/** =====================================================================
	 *      Output camera default configurations
	 *  ===================================================================== */

	/** Whether the output camera is orthographic */
	private const bool orthographic = true;
	/** Define a 1 unit wide view frustrum, centered at Z = 0 */
	private const float nearClipPlane = -0.5f;
	/** Define a 1 unit wide view frustrum, centered at Z = 0 */
	private const float farClipPlane = 0.5f;
	/** Default pillowing color (Unity doesn't allow const Color =/ ) */
	private static Color backgroundColor = Color.black;
	/** How the camera should be cleared, every frame */
	private const CameraClearFlags clearFlags = CameraClearFlags.SolidColor;


	/** =====================================================================
	 *      Mesh renderer default configurations
	 *  ===================================================================== */

	private const ShadowCastingMode shadowCastingMode = ShadowCastingMode.Off;
	private const bool receiveShadows = false;
	private const bool useLightProbes = false;
	private const ReflectionProbeUsage reflectionProbeUsage =
			ReflectionProbeUsage.Off;


	/** =====================================================================
	 *      Script-created objects
	 *  ===================================================================== */

	/** Texture where the virtual screen gets rendered */
	private RenderTexture _virtualBuffer;

	/** Renders _outputObject to the screen/window */
	private Camera _outputCamera;

	/** Unlit texture shader, used by _outputObject */
	public Material outputMaterial { get { return this._outputMaterial; } }
	private Material _outputMaterial = null;

	/** Quad used to display the virtual buffer */
	private GameObject _outputObject;


	/** =====================================================================
	 *      Cached values
	 *  ===================================================================== */

	/** Offset of the output object from the window's center */
	public Vector3 windowOffset { get { return this._windowOffset; } }
	private Vector3 _windowOffset;

	/** Width of the output window */
	public int windowWidth { get { return this._windowWidth; } }
	private int _windowWidth;

	/** Height of the output window */
	public int windowHeight { get { return this._windowHeight; } }
	private int _windowHeight;

	/** Current scalling factor */
	public int scale { get { return this._scale; } }
	private int _scale;

	/** =====================================================================
	 *      Configurable parameters
	 *  ===================================================================== */

	/** Width of the game's virtual window */
	public int gameWidth = 320;
	
	/** Height of the game's virtual window */
	public int gameHeight = 240;
	
	/** Map a given amount of "real pixels" (i.e., within the virtual window)
	 * to a unit */
	public int pixelsToUnits = 16;

	/** Base material, should be a "unlit texture shader" */
	public Shader outputShader = null;

	/** Camera to be made pixel perfect */
	public Camera targetCamera = null;


	/** =====================================================================
	 *      Internal/Helper functions
	 *  ===================================================================== */

	/**
	 * Create a new render texture of the desired dimension.
	 *
	 * The texture dimension should be defined on the inspector, through the
	 * attributes "Game Width" and "Game Height".
	 */
	private void createVirtualBuffer() {
		this._virtualBuffer = new RenderTexture(this.gameWidth, this.gameHeight
				, PixelPerfectCamera.depth, PixelPerfectCamera.fmt
				, PixelPerfectCamera.readWrite);
		this._virtualBuffer.filterMode = PixelPerfectCamera.filterMode;
		this._virtualBuffer.anisoLevel = PixelPerfectCamera.anisoLevel;
		this._virtualBuffer.antiAliasing = PixelPerfectCamera.antiAliasing;
		this._virtualBuffer.wrapMode = PixelPerfectCamera.wrapMode;
		this._virtualBuffer.name = PixelPerfectCamera.textureName;

		this._virtualBuffer.Create();
	}

	/**
	 * Create the material used by the output object.
	 *
	 * This material should use the default shader "Unlit/Texture" and the
	 * render texture (i.e., _virtualBuffer) is assigned as its input texture.
	 * This way, the output object will actually render the virtual buffer to
	 * the screen.
	 *
	 * This function must be called after 'createVirtualBuffer', since it uses
	 * _virtualBuffer (which is set by that function call).
	 */
	private void createOutputMaterial() {
		this._outputMaterial = new Material(this.outputShader);
		this._outputMaterial.mainTexture = this._virtualBuffer;
		this._outputMaterial.name = PixelPerfectCamera.materialName;
	}

	/**
	 * Adjust the scene's main camera so it may act as a virtual camera.
	 *
	 * Modifies the main camera so it renders to a previously created render
	 * texture of the desired resolution. The camera's normalized dimensions is
	 * set according to the attribute "Pixels To Units" set on the inspector.
	 * Every imported asset must have that same "Pixels To Units" to ensure that
	 * their dimension, as viewed on the scene editor, is kept when rendered.
	 *
	 * This function also modifies the position of this game object. It's
	 * placed outside the view frustrum (10 units away from its near plane).
	 *
	 * This function must be called after 'createVirtualBuffer', since it uses
	 * _virtualBuffer (which is set by that function call).
	 */
	private void adjustTargetCamera() {
		Vector3 pos;

		this.targetCamera.targetTexture = this._virtualBuffer;
		this.targetCamera.aspect = this.gameWidth / (float)this.gameHeight;
		this.targetCamera.orthographicSize = this.gameHeight
				/ this.pixelsToUnits * 0.5f;

		pos = this.targetCamera.transform.position;
		pos.z = pos.z - this.targetCamera.nearClipPlane - 10f;
		this.transform.position = pos;
	}

	/**
	 * Create the output camera.
     */
	private void createOutputCamera() {
		this._outputCamera = this.gameObject.AddComponent<Camera>();
		this._outputCamera.orthographic = PixelPerfectCamera.orthographic;
		this._outputCamera.nearClipPlane = PixelPerfectCamera.nearClipPlane;
		this._outputCamera.farClipPlane = PixelPerfectCamera.farClipPlane;
		this._outputCamera.backgroundColor = PixelPerfectCamera.backgroundColor;
		this._outputCamera.clearFlags = PixelPerfectCamera.clearFlags;
	}

	/**
	 * Create the output object.
	 *
	 * The object is a simple quad with a material that "maps" the render
	 * texture to it. It's placed at the center of this object (therefore,
	 * within the output camera).
	 *
	 * This function must be called after 'createOutputMaterial', since it uses
	 * _outputMaterial (which is instantiated by that function call).
	 */
	private void createOutputObject() {
		MeshRenderer mr;

		this._outputObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
		GameObject.Destroy(this._outputObject.GetComponent<MeshCollider>());

		this._outputObject.transform.SetParent(this.transform);
		this._outputObject.transform.localPosition = Vector3.zero;
		mr = this._outputObject.gameObject.GetComponent<MeshRenderer>();

		mr.shadowCastingMode = PixelPerfectCamera.shadowCastingMode;
		mr.receiveShadows = PixelPerfectCamera.receiveShadows;
		mr.useLightProbes = PixelPerfectCamera.useLightProbes;
		mr.reflectionProbeUsage = PixelPerfectCamera.reflectionProbeUsage;

		mr.material = this._outputMaterial;
	}

	/**
	 * Resize and center the displayed texture.
	 *
	 * The image is resized by setting the dimensions of the output object
	 * (through its scale). The orthographic size of the output camera is
	 * also modified (and set to twice the window's height) to ease centering
	 * the object on the screen.
	 *
	 * Whenever one of the screen's dimensions is odd, the displayed imaged
	 * would be rendered on a half-pixel, leading to a slightly blurred
	 * image. To avoid that, the image position (on the odd coordinate) is
	 * set to 1.
	 *
	 * This function also re-caches the window's dimensions, so it's possible
	 * to detected when the window is resized.
	 */
	virtual protected void adjustOutput() {
		int scaleX, scaleY, scale;

		this._outputCamera.orthographicSize = this._outputCamera.pixelHeight;

		scaleX = this._outputCamera.pixelWidth / this.gameWidth;
		scaleY = this._outputCamera.pixelHeight / this.gameHeight;
		scale = 2 * Mathf.Min(scaleX, scaleY);

		this._windowOffset.Set((float)(this._outputCamera.pixelWidth % 2)
				, (float)(this._outputCamera.pixelHeight % 2), 0.0f);

		this._outputObject.transform.localScale = new Vector3(
				this.gameWidth * scale, this.gameHeight * scale, 1f);
		this._outputObject.transform.localPosition = this._windowOffset;

		this._windowWidth = this._outputCamera.pixelWidth;
		this._windowHeight = this._outputCamera.pixelHeight;
		this._scale = scale / 2;
	}


	/** =====================================================================
	 *      MonoBehaviour events
	 *  ===================================================================== */

	/**
	 * Automatically convert a common camera to a pixel perfect one.
	 */
	void Awake() {
		if (this.outputShader == null) {
			Debug.LogError("No output shader found!\nAssign the output shader"
					+ " on the inspector and run again. The default base"
					+ " material can be found on the plugin directory as 'Unlit"
					+ " Texture'");
			GameObject.Destroy(this.gameObject);
			return;
		}

		if (this.targetCamera == null) {
			this.targetCamera = Camera.main;
		}

		this.createVirtualBuffer();
		this.createOutputMaterial();
		this.adjustTargetCamera();
		this.createOutputCamera();
		this.createOutputObject();
		this.adjustOutput();
	}

	/**
	 * Center and re-scale the displayed image.
	 *
	 * At the end of every frame, check if the screen dimension has changed. If
	 * so, recalculate the scaling of the virtual buffer and re-center it on
	 * the screen.
	 *
	 * The screen dimensions have to be polled since there doesn't seem to
	 * exist any event issued be Unity to signal that.
	 *
	 * The virtual buffer is also checked, for the case that its texture needs
	 * to be recreated. This will usually be unecessary, but it's here for the
	 * odd case that it happens...
	 */
	void LateUpdate() {
		if (this._windowWidth != this._outputCamera.pixelWidth ||
				this._windowHeight != this._outputCamera.pixelHeight) {
			this.adjustOutput();
		}

		if (!this._virtualBuffer.IsCreated()) {
			Debug.Log("Re-creating render texture...");
			this._virtualBuffer.Create();
		}
	}
}
