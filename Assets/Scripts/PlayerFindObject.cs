using UnityEngine;

public class PlayerFindObject : MonoBehaviour
{
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
        InvokeRepeating(nameof(FindObject), 0f, 0.05f);
    }

    void FindObject()
    {
        currentTarget = null;

        Vector3 origin = transform.position + Vector3.up * (_characterController.height * 0.5f);

        hasTarget = Physics.Raycast(origin, transform.forward, out RaycastHit hit, distance, targetLayer);
        if (hasTarget)
        {
            currentHit = hit;
            currentTarget = hit.transform;
        }
        else
        {
            currentTarget = null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 origin = transform.position + Vector3.up;
        Gizmos.DrawRay(origin, transform.forward * distance);
    }
}
