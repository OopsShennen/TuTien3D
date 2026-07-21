using StarterAssets;
using UnityEngine;
using static StarterAssets.ThirdPersonController;

[RequireComponent(typeof(Animator))]
public class PlayerJumpObstacle : MonoBehaviour
{
    [Header("Jump Obstacle")]
    [SerializeField] private LayerMask jumpObstacleLayer;
    [SerializeField] private float jumpDetectDistance = 1.5f;
    [SerializeField] private float jumpSphereRadius = 0.35f;

    public bool IsJumpObstacle { get; private set; }

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
        if (IsJumpObstacle)
            return;

        CheckJumpObstacle();
    }

    private void CheckJumpObstacle()
    {
        if (!controller.Grounded)
            return;

        if (controller.State != PlayerState.Locomotion)
            return;

        if (!input.sprint)
            return;

        if (input.move.y < 0.8f)
            return;

        if (!TryFindJumpObstacle(out Climbable climb))
            return;

        StartJumpObstacle(climb.transform);
    }

    private bool TryFindJumpObstacle(out Climbable climb)
    {
        climb = null;

        Vector3 origin = transform.position + Vector3.up;

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

    private void StartJumpObstacle(Transform obstacle)
    {
        if (controller.IsBusy)
            return;

        input.ClearInput();

        IsJumpObstacle = true;

        controller.State = PlayerState.JumpObstacle;
        controller.CanMove = false;
        controller.InputLocked = true;

        characterController.enabled = false;

        animator.applyRootMotion = true;

        animator.SetInteger("JumpBoxIndex", Random.Range(0, 2));
        animator.SetTrigger("JumpBox");
    }

    // Animation Event
    public void EndJumpObstacle()
    {
        IsJumpObstacle = false;

        controller.State = PlayerState.Locomotion;

        controller.CanMove = true;
        controller.InputLocked = false;

        characterController.enabled = true;

        animator.applyRootMotion = false;
    }

    private void OnAnimatorMove()
    {
        if (!IsJumpObstacle)
            return;

        if (!animator.applyRootMotion)
            return;

        ApplyGroundRootMotion();
    }

    private void ApplyGroundRootMotion()
    {
        Vector3 delta = animator.deltaPosition;

        Quaternion deltaRot = animator.deltaRotation;

        Vector3 euler = deltaRot.eulerAngles;
        euler.x = 0f;
        euler.z = 0f;

        transform.rotation *= Quaternion.Euler(euler);

        transform.position += new Vector3(delta.x, 0f, delta.z);

        if (Physics.Raycast(transform.position + Vector3.up,
                            Vector3.down,
                            out RaycastHit hit,
                            3f,
                            controller.GroundLayers))
        {
            transform.position = new Vector3(
                transform.position.x,
                hit.point.y,
                transform.position.z);
        }
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 origin = transform.position + Vector3.up;

        Gizmos.DrawWireSphere(origin, jumpSphereRadius);

        Gizmos.DrawLine(
            origin,
            origin + transform.forward * jumpDetectDistance);
    }

#endif
}