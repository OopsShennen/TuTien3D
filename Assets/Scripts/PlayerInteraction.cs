using StarterAssets;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private StarterAssetsInputs input;
    private PlayerFindObject finder;
    private PlayerCover cover;
    private PlayerClimb climb;
    private ThirdPersonController controller;

    private void Start()
    {
        input = GetComponent<StarterAssetsInputs>();
        finder = GetComponent<PlayerFindObject>();
        cover = GetComponent<PlayerCover>();
        climb = GetComponent<PlayerClimb>();
        controller = GetComponent<ThirdPersonController>();
    }

    private void Update()
    {
        if (!input.interact)
            return;

        if (controller.IsCover)
        {
            cover.ExitCover();
        }
        else
        {
            switch (finder.currentType)
            {
                case PlayerFindObject.InteractType.Cover:
                    cover.EnterCover();
                    break;

                case PlayerFindObject.InteractType.Climb:
                    climb.TryClimb();
                    break;
            }
        }

        input.interact = false;
    }
}
