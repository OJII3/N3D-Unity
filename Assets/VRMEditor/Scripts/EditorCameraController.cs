using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRMEditor
{
    public class EditorCameraController : MonoBehaviour
    {
        [SerializeField] private float initialFOV = 40f;
        [SerializeField] private CinemachineFreeLook freeLookCamera;
        private PlayerInputActions playerInputActions;

        private void Awake()
        {
            playerInputActions = new PlayerInputActions();
            playerInputActions.Enable();
            freeLookCamera.m_Lens.FieldOfView = initialFOV;

            // playerInputActions.Player.Zoom.performed += OnZoom;
        }

        private void OnZoom(InputAction.CallbackContext ctx)
        {
            freeLookCamera.m_Lens.FieldOfView += ctx.ReadValue<float>() * 0.1f;
        }
    }
}