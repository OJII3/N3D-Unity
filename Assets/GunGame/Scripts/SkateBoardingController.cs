using System;
using System.IO.Pipes;
using Cinemachine;
using TMPro;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace GunGame
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class SkateBoardingController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody rigidbody;

        public Camera followCamera;
        public bool grounded = true;
        public bool attacking = false;
        public LayerMask groundLayers;

        private PlayerInputActions _inputActions;

        private const float MoveSpeed = 40.0f;
        private const float SprintSpeed = 80f;
        private const float RotationSmoothTime = 0.12f;
        private const float SpeedChangeRate = 8.0f;
        private const float JumpHeight = 1.2f;
        private const float Gravity = -15.0f;

        private float jumpTriggeredTimeout = 0.50f;
        private float fallTimeout = 0.15f;
        private float attackTriggeredTimeout = 0.50f;

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
        private float _attackTriggeredTimeoutDelta;

        // animation Id
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJumpTriggered;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDAttacking;
        private int _animIDAttackTriggered;

        private void Awake()
        {
            _inputActions = new PlayerInputActions();
            _inputActions.Enable();

            AssignAnimationIDs();
        }

        private void OnDestroy()
        {
            _inputActions.Disable();
            _inputActions.Dispose();
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
            _animIDAttacking = Animator.StringToHash("Attacking");
            _animIDAttackTriggered = Animator.StringToHash("AttackTriggered");
        }

        private void GroundCheck()
        {
            var spherePosition = transform.position;
            grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers,
                QueryTriggerInteraction.Ignore);
            animator.SetBool(_animIDGrounded, grounded);
        }

        private void Move()
        {
            var targetSpeed = _inputActions.Player.Sprint.IsInProgress() ? SprintSpeed : MoveSpeed;
            var moveInput = _inputActions.Player.Move.ReadValue<Vector2>();

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            if (moveInput == Vector2.zero) targetSpeed = 0.0f;

            var currentHorizontalSpeed = new Vector3(rigidbody.velocity.x, 0.0f, rigidbody.velocity.z).magnitude;
            var speedOffset = 0.1f;
            var inputMagnitude = 1f; // in case that input was analog stick


            // handle speed change
            if (Mathf.Abs(currentHorizontalSpeed - targetSpeed) > speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            var inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            if (moveInput != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  followCamera.transform.eulerAngles.y;
                var rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);
                transform.rotation =
                    Quaternion.Euler(0.0f, rotation, rotation - _targetRotation);
            }

            var targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
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

                if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;

                if (_inputActions.Player.Jump.IsInProgress() && _jumpTriggeredTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    animator.SetBool(_animIDJumpTriggered, true);
                }

                if (_jumpTriggeredTimeoutDelta >= 0.0f) _jumpTriggeredTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTriggeredTimeoutDelta = jumpTriggeredTimeout;

                if (_fallTimeoutDelata >= 0.0f)
                    _fallTimeoutDelata -= Time.deltaTime;
                else
                    animator.SetBool(_animIDFreeFall, true);

                // inputActions.Player.Jump.ReadValue<bool>();
            }

            // limit vertical speed
            if (_verticalVelocity < _terminalVelocity) _verticalVelocity += Gravity * Time.deltaTime;
        }
    }
}