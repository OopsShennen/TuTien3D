using StarterAssets;
using UnityEngine;
using static StarterAssets.ThirdPersonController;

[RequireComponent(typeof(Animator))]
public class PlayerSlide : MonoBehaviour
{
    [Header("Slide")]
    [SerializeField] private float cooldown = 0.8f;

    private float lastSlideTime = -Mathf.Infinity;

    public bool IsSliding { get; private set; }

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
        if (!input.slide)
            return;

        input.slide = false;

        if (!CanSlide())
            return;

        StartSlide();
    }

    private bool CanSlide()
    {
        return
            !IsSliding &&
            controller.Grounded &&
            controller.State == PlayerState.Locomotion &&
            input.sprint &&
            input.move != Vector2.zero &&
            Time.time >= lastSlideTime + cooldown;
    }

    private void StartSlide()
    {
        if (controller.IsBusy)
            return;

        input.ClearInput();

        IsSliding = true;

        controller.State = PlayerState.Slide;
        controller.CanMove = false;
        controller.InputLocked = true;

        characterController.enabled = false;

        animator.applyRootMotion = true;

        lastSlideTime = Time.time;

        animator.SetTrigger("Slide");
    }

    // Animation Event
    public void EndSlide()
    {
        IsSliding = false;

        controller.State = PlayerState.Locomotion;

        controller.CanMove = true;
        controller.InputLocked = false;

        characterController.enabled = true;

        animator.applyRootMotion = false;
    }

    private void OnAnimatorMove()
    {
        if (!IsSliding)
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
}