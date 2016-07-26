# Pixel Perfect Canvas

Using Unity 3D for pixel art games comes with two main issues: there's no
trivial way to have a window with fixed dimensions, in pixels, nor is there
a way to restrict the view re-scaling to integer factors only.

The later may be somewhat solved by importing assets with a high "pixel to
units" value. However, this doesn't take into account that each tile/sprite
should ideally be a multiple of a tile.

This plugin tries to solve those issues by rendering the main camera to a
texture and ensuring that the displayed image is centered at the screen and
scaled by an integer factor. This solution has the added effect that one may
even write a custom shader for modifying how the upscaled image will be
rendered (e.g., adding fake scanlines).

Also, although it's usually required to create the render texture
beforehand, this plugin works as expected and creates its own ephemeral
texture.


## Installing

Copy the 'plugin/' directory into your project's 'Assets/' directory. No
extra step is required, since it comes bundled with a default material and
the target texture is created at run time.


## Using

Add the 'Pixel Perfect Camera' component to an empty game object. The canvas
should be configured by the following attributes:

* **Game Width:** Width of the game's virtual window
* **Game Height:** Height of the game's virtual window
* **Pixels To Units:** Define the virtual window's dimensions, in units
* **Base Material:** Template for a new material, used to render the virtual
  window to the screen
* **Target Camera:** Camera to be made pixel perfect

If no "Base Material" is set, the component will log an error and destroy
itself (to avoid exceptions). At the very least, the bundled "Unlit Texture"
should be assigned to the "Base Material".


## Customizing

It's possible to customize how the virtual window gets rendered to the
screen. To do so, one should write their own shader, assign it to the
material used by the component and extend the "Pixel Perfect Camera" class
(so any custom parameter shall be set on the shader).

The only virtual function on the class is 'adjustOutput', which is called
whenever the window dimensions change and the view may be re-scaled. After
calling the base implementation, any required parameters should be set on
the material.

The following read-only attributes are available on the derived class:

* **windowOffset:** Offset of the image from the window's center
* **windowWidth:** Current width of the window
* **windowHeight:** Current height of the window
* **scale:** Current scaling factor
* **outputMaterial:** The material used to render the virtual buffer

