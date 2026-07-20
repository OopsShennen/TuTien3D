using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class ThirdPersonController : MonoBehaviour
    {
        public enum PlayerState
        {
            Locomotion,
            Cover,
            Climb,
            Slide,
            Jump,
            Roll,
            WallRun,
            JumpObstacle,
            Fall,
            Crouch
        }

        public PlayerState State;
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;
        public bool CanMove = true;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        private bool IsJumpObstacle;
        [SerializeField] private LayerMask jumpObstacleLayer;
        [SerializeField] private float jumpDetectDistance = 1.5f;
        [SerializeField] private float jumpSphereRadius = 0.35f;

        [Header("Player Wall Run")]
        [SerializeField] private LayerMask wallRunLayer;
        [SerializeField] private float wallDetectDistance = 0.8f;

        private RaycastHit wallHit;
        private bool hasWallRun;
        [SerializeField] private float wallRunDistanceMultiplier = 1.3f;

        [SerializeField] private float wallOffset = 0.28f;

        private bool wallOnRight;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Player Crouched")]
        [Tooltip("If the character is crouched or not. Not part of the CharacterController built in crouched check")]
        public bool Crouched;

        [Header("Player Roll")]
        [SerializeField] private float rollCooldown = 0.6f;

        private float lastRollTime = -Mathf.Infinity;

        public bool IsRolling;

        [Header("Player Cover")]
        [Tooltip("If the character is covered or not. Not part of the CharacterController built in covered check")]
        public bool IsCover;

        [Header("Player Slide")]
        [Tooltip("If the character is Slided or not. Not part of the CharacterController built in Slided check")]
        [SerializeField] private float slideCooldown = 0.8f;
        private float lastSlideTime = -Mathf.Infinity;
        public bool IsSlide;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;

        private PlayerInput _playerInput;
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private PlayerCover cover;


        public bool InputLocked;
        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
                return _playerInput.currentControlScheme == "KeyboardMouse";
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

        }

        private void Start()
        {

            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerInput = GetComponent<PlayerInput>();
            cover = GetComponent<PlayerCover>();

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

        }
       
        private void Update()
        {

            _hasAnimator = TryGetComponent(out _animator);

            Crouching();

            GroundedCheck();

            if (State != PlayerState.JumpObstacle &&
                State != PlayerState.WallRun)
            {
                JumpAndGravity();
                CheckJumpObstacle();
                CheckWallRun();
                Move();
            }


            if (_input.slide)
            {
                if (CanSlide())
                {
                    _input.slide = false;
                    StartSlide();
                }
                else
                {
                    // Hủy yêu cầu Slide nếu không hợp lệ
                    _input.slide = false;
                }
            }

            if (_input.roll)
            {
                if (CanRoll())
                {
                    _input.roll = false;
                    StartRoll();
                }
                else
                {
                    _input.roll = false;
                }
            }

        }

        private void LateUpdate()
        {

            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
        }
        public bool IsBusy =>
                InputLocked ||
                IsSlide ||
                IsRolling ||
                State == PlayerState.Climb ||
                State == PlayerState.Cover;

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);
            if (Grounded)
            {
                hasWallRun = false;
            }
            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }
        private bool CanRoll()
        {
            return !IsRolling &&
                   Grounded &&
                   State == PlayerState.Locomotion &&
                   Time.time >= lastRollTime + rollCooldown;
        }
        private void StartRoll()
        {
            if (IsBusy)
                return;

            bool sprintRoll = _input.sprint && _input.move != Vector2.zero;

            State = PlayerState.Roll;
            IsRolling = true;
            CanMove = false;
            _controller.enabled = false;
            _animator.applyRootMotion = true;
            _verticalVelocity = 0f;
            lastRollTime = Time.time;

            if (sprintRoll)
                _animator.SetTrigger("SprintRoll");
            else
                _animator.SetTrigger("StandRoll");
        }
        public void EndRoll()
        {
            State = PlayerState.Locomotion;

            IsRolling = false;
            _controller.enabled = true;
            CanMove = true;

            _animator.applyRootMotion = false;
        }
        private void CheckWallRun()
        {
            if (State != PlayerState.Fall &&
                State != PlayerState.Jump)
                return;

            if (Grounded)
                return;

            if (hasWallRun)
                return;

            if (!_input.sprint)
                return;

            if (_input.move.y < 0.5f)
                return;
            Vector3 origin = transform.position + Vector3.up;

            if (Physics.Raycast(origin,
                transform.right,
                out wallHit,
                wallDetectDistance,
                wallRunLayer))
            {
                StartWallRun(wallHit, true);
                return;
            }

            if (Physics.Raycast(origin,
                -transform.right,
                out wallHit,
                wallDetectDistance,
                wallRunLayer))
            {
                StartWallRun(wallHit, false);
            }
        }
        private bool TryFindJumpObstacle(out Climbable climb)
        {
            climb = null;

            Vector3 origin = transform.position + Vector3.up * 1f;

            if (Physics.SphereCast(
                    origin,
                    jumpSphereRadius,
                    transform.forward,
                    out RaycastHit hit,
                    jumpDetectDistance,
                    jumpObstacleLayer,
                    QueryTriggerInteraction.Ignore))
            {
                climb = hit.collider.GetComponent<Climbable>();

                if (climb != null && climb.isJumpObstacle)
                {
                    Debug.DrawLine(origin, hit.point, Color.yellow, 1f);
                    return true;
                }
            }

            return false;
        }
        private void StartWallRun(RaycastHit hit, bool rightWall)
        {
            hasWallRun = true;

            wallOnRight = rightWall;

            State = PlayerState.WallRun;

            CanMove = false;

            Vector3 wallForward = Vector3.Cross(hit.normal, Vector3.up);

            if (Vector3.Dot(wallForward, transform.forward) < 0)
                wallForward = -wallForward;

            transform.rotation = Quaternion.LookRotation(wallForward);

            _controller.enabled = false;

            _animator.applyRootMotion = true;

            if (rightWall)
            {
                _animator.CrossFade("WallRunRight", 0.1f);
            }
            else
            {
                _animator.CrossFade("WallRunLeft", 0.1f);
            }
        }
        public void EndWallRun()
        {

            State = PlayerState.Fall;

            _controller.enabled = true;

            _animator.applyRootMotion = false;

            CanMove = true;

        }
        private void SnapToWallRun()
        {
            if (State != PlayerState.WallRun)
                return;

            Vector3 origin = transform.position + Vector3.up;

            Vector3 dir = wallOnRight ? transform.right : -transform.right;

            if (Physics.Raycast(origin, dir, out RaycastHit hit,
                wallDetectDistance + 0.5f,
                wallRunLayer))
            {
                // chỉ dịch theo phương vuông góc với tường
                float distance = hit.distance - wallOffset;

                transform.position -= hit.normal * distance;
            }
        }
        private void CheckJumpObstacle()
        {
            if (!Grounded)
                return;

            if (State != PlayerState.Locomotion)
                return;

            if (IsJumpObstacle)
                return;

            if (!_input.sprint || _input.move.y < 0.8f)
                return;

            if (!TryFindJumpObstacle(out Climbable climb))
                return;

            StartJumpObstacle(climb.transform);
        }
        
        private void StartJumpObstacle(Transform obstacle)
        {

            IsJumpObstacle = true;

            State = PlayerState.JumpObstacle;

            CanMove = false;

            _controller.enabled = false;
            _animator.applyRootMotion = true;
            int index = Random.Range(0, 2);

            _animator.SetInteger("JumpBoxIndex", index);
            _animator.SetTrigger("JumpBox");
        }
        public void EndJumpObstacle()
        {
            IsJumpObstacle = false;

            State = PlayerState.Locomotion;

            CanMove = true;
            _controller.enabled = true;
            _animator.applyRootMotion = false;
        }
        private void OnAnimatorMove()
        {
            if (!_animator.applyRootMotion)
                return;

            switch (State)
            {
                case PlayerState.Roll:
                case PlayerState.Slide:
                case PlayerState.JumpObstacle:
                    ApplyGroundRootMotion();
                    break;

                case PlayerState.WallRun:
                    ApplyWallRunRootMotion();
                    break;
            }
        }
        private void ApplyGroundRootMotion()
        {
            Vector3 delta = _animator.deltaPosition;

            // Chỉ lấy rotation Y
            Quaternion deltaRot = _animator.deltaRotation;
            Vector3 euler = deltaRot.eulerAngles;
            euler.x = 0;
            euler.z = 0;

            transform.rotation *= Quaternion.Euler(euler);

            // Chỉ di chuyển ngang
            transform.position += new Vector3(delta.x, 0f, delta.z);

            // Bám mặt đất
            if (Physics.Raycast(transform.position + Vector3.up,
                                Vector3.down,
                                out RaycastHit hit,
                                3f,
                                GroundLayers))
            {
                transform.position = new Vector3(
                    transform.position.x,
                    hit.point.y,
                    transform.position.z);
            }
        }
        private void ApplyWallRunRootMotion()
        {
            Vector3 delta = _animator.deltaPosition * wallRunDistanceMultiplier;

            transform.position += delta;
            transform.rotation *= _animator.deltaRotation;

            SnapToWallRun();
        }
        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if (!CanMove)
                return;
           
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            if (Crouched)
            {
                targetSpeed = MoveSpeed * 0.5f;
            }
            else if (_input.sprint)
            {
                targetSpeed = SprintSpeed;
            }
            else
            {
                targetSpeed = MoveSpeed;
            }
            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;



            Vector3 forward = _mainCamera.transform.forward;
            Vector3 right = _mainCamera.transform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection;

            if (!IsCover)
            {
                moveDirection =
                    forward * _input.move.y +
                    right * _input.move.x;
            }
            else
            {
                moveDirection = cover.WallDirection * _input.move.x;
            }


            // move the player
            _controller.Move(moveDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (!IsCover) 
           {
            if (_input.move != Vector2.zero)
            {
                if (moveDirection.sqrMagnitude > 0.01f)
                {
                    if (_input.sprint && _input.move.y >= 0)
                    {
                        _targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;

                        float rotation = Mathf.SmoothDampAngle(
                            transform.eulerAngles.y,
                            _targetRotation,
                            ref _rotationVelocity,
                            RotationSmoothTime);

                        transform.rotation = Quaternion.Euler(0, rotation, 0);
                    }
                    else
                    {
                        if (Crouched && _input.move.y < 0)
                        {
                            // Ngồi + S -> quay 180 độ để dùng animation đi tới
                            _targetRotation = _mainCamera.transform.eulerAngles.y + 180f;
                        }
                     
                        else
                        {
                            if (_input.move.y < 0)
                            {
                                // Đi lùi: chỉ xoay theo camera
                                _targetRotation = _mainCamera.transform.eulerAngles.y;
                            }
                            else
                            {
                                // W, A, D, W+A, W+D...
                                _targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                            }
                        }

                        float rotation = Mathf.SmoothDampAngle(
                            transform.eulerAngles.y,
                            _targetRotation,
                            ref _rotationVelocity,
                            RotationSmoothTime);

                        transform.rotation = Quaternion.Euler(0, rotation, 0);
                    }
                }
            }
           }

            if (_hasAnimator)
            {
                float velX = 0f;
                float velY = 0f;
                if (IsCover)
                {
                    _input.sprint = false;

                    if (_input.move.x > 0.1f)
                        _animator.SetBool("CoverLeft", false);

                    if (_input.move.x < -0.1f)
                        _animator.SetBool("CoverLeft", true);

                    _animator.SetFloat("VelocityX", _input.move.x, 0.1f, Time.deltaTime);
                }
                if (Crouched)
                {
                    velX = _input.move.x;

                    if (_input.move != Vector2.zero)
                    {
                        velY = 0.5f;      // luôn dùng animation Crouch Walk Forward
                    }
                }
                else if (_input.move.y < -0.1f)
                {
                    velX = 0f;
                    velY = _input.sprint ? -1f : -0.5f;
                }
                // Sprint
                else if (_input.sprint && _input.move != Vector2.zero)
                {
                    velX = 0f;
                    velY = 5f;
                }
                // Walk
                else
                {
                    velX = _input.move.x;

                    if (_input.move.y > 0)
                        velY = 1.5f;

                    if (Mathf.Abs(_input.move.x) > 0.1f && Mathf.Abs(_input.move.y) < 0.1f)
                        velY = 0f;
                }

                _animator.SetFloat("VelocityX", velX, 0.1f, Time.deltaTime);
                _animator.SetFloat("VelocityY", velY, 0.1f, Time.deltaTime);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                if (State != PlayerState.Climb &&
                       State != PlayerState.Slide &&
                       State != PlayerState.Cover &&
                       State != PlayerState.Roll &&
                       State != PlayerState.JumpObstacle)
                {
                    State = Crouched
                        ? PlayerState.Crouch
                        : PlayerState.Locomotion;
                }
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0 && State == PlayerState.Locomotion)
                {
                    State = PlayerState.Jump;
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {

                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        State = PlayerState.Fall;
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }
        private void StartSlide()
        {
            if (IsBusy)
                return;
            _input.ClearInput();
            State = PlayerState.Slide;

            IsSlide = true;
            CanMove = false;
            _animator.applyRootMotion = true;
            lastSlideTime = Time.time;
            _animator.SetTrigger("Slide");

        }
        public void EndSlide()
        {
            State = PlayerState.Locomotion;

            IsSlide = false;
            _input.slide = false;
            _animator.applyRootMotion = false;
            CanMove = true;
        }
        private bool CanSlide()
        {
            return !IsSlide &&
                   Grounded &&
                   Time.time >= lastSlideTime + slideCooldown &&
                   State == PlayerState.Locomotion &&
                   _input.sprint &&
                   _input.move != Vector2.zero;
        }
        private void Crouching()
        {

            if (!_hasAnimator)
                return;

            if (Crouched && _input.sprint)
            {
                
                Crouched = false;
                _input.crouch = false;

                _animator.SetFloat("CrouchValue", 0);
                _animator.SetTrigger("CrouchToSprint");

            }

            if (_input.crouch && !IsBusy)
            {

                State = PlayerState.Crouch;
                Crouched = true;
                _animator.SetFloat("CrouchValue", 1);
            }
            if (!_input.crouch && !_input.sprint && State == PlayerState.Crouch)
            {
                _input.ClearInput();
                State = PlayerState.Locomotion;
                Crouched = false;
                _animator.SetFloat("CrouchValue", 0);
            }

        }
       
        public void EndCrouch()
        {
            Crouched = false;
            CanMove = true;

        }
        public void LockMovement()
        {

            CanMove = false;
        }
        public void UnlockMovement()
        {
            Crouched = true;
            CanMove = true;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}
