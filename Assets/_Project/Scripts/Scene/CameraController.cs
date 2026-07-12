using NeighborhoodManager.Core;
using NeighborhoodManager.Models;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NeighborhoodManager.Scene
{
    public enum CameraDragButton { Middle, Right }

    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private CameraDragButton dragButton = CameraDragButton.Middle;
        [Min(0f)] [SerializeField] private float dragSpeed = 0.02f;
        [Min(0f)] [SerializeField] private float zoomSpeed = 0.05f;

        [Header("Movement Limits")]
        [SerializeField] private Vector2 xLimits = new Vector2(-18f, 18f);
        [SerializeField] private Vector2 zLimits = new Vector2(-28f, 12f);

        [Header("Perspective Zoom")]
        [SerializeField] private float minimumFieldOfView = 35f;
        [SerializeField] private float maximumFieldOfView = 70f;

        [Header("Scene References")]
        [SerializeField] private GameRoot gameRoot;

        private Camera controlledCamera;
        private InputAction pointAction;
        private InputAction dragAction;
        private InputAction zoomAction;
        private Vector2 previousPointerPosition;
        private bool wasDragging;
        private bool dragBlocked;

        public bool HasRequiredReferences => gameRoot != null && TryGetComponent(out Camera _);

        public void Configure(GameRoot root)
        {
            gameRoot = root;
            controlledCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            controlledCamera = GetComponent<Camera>();
            pointAction = new InputAction("Point", binding: "<Pointer>/position");
            string dragBinding = dragButton == CameraDragButton.Right
                ? "<Mouse>/rightButton" : "<Mouse>/middleButton";
            dragAction = new InputAction("CameraDrag", InputActionType.Button, dragBinding);
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
            pointAction = null;
            dragAction = null;
            zoomAction = null;
            wasDragging = false;
            dragBlocked = false;
        }

        private void Update()
        {
            Vector2 pointerPosition = pointAction.ReadValue<Vector2>();
            bool dragging = dragAction.IsPressed();
            bool blockedNow = IsInputBlocked();

            if (dragging && !wasDragging)
            {
                dragBlocked = blockedNow;
            }
            else if (dragging && blockedNow)
            {
                dragBlocked = true;
            }

            if (dragging && wasDragging && !dragBlocked)
            {
                Vector2 delta = pointerPosition - previousPointerPosition;
                Vector3 groundRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                Vector3 groundForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 movement = (-groundRight * delta.x - groundForward * delta.y) * dragSpeed;
                transform.position = ClampPosition(transform.position + movement, xLimits, zLimits);
            }

            if (!dragging)
            {
                dragBlocked = false;
            }

            if (!blockedNow)
            {
                float zoom = zoomAction.ReadValue<float>();
                if (Mathf.Abs(zoom) > 0.01f)
                {
                    controlledCamera.fieldOfView = ClampZoom(
                        controlledCamera.fieldOfView - zoom * zoomSpeed,
                        minimumFieldOfView, maximumFieldOfView);
                }
            }

            previousPointerPosition = pointerPosition;
            wasDragging = dragging;
        }

        private bool IsInputBlocked()
        {
            bool pointerOverUi = UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            GamePhase phase = gameRoot?.Session?.State?.Phase ?? GamePhase.None;
            return ShouldBlockInput(pointerOverUi, phase, false);
        }

        public static Vector3 ClampPosition(Vector3 position, Vector2 horizontalLimits, Vector2 depthLimits)
        {
            float minX = Mathf.Min(horizontalLimits.x, horizontalLimits.y);
            float maxX = Mathf.Max(horizontalLimits.x, horizontalLimits.y);
            float minZ = Mathf.Min(depthLimits.x, depthLimits.y);
            float maxZ = Mathf.Max(depthLimits.x, depthLimits.y);
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.z = Mathf.Clamp(position.z, minZ, maxZ);
            return position;
        }

        public static float ClampZoom(float value, float minimum, float maximum)
        {
            float safeMinimum = Mathf.Min(minimum, maximum);
            float safeMaximum = Mathf.Max(minimum, maximum);
            return Mathf.Clamp(value, safeMinimum, safeMaximum);
        }

        public static bool ShouldBlockInput(bool pointerOverUi, GamePhase phase, bool overlayVisible)
        {
            return pointerOverUi || overlayVisible || phase != GamePhase.Playing;
        }

        private void OnValidate()
        {
            dragSpeed = Mathf.Max(0f, dragSpeed);
            zoomSpeed = Mathf.Max(0f, zoomSpeed);
        }
    }
}
