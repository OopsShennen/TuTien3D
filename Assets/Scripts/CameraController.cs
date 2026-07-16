using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;

    [SerializeField] private PlayerCover cover;
    [SerializeField] private ThirdPersonController controller;


    [Header("Camera")]
    [SerializeField] private float normalFOV = 40f;
    [SerializeField] private float aimFOV = 30f;

    [SerializeField] private Vector3 normalShoulder = new Vector3(0.4f, 1.4f, 0);
    [SerializeField] private Vector3 rightPeekShoulder = new Vector3(0.7f, 1.4f, 0);

    [SerializeField] private Vector3 leftPeekShoulder = new Vector3(-0.7f, 1.4f, 0);
    [SerializeField] private float normalDistance = 3.5f;
    [SerializeField] private float aimDistance = 2.3f;

    [SerializeField] private float smooth = 10f;


    private void Update()
    {
        bool aiming =
             cover.CanPeek &&
             cover.State == PlayerCover.CoverState.Peeking;

        float targetFOV = aiming ? aimFOV : normalFOV;

        Vector3 targetShoulder = normalShoulder;

        if (aiming)
        {
            targetShoulder = cover.CoverLeft
                 ? leftPeekShoulder
                 : rightPeekShoulder;
        }

        float targetDistance = aiming ? aimDistance : normalDistance;


        cinemachineCamera.Lens.FieldOfView =
            Mathf.Lerp(
                cinemachineCamera.Lens.FieldOfView,
                targetFOV,
                smooth * Time.deltaTime
            );


        thirdPersonFollow.ShoulderOffset =
            Vector3.Lerp(
                thirdPersonFollow.ShoulderOffset,
                targetShoulder,
                smooth * Time.deltaTime
            );


        thirdPersonFollow.CameraDistance =
            Mathf.Lerp(
                thirdPersonFollow.CameraDistance,
                targetDistance,
                smooth * Time.deltaTime
            );
    }
}
