using UnityEngine;

public class MouseOrbit : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float sensitivity = 5f;
    
    [Header("Detection")]
    public LayerMask detectionLayer;

    private bool _isDragging = false;
    private Camera _mainCam;
    private Vector3 _modelCenter;

    void Start()
    {
        _mainCam = Camera.main;
    }

    void Update()
    {
        // 1. Start dragging only if we click ON the model
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.transform.IsChildOf(this.transform) || hit.transform == this.transform)
                {
                    _isDragging = true;
                    _modelCenter = CalculateBoundsCenter();
                }
            }
        }

        // 2. Stop dragging when mouse is released
        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }

        // 3. Rotate around the calculated center based on Mouse movement
        if (_isDragging)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            transform.RotateAround(_modelCenter, Vector3.up, -mouseX);
            transform.RotateAround(_modelCenter, _mainCam.transform.right, mouseY);
        }
    }

    public void RecenterPivot(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 offset = transform.position - bounds.center;
        model.transform.position += offset;
        
        Debug.Log("[ARK] Model re-centered for perfect rotation.");
    }

    private Vector3 CalculateBoundsCenter()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0) return transform.position;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        return bounds.center;
    }
}