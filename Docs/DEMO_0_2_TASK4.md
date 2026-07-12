# Demo 0.2 Task 4 - Camera and Scene Wiring

This batch extends the existing perspective `CameraController` and completes the Demo 0.2 scene references without changing gameplay values, UI layout design, camera rotation, or the initial camera transform.

## Camera Controls

- Hold the middle mouse button and drag to move on the XZ plane.
- Use the mouse wheel over the world to adjust field of view.
- Position is clamped to configurable X and Z limits; field of view is clamped to configurable minimum and maximum values.
- Camera input is blocked when the pointer is over UGUI or the game phase is not `Playing`.
- A drag that starts over UI remains blocked until the mouse button is released.

## Scene Wiring

Run **Tools > Community Manager > Apply Demo 0.2 Scene Wiring** to safely wire all nine event assets and the `GameRoot` reference on the camera controller. The command validates one UI EventSystem, one camera controller, and zero Missing Script components before reporting success.
