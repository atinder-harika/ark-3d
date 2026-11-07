using System.Collections.Generic;
using UnityEngine;

public class ObjectBehavior : MonoBehaviour
{
    [System.Serializable]
    public class PartData
    {
        public Transform part;
        public Vector3 originalLocalPosition;
        public Quaternion originalLocalRotation;
        public Vector3 explodeDirection;
    }

    public float explodeDistance = 4f;
    public float moveSpeed = 4f;

    private List<PartData> parts = new List<PartData>();
    private bool isExploded = false;

    void Start()
    {
        if (transform.childCount > 0)
        {
            SetupModel(this.transform);
        }
    }

    public void SetupModel(Transform modelRoot)
    {
        parts.Clear();
        isExploded = false;

        // 1. Find all actual parts (ignores empty wrappers by only grabbing objects with Renderers)
        MeshRenderer[] renderers = modelRoot.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0) return;

        // 2. Calculate the true center of the entire model using bounding boxes
        Vector3 sum = Vector3.zero;
        foreach (MeshRenderer r in renderers)
        {
            sum += r.bounds.center;
        }
        Vector3 worldCenter = sum / renderers.Length;

        // 3. Calculate explosion direction for each individual mesh
        foreach (MeshRenderer r in renderers)
        {
            Transform child = r.transform;
            PartData data = new PartData();
            data.part = child;
            data.originalLocalPosition = child.localPosition;
            data.originalLocalRotation = child.localRotation;

            string name = child.name.ToLower();
            
            // Calculate outward direction from the center of the ship
            Vector3 worldDir = (r.bounds.center - worldCenter).normalized;

            if (name.Contains("cone"))
            {
                worldDir = modelRoot.up;
            }
            else if (name.Contains("cylinder") || name.Contains("body"))
            {
                worldDir = -modelRoot.up * 0.5f;
            }
            else if (name.Contains("tail") || name.Contains("fin"))
            {
                // Fins explode outward horizontally
                Vector3 localDir = modelRoot.InverseTransformDirection(worldDir);
                localDir.y = 0f;
                if (localDir == Vector3.zero) localDir = Vector3.right;
                worldDir = modelRoot.TransformDirection(localDir).normalized;
            }
            else
            {
                if (worldDir == Vector3.zero) worldDir = modelRoot.up;
            }

            // Convert the world explosion direction back into the child's local space
            data.explodeDirection = child.parent.InverseTransformDirection(worldDir).normalized;
            parts.Add(data);
        }
    }

    void Update()
    {
        HandleExplosion();
    }

    void HandleExplosion()
    {
        foreach (PartData data in parts)
        {
            if (data.part == null) continue;

            Vector3 targetPosition;

            if (isExploded)
            {
                targetPosition = data.originalLocalPosition + data.explodeDirection * explodeDistance;
            }
            else
            {
                targetPosition = data.originalLocalPosition;
            }

            data.part.localPosition = Vector3.Lerp(
                data.part.localPosition,
                targetPosition,
                Time.deltaTime * moveSpeed
            );

            data.part.localRotation = Quaternion.Lerp(
                data.part.localRotation,
                data.originalLocalRotation,
                Time.deltaTime * moveSpeed
            );
        }
    }

    public void Explode()
    {
        isExploded = true;
    }

    public void Assemble()
    {
        isExploded = false;
    }

    public void ToggleExplode()
    {
        isExploded = !isExploded;
    }
}