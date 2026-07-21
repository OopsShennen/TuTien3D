using StarterAssets;
using UnityEngine;
using static StarterAssets.ThirdPersonController;

[RequireComponent(typeof(Animator))]
public class PlayerRoll : MonoBehaviour
{
    [Header("Roll")]
    [SerializeField] private float rollCooldown = 0.6f;

    private float lastRollTime = -Mathf.Infinity;

    public bool IsRolling { get; private set; }

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
        if (!input.roll)
            return;

        input.roll = false;

        if (!CanRoll())
            return;

        StartRoll();
    }

    private bool CanRoll()
    {
        return !IsRolling &&
               controller.Grounded &&
               controller.State == PlayerState.Locomotion &&
               Time.time >= lastRollTime + rollCooldown;
    }

    private void StartRoll()
    {
        if (controller.IsBusy)
            return;

        bool sprintRoll =
            input.sprint &&
            input.move != Vector2.zero;

        IsRolling = true;

        controller.State = PlayerState.Roll;
        controller.CanMove = false;
        controller.InputLocked = true;

        characterController.enabled = false;

        animator.applyRootMotion = true;

        lastRollTime = Time.time;

        if (sprintRoll)
            animator.SetTrigger("SprintRoll");
        else
            animator.SetTrigger("StandRoll");

        input.ClearInput();
    }

    // Animation Event
    public void EndRoll()
    {
        IsRolling = false;

        controller.State = PlayerState.Locomotion;

        controller.CanMove = true;
        controller.InputLocked = false;

        characterController.enabled = true;

        animator.applyRootMotion = false;
    }

    private void OnAnimatorMove()
    {
        if (!IsRolling)
            return;

        if (!animator.applyRootMotion)
            return;

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
}