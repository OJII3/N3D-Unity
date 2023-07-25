using System;
using Cinemachine;
using TMPro;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace N3D
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class NchanController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private CapsuleCollider collider;
        [SerializeField] private Rigidbody rigidbody;
        [SerializeField] private Camera followCamera;

        public bool grounded = true;
        public LayerMask groundLayers;

        private PlayerInputActions inputActions;

        private readonly float MoveSpeed = 6.0f;
        private float sprintSpeed = 16f;
        private float rotationSmoothTime = 0.12f;
        private float speedChangeRate = 10.0f;
        private float jumpHeight = 1.2f;
        private float gravity = -15.0f;
        private float jumpTriggeredTimeout = 0.50f;
        private float fallTimeout = 0.15f;

        private float groundedOffset = -0.14f;
        private float groundedRadius = 0.8f;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTriggeredTimeoutDelta;
        private float _fallTimeoutDelata;

        // animation Id
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJumpTriggered;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private void Awake()
        {
            inputActions = new PlayerInputActions();
            inputActions.Enable();
            
            AssignAnimationIDs();
        }

        private void FixedUpdate()
        {
            GroundCheck();
            HandleJumpAndGravity();
            Move();
        }

        private void Update()
        {
        }

        private void LateUpdate()
        {
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJumpTriggered = Animator.StringToHash("JumpTriggered");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundCheck()
        {
            Vector3 shperePosition = transform.position;
            grounded = Physics.CheckSphere(shperePosition, groundedRadius, groundLayers,
                QueryTriggerInteraction.Ignore);
            animator.SetBool(_animIDGrounded, grounded);
        }

        private void Move()
        {
            float targetSpeed = inputActions.Player.Sprint.IsInProgress() ? sprintSpeed : MoveSpeed;
            var moveInput = inputActions.Player.Move.ReadValue<Vector2>();

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            if (moveInput == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(rigidbody.velocity.x, 0.0f, rigidbody.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = 1f; // in case that input was analog stick


            // handle speed change
            if (Mathf.Abs(currentHorizontalSpeed - targetSpeed) > speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * speedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * speedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            var inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            if (moveInput != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  followCamera.transform.eulerAngles.y;
                var rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            // move player
            rigidbody.position += targetDirection.normalized * (_speed * Time.deltaTime) +
                                  new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;

            // update animator
            animator.SetFloat(_animIDSpeed, _animationBlend);
            animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }

        private void HandleJumpAndGravity()
        {
            if (grounded)
            {
                _fallTimeoutDelata = fallTimeout; // reset
                animator.SetBool(_animIDJumpTriggered, false);
                animator.SetBool(_animIDFreeFall, false);

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (inputActions.Player.Jump.IsInProgress() && _jumpTriggeredTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    animator.SetBool(_animIDJumpTriggered, true);
                }

                if (_jumpTriggeredTimeoutDelta >= 0.0f)
                {
                    _jumpTriggeredTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTriggeredTimeoutDelta = jumpTriggeredTimeout;

                if (_fallTimeoutDelata >= 0.0f)
                {
                    _fallTimeoutDelata -= Time.deltaTime;
                }
                else
                {
                    animator.SetBool(_animIDFreeFall, true);
                }

                // inputActions.Player.Jump.ReadValue<bool>();
            }
            
            // limit vertical speed
            if (_verticalVelocity < _terminalVelocity) _verticalVelocity += gravity * Time.deltaTime;
        }
    }
}