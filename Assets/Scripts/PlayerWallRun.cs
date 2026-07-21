using StarterAssets;
using UnityEngine;
using static StarterAssets.ThirdPersonController;

[RequireComponent(typeof(Animator))]
public class PlayerWallRun : MonoBehaviour
{
    [Header("Wall Run")]
    [SerializeField] private LayerMask wallRunLayer;
    [SerializeField] private float wallDetectDistance = 0.8f;
    [SerializeField] private float wallOffset = 0.28f;
    [SerializeField] private float wallRunDistanceMultiplier = 1.3f;

    public bool IsWallRunning { get; private set; }

    private bool hasWallRun;
    private bool wallOnRight;
    [SerializeField] private float wallRunCooldown = 1f;
    private float lastWallRunTime;
    private RaycastHit wallHit;

    private ThirdPersonController controller;
    private StarterAssetsInputs input;
    private Animator animator;
    private CharacterController characterController;

    private void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
        input = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (controller.Grounded)
        {
            hasWallRun = false;
            return;
        }

        if (IsWallRunning)
            return;

        CheckWallRun();
    }

    private void CheckWallRun()
    {
        if (controller.State != PlayerState.Fall &&
            controller.State != PlayerState.Jump)
            return;
        if (Time.time < lastWallRunTime + wallRunCooldown)
            return;

        if (hasWallRun)
            return;

        if (!input.sprint)
            return;

        if (input.move.y < 0.5f)
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

    private void StartWallRun(RaycastHit hit, bool rightWall)
    {

        if (controller.IsBusy)
            return;
        lastWallRunTime = Time.time;

        hasWallRun = true;
        wallOnRight = rightWall;
        IsWallRunning = true;

        controller.State = PlayerState.WallRun;
        controller.CanMove = false;
        controller.InputLocked = true;

        Vector3 wallForward = Vector3.Cross(hit.normal, Vector3.up);

        if (Vector3.Dot(wallForward, transform.forward) < 0)
            wallForward = -wallForward;

        transform.rotation = Quaternion.LookRotation(wallForward);

        characterController.enabled = false;

        animator.applyRootMotion = true;

        if (rightWall)
            animator.CrossFade("WallRunRight", 0.1f);
        else
            animator.CrossFade("WallRunLeft", 0.1f);
    }

    // Animation Event
    public void EndWallRun()
    {
        Vector3 pushDirection = wallOnRight ?
                        -transform.right :
                        transform.right;


        transform.position += pushDirection * 0.3f;
        IsWallRunning = false;

        controller.State = PlayerState.Fall;

        controller.CanMove = true;
        controller.InputLocked = false;

        characterController.enabled = true;

        animator.applyRootMotion = false;
    }

    private void OnAnimatorMove()
    {
        if (!IsWallRunning)
            return;

        if (!animator.applyRootMotion)
            return;

        ApplyWallRunRootMotion();
    }

    private void ApplyWallRunRootMotion()
    {
        Vector3 delta = animator.deltaPosition * wallRunDistanceMultiplier;

        transform.position += delta;
        transform.rotation *= animator.deltaRotation;

        SnapToWall();
    }

    private void SnapToWall()
    {
        Vector3 origin = transform.position + Vector3.up;

        Vector3 dir = wallOnRight ?
            transform.right :
            -transform.right;

        if (Physics.Raycast(origin,
                            dir,
                            out RaycastHit hit,
                            wallDetectDistance + 0.5f,
                            wallRunLayer))
        {
            float distance = hit.distance - wallOffset;

            transform.position -= hit.normal * distance;
        }
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 origin = transform.position + Vector3.up;

        Gizmos.DrawLine(origin,
            origin + transform.right * wallDetectDistance);

        Gizmos.DrawLine(origin,
            origin - transform.right * wallDetectDistance);
    }

#endif
}