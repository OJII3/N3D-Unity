using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;


namespace GunGame
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float initialFOV = 40f;
        [SerializeField] private CinemachineFreeLook freeLookCamera;
        private PlayerInputActions playerInputActions;

        private void Awake()
        {
            playerInputActions = new PlayerInputActions();
            playerInputActions.Enable();

            // playerInputActions.Player.Zoom.performed += OnZoom;
        }

        private void OnDestroy()
        {
            playerInputActions.Disable();
            playerInputActions.Dispose();
        }

        private void OnZoom(InputAction.CallbackContext ctx)
        {
            freeLookCamera.m_Lens.FieldOfView += ctx.ReadValue<float>();
        }

        private void LateUpdate()
        {
            freeLookCamera.m_XAxis.Value += playerInputActions.Player.Look.ReadValue<Vector2>().x * 0.5f;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}