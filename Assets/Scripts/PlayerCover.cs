using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.Windows;
using static StarterAssets.ThirdPersonController;

public class PlayerCover : MonoBehaviour
{
    public enum CoverState
    {
        None,
        Entering,
        Idle,
        Moving,
        Peeking,
        Exiting
    }

    public CoverState State;

    private PlayerFindObject finder;
    private ThirdPersonController controller;
    private Animator animator;
    private GameObject _mainCamera;
    private StarterAssetsInputs input;
    private bool isTransition;
    public bool CanPeek;
    public bool CoverLeft { get; private set; }
    
    [SerializeField] private float coverDistance = 0.2f;
    [SerializeField] private float snapSpeed = 1f;
    [SerializeField] private float edgeCheckDistance = 0.4f;
    [SerializeField] private float edgeRayLength = 0.6f;
    public Vector3 WallNormal { get; private set; }
    public Vector3 WallDirection { get; private set; }
    void Start()
    {
        finder = GetComponent<PlayerFindObject>();
        controller = GetComponent<ThirdPersonController>();
        animator = GetComponent<Animator>();
        input = GetComponent<StarterAssetsInputs>();
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    
    private void Update()
    {
        if (!controller.IsCover)
            return;

        UpdateWall();
        SnapToWall();
        CheckEdge();
        if (input.move.x < -0.1f)
        {
            CoverLeft = true;
            animator.SetBool("CoverLeft", input.move.x < -0.1f);
        }
        else if (input.move.x > 0.1f)
        {
            CoverLeft = false;
            animator.SetBool("CoverLeft", false);
        }

        if (!CanPeek)
        {
            animator.SetBool("Peek", false);
            return;
        }

        bool peeking = input.aim;

        controller.CanMove = !peeking;
        State = peeking ? CoverState.Peeking : CoverState.Idle;
        animator.SetBool("Peek", peeking);

    }
    public void EnterCover()
    {
        if (controller.IsBusy || isTransition || !finder.hasTarget)
            return;

        isTransition = true;
        State = CoverState.Entering;

        StartCoroutine(MoveToCover());

    }
    public void ExitCover()
    {
        controller.CanMove = false;
        State = CoverState.Exiting;
        animator.SetTrigger("ExitCover");
    }
    public void FinishEnterCover()
    {
        controller.State = PlayerState.Cover;

        input.move = Vector2.zero;

        controller.IsCover = true;

        animator.SetBool("Cover", true);

        controller.CanMove = true;

        isTransition = false;

        State = CoverState.Idle;

        CanPeek = true;
        input.canAim = true;
    }
    public void FinishExitCover()
    {
        controller.State = PlayerState.Locomotion;
        input.move = Vector2.zero;

        controller.IsCover = false;

        animator.SetBool("Cover", false);

        controller.CanMove = true;

        isTransition = false;

        State = CoverState.None;

        CanPeek = false;
        input.canAim = false;
        input.aim = false;
    }
    public void ExitCoverInstant()
    {
        controller.IsCover = false;
        CanPeek = false;
        input.canAim = false;
        input.aim = false;
        State = CoverState.None;
        animator.SetBool("Cover", false);

        controller.CanMove = true;

        animator.ResetTrigger("StandToCover");
        animator.ResetTrigger("ExitCover");
        input.move = Vector2.zero;
        animator.SetFloat("VelocityX", 0);
        animator.SetFloat("VelocityY", 0);
    }
    IEnumerator MoveToCover()
    {
        controller.CanMove = false;
        input.move = Vector2.zero;
        float coverDistance = 0.2f;

        WallNormal = finder.currentHit.normal;

        WallDirection = Vector3.Cross(Vector3.up, WallNormal).normalized;

        Vector3 targetPos =
        finder.currentHit.point + finder.currentHit.normal * coverDistance;
        targetPos.y = transform.position.y;
        Quaternion targetRot = Quaternion.LookRotation(-finder.currentHit.normal);
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                snapSpeed * Time.deltaTime);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                720f * Time.deltaTime);

            yield return null;
        }
        animator.SetTrigger("StandToCover");
    }
    private void UpdateWall()
    {
        Vector3 origin = transform.position + Vector3.up;

        if (Physics.Raycast(origin,
                            -WallNormal,
                            out RaycastHit hit,
                            1f,
                            finder.targetLayer))
        {
            WallNormal = hit.normal;
            WallDirection = Vector3.Cross(Vector3.up, WallNormal).normalized;

            if (Vector3.Dot(WallDirection, _mainCamera.transform.right) < 0)
            {
                WallDirection = -WallDirection;
            }
        }
    }
    private void SnapToWall()
    {
        if (!controller.IsCover)
            return;
       
        Vector3 origin = transform.position + Vector3.up;

        if (Physics.Raycast(origin,
                            -WallNormal,
                            out RaycastHit hit,
                            1f,
                            finder.targetLayer))
        {
            // cập nhật normal nếu tường đổi hướng
            WallNormal = hit.normal;

            WallDirection = Vector3.Cross(Vector3.up, WallNormal).normalized;

            if (Vector3.Dot(WallDirection, _mainCamera.transform.right) < 0)
                WallDirection = -WallDirection;
            float distance = Vector3.Dot(
                 transform.position - hit.point,
                 hit.normal);

            float offset = distance - coverDistance;

            transform.position -= hit.normal * offset;
        }
    }

    private void CheckEdge()
    {
        float horizontal = input.move.x;

        if (Mathf.Abs(horizontal) < 0.1f)
            return;

        Vector3 dir = horizontal > 0 ? WallDirection : -WallDirection;

        if (!CheckWall(dir))
        {
            ExitCoverInstant();
        }
    }
    private bool CheckWall(Vector3 side)
    {
        Vector3 origin =
            transform.position +
            Vector3.up +
            side * edgeCheckDistance;

        return Physics.Raycast(
            origin,
            -WallNormal,
            edgeRayLength,
            finder.targetLayer);
    }

}
