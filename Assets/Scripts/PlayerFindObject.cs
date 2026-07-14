using UnityEngine;

public class PlayerFindObject : MonoBehaviour
{
    public enum InteractType
    {
        None,
        Cover,
        Climb
    }

    public InteractType currentType;
    [Header("Find Settings")]
    public LayerMask targetLayer;
    public float distance = 0.5f;
    private CharacterController _characterController;
    public Transform currentTarget;
    public RaycastHit currentHit;
    public bool hasTarget;
    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }
    private void Update()
    {
        FindObject();
    }

    void FindObject()
    {

        currentTarget = null;
        currentType = InteractType.None;
        hasTarget = false;

        Vector3 origin = transform.position + Vector3.up * (_characterController.height * 0.5f);

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, distance, targetLayer))
        {
            hasTarget = true;
            currentHit = hit;
            currentTarget = hit.transform;

            if (hit.collider.TryGetComponent(out Cover _))
            {
                currentType = InteractType.Cover;
            }
            else if (hit.collider.TryGetComponent(out Climbable _))
            {
                currentType = InteractType.Climb;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 origin = transform.position + Vector3.up;
        Gizmos.DrawRay(origin, transform.forward * distance);
    }
}
