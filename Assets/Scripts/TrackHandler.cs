using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrackHandler : MonoBehaviour
{
    public float rowSpacing = 1f; // Spacing between rows as a fraction of the platform length
    public float gizmoSphereSize = 0.45f; 
    public int itemsPerRow = 3; // Number of items to be placed in each row
    public float spacingBetweenItems = 0.65f;
    private float platformLength;
    private float platformWidth;
    public float[] obstaclePositions;

    [Header("Editor Gizmo Settings")]
    public int selectedRowIndex = 0; // Index of the selected row
    public int selectedItemIndex = 0; // Index of the selected item
    //private bool randomPositionSelected = false; // Flag to ensure we only pick a position once


    private void OnEnable()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null) return; // Exit if there is no MeshRenderer

        platformLength = renderer.bounds.size.z; // Length of the platform along the z-axis
        platformWidth = renderer.bounds.size.x; // Width of the platform along the x-axis

        // Generate obstacle positions based on the platform length, rowSpacing, and sphere size
        GenerateObstaclePositions();
    }



    // Method to generate evenly spaced obstacle positions based on rowSpacing and items per row
    private void GenerateObstaclePositions()
    {
        // Ensure the gizmoSphereSize meets the minimum requirement
        gizmoSphereSize = Mathf.Max(gizmoSphereSize, 0.45f); // Minimum sphere size

        // Calculate effective spacing based on gizmo sphere size
        float effectiveSpacing = rowSpacing + gizmoSphereSize; // Total space occupied by a sphere and its spacing
        int obstacleCount = Mathf.FloorToInt(platformLength / effectiveSpacing); // Count based on available length

        // Ensure there is at least one obstacle to position
        if (obstacleCount < 1)
        {
            obstacleCount = 1; // Ensure at least one position is generated
        }

        obstaclePositions = new float[obstacleCount];

        // Generate positions ensuring even distribution from 0 to 1
        for (int i = 0; i < obstacleCount; i++)
        {
            // Calculate normalized position evenly spaced between 0 and 1
            obstaclePositions[i] = (float)i / (obstacleCount - 1); // Normalize to the maximum length
        }
    }

    // Method to generate item positions in each row
    private Vector3[] GenerateItemPositions(Vector3 rowBasePosition)
    {
        Vector3[] itemPositions = new Vector3[itemsPerRow];

        // Calculate total width of the items including spacing
        float totalItemWidth = (itemsPerRow * gizmoSphereSize) + (spacingBetweenItems * (itemsPerRow - 1));
        float startX = rowBasePosition.x - (totalItemWidth / 2) + (gizmoSphereSize / 2); // Center items based on their width

        for (int i = 0; i < itemsPerRow; i++)
        {
            // Calculate the position of each item in the row
            itemPositions[i] = new Vector3(startX + (i * (gizmoSphereSize + spacingBetweenItems)), rowBasePosition.y, rowBasePosition.z);
        }

        return itemPositions;
    }


    public Vector3 GetWorldPosition(float normalizedRowPosition, int itemIndex)
    {
        // Ensure the itemIndex is valid
        if (itemIndex < 0 || itemIndex >= itemsPerRow)
        {
            Debug.LogWarning("Invalid item index");
            return Vector3.zero; // Return zero vector if the index is out of bounds
        }

        // Get the MeshRenderer for platform bounds
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null) return Vector3.zero; // Exit if no MeshRenderer

        // Calculate platform length (z-axis) and width (x-axis)
        platformLength = renderer.bounds.size.z;
        platformWidth = renderer.bounds.size.x;

        // Calculate the z-position for the row using the normalized row position
        Vector3 platformStart = renderer.bounds.min; // Start point of the platform
        float rowZPosition = Mathf.Lerp(platformStart.z, platformStart.z + platformLength, normalizedRowPosition);

        // Calculate total width of the items including spacing
        float totalItemWidth = (itemsPerRow * gizmoSphereSize) + (spacingBetweenItems * (itemsPerRow - 1));
        float startX = transform.position.x - (totalItemWidth / 2) + (gizmoSphereSize / 2); // Center items based on their width

        // Calculate the x-position of the item using the itemIndex
        float itemXPosition = startX + (itemIndex * (gizmoSphereSize + spacingBetweenItems));

        // Return the world position for the row and item
        return new Vector3(itemXPosition, transform.position.y, rowZPosition);
    }



    // Method to randomly select a row and item index, then return its world position
    public Vector3 GetRandomWorldPosition()
    {
        // Ensure there are obstacle positions to select from
        if (obstaclePositions == null || obstaclePositions.Length == 0)
            return Vector3.zero;

        // Randomly pick a valid row index
        int randomRowIndex = Random.Range(0, obstaclePositions.Length);

        // Randomly pick a valid item index within the row
        int randomItemIndex = Random.Range(0, itemsPerRow);

        // Return the world position for the selected row and item index
        return GetWorldPosition(obstaclePositions[randomRowIndex], randomItemIndex);
    }





#if UNITY_EDITOR
    /// <summary>
    /// will need post mvi so making half ass adjustments rn..
    /// </summary>
    void OnDrawGizmos()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null) return; // Exit if there is no MeshRenderer

        platformLength = renderer.bounds.size.z; // Length of the platform along the z-axis
        platformWidth = renderer.bounds.size.x; // Width of the platform along the x-axis
        // Generate obstacle positions based on the platform length, rowSpacing, and sphere size
        GenerateObstaclePositions();

        // Draw gizmos for each normalized obstacle position
        Vector3 platformStart = renderer.bounds.min; // Start point of the platform along the z-axis

        // Draw rows and items
        for (int i = 0; i < obstaclePositions.Length; i++)
        {
            // Calculate the z position for the current row
            float zPosition = Mathf.Lerp(platformStart.z, platformStart.z + platformLength, obstaclePositions[i]);
            // Calculate the base position for the row
            Vector3 rowBasePosition = new Vector3(transform.position.x, transform.position.y, zPosition);
            // Generate item positions for the current row
            Vector3[] itemPositions = GenerateItemPositions(rowBasePosition);
            // Draw gizmos for each item in the row
            foreach (Vector3 itemPosition in itemPositions)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(itemPosition, gizmoSphereSize / 2); // Use radius for DrawSphere
            }
        }
    }
#endif
}
