using StarterAssets;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerClimb : MonoBehaviour
{
    private PlayerFindObject finder;
    private ThirdPersonController controller;
    private StarterAssetsInputs input;
    private Animator animator;
    private Vector3 startPos;
    private Vector3 endPos;
    [SerializeField] private float climbDuration = 1f;
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

        if (isClimbing)
        {
            climbTimer += Time.deltaTime;

            float t = climbTimer / climbDuration;

            transform.position = Vector3.Lerp(startPos, endPos, t);

            if (t >= 1f)
            {
                EndClimb();
            }

        }

    }

    public void TryClimb()
    {
        if (finder.currentTarget == null)
            return;

        if (!finder.currentTarget.TryGetComponent(out Climbable climbable))
            return;

        // Lấy thông tin từ Raycast đầu tiên
        RaycastHit wallHit = finder.currentHit;

        Vector3 hitPoint = wallHit.point;
        Vector3 normal = wallHit.normal;

        // Điểm kiểm tra ở phía trên tường
        Vector3 checkPos = hitPoint + Vector3.up * climbable.maxClimbHeight;

        // Raycast xuống để tìm mặt trên của tường
        if (!Physics.Raycast(checkPos, Vector3.down, out RaycastHit topHit, climbable.maxClimbHeight + 1f))
            return;

        startPos = transform.position;

        // Đẩy người chơi vào trong một chút sau khi leo xong
        endPos = topHit.point + Vector3.up * 0.05f - normal * 0.05f;

        climbTimer = 0f;

        isClimbing = true;

        controller.CanMove = false;

        animator.SetTrigger("ClimbWall");
    }

    public void EndClimb()
    {
        transform.position = endPos;

        controller.CanMove = true;
        isClimbing = false;
    }
}
