using UnityEngine;

public class PlayerActivite : MonoBehaviour
{
    private PlayerFindObject findObject;

    private void Awake()
    {
        findObject = GetComponent<PlayerFindObject>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Activate();
        }
    }

    void Activate()
    {
        if (findObject.currentTarget == null)
            return;

        // Sau này xử lý tại đây
    }

}
