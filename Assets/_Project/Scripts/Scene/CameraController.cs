using UnityEngine;
using UnityEngine.InputSystem;

namespace NeighborhoodManager.Scene
{
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private float dragSpeed = 0.02f;
        [SerializeField] private float zoomSpeed = 0.02f;
        [SerializeField] private float minimumHeight = 8f;
        [SerializeField] private float maximumHeight = 35f;

        private InputAction pointAction;
        private InputAction dragAction;
        private InputAction zoomAction;
        private Vector2 previousPointerPosition;
        private bool wasDragging;

        private void OnEnable()
        {
            pointAction = new InputAction("Point", binding: "<Pointer>/position");
            dragAction = new InputAction("CameraDrag", InputActionType.Button, "<Mouse>/rightButton");
            dragAction.AddBinding("<Mouse>/middleButton");
            zoomAction = new InputAction("CameraZoom", binding: "<Mouse>/scroll/y");
            pointAction.Enable();
            dragAction.Enable();
            zoomAction.Enable();
        }

        private void OnDisable()
        {
            pointAction?.Dispose();
            dragAction?.Dispose();
            zoomAction?.Dispose();
        }

        private void Update()
        {
            Vector2 pointerPosition = pointAction.ReadValue<Vector2>();
            bool dragging = dragAction.IsPressed();
            if (dragging && wasDragging)
            {
                Vector2 delta = pointerPosition - previousPointerPosition;
                Vector3 movement = ((-transform.right * delta.x) + (-Vector3.ProjectOnPlane(transform.up, Vector3.up).normalized * delta.y)) * dragSpeed;
                transform.position += movement;
            }

            float zoom = zoomAction.ReadValue<float>();
            if (Mathf.Abs(zoom) > 0.01f)
            {
                Vector3 position = transform.position + (transform.forward * zoom * zoomSpeed);
                position.y = Mathf.Clamp(position.y, minimumHeight, maximumHeight);
                transform.position = position;
            }

            previousPointerPosition = pointerPosition;
            wasDragging = dragging;
        }
    }
}
