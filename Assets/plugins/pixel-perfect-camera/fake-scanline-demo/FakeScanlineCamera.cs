/** =========================================================================
 *      Pixel Perfect Camera
 *  -------------------------------------------------------------------------
 *    Summary : Pixel perfect camera with fake scanlines
 *    License : Zlib license
 *    Author  : GFM <gfmandaji@hotmail.com>
*  ========================================================================= */

public class FakeScanlineCamera : PixelPerfectCamera {

	/**
	 * After resizing and centering the output texture, set the required
	 * attributes on the shader.
	 */
	protected override void adjustOutput () {
		base.adjustOutput ();

		/* The compiler was being dumb about assigning it to an int, so... */
		if (this.scale > 1) {
			this.outputMaterial.SetFloat("apply", 1.0f);
		}
		else {
			this.outputMaterial.SetFloat("apply", 0.0f);
		}
		this.outputMaterial.SetInt("scale", this.scale);
		this.outputMaterial.SetInt("height", this.scale * this.gameHeight);
	}
}
