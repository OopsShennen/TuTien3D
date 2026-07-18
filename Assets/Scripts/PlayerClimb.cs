using StarterAssets;
using UnityEngine;

public class PlayerClimb : MonoBehaviour
{
    private PlayerFindObject finder;
    private ThirdPersonController controller;
    private StarterAssetsInputs input;
    private Climbable climb;
    private Animator animator;
    private Vector3 startPos;
    private Vector3 endPos;
    [SerializeField] private float lowClimbDuration = 0.8f;
    [SerializeField] private float highClimbDuration = 1.3f;

    private float climbDuration;
    private float climbTimer;
    private bool isClimbing;

    private void Awake()
    {
        finder = GetComponent<PlayerFindObject>();
        animator = GetComponent<Animator>();
        input = GetComponent<StarterAssetsInputs>();
        controller = GetComponent<ThirdPersonController>();
    }
   
    private void Update()
    {
        if (!isClimbing)
            return;

        climbTimer += Time.deltaTime;

        float t = Mathf.Clamp01(climbTimer / climbDuration);

        // Làm chuyển động mượt hơn
        t = Mathf.SmoothStep(0f, 1f, t);

        transform.position = Vector3.Lerp(startPos, endPos, t);

        if (t >= 1f)
        {
            EndClimb();
        }
    }

    public void TryClimb()
    {

        if (controller.IsBusy)
            return;

        if (finder.currentTarget == null)
            return;

        if (!finder.currentTarget.TryGetComponent(out Climbable climbable))
            return;

        if (climbable.isHighClimb && !input.sprint)
        {
            return;
        }
        if (climb.isJumpObstacle)
            return;
        // Lấy thông tin từ Raycast đầu tiên
        Vector3 hitPoint = finder.currentHit.point;
        Vector3 normal = finder.currentHit.normal;

        // Điểm kiểm tra ở phía trên tường
        Vector3 checkPos = hitPoint + Vector3.up * climbable.maxClimbHeight;

        // Raycast xuống để tìm mặt trên của tường
        if (!Physics.Raycast(checkPos, Vector3.down, out RaycastHit topHit, climbable.maxClimbHeight + 1f))
            return;
        controller.InputLocked = true;
        controller.State = ThirdPersonController.PlayerState.Climb;

        // Đẩy người chơi vào trong một chút sau khi leo xong
        endPos = topHit.point + Vector3.up * 0.05f - normal * 0.05f;

        startPos = transform.position;
        climbTimer = 0f;
        isClimbing = true;
        controller.CanMove = false;

        if (climbable.isHighClimb)
        {
            climbDuration = highClimbDuration;
            animator.SetTrigger("ClimbHigh");
        }
        else
        {
            climbDuration = lowClimbDuration;
            animator.SetTrigger("ClimbLow");
        }

    }

    public void EndClimb()
    {
        controller.State = ThirdPersonController.PlayerState.Locomotion;

        controller.CanMove = true;
        controller.InputLocked = false;

        isClimbing = false;
    }
}
