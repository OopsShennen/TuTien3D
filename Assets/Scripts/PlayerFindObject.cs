using UnityEngine;

public class PlayerFindObject : MonoBehaviour
{
    public enum InteractType
    {
        None,
        Cover,
        Climb,
        JumpBox
    }

    public InteractType currentType;
    [Header("Find Settings")]
    public LayerMask targetLayer;
    public float distance = 1f;
    [SerializeField]
    private float rayHeight = 1.3f;
    public Transform currentTarget;
    public RaycastHit currentHit;
    public bool hasTarget;
    private void Start()
    {
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

        Vector3 origin = transform.position + Vector3.up * rayHeight;

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
            else if (hit.collider.TryGetComponent(out JumpBox _))
            {
                currentType = InteractType.JumpBox;
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
