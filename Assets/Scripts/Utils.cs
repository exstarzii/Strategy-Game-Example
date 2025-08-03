using UnityEngine;
using UnityEngine.AI;

public class Utils
{
    public static LayerMask groundLayer = LayerMask.GetMask("Ground");
    public static LayerMask unitLayer = LayerMask.GetMask("Unit");
    public static LayerMask obstacleLayer = LayerMask.GetMask("Obstacle");
    public static LayerMask unitAndGroundLayer = LayerMask.GetMask("Unit", "Ground");

    public static float GetPathLength(NavMeshPath path)
    {
        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return length;
    }

    public static bool CheckPath(NavMeshPath path, float maxLength)
    {
        if (path == null || path.corners.Length == 0) return false;

        float length = Utils.GetPathLength(path);
        if (length <= maxLength)
        {
            return true;
        }
        return false;
    }

    public static bool CheckPath(Unit unit, Vector3 destination, out NavMeshPath path)
    {
        path = new NavMeshPath();
        if (NavMesh.CalculatePath(unit.transform.position, destination, NavMesh.AllAreas, path))
        {
            float length = Utils.GetPathLength(path);
            if (length <= unit.moveSpeed)
            {
                return true;
            }
        }
        return false;
    }

    public static bool CanAttack(Vector3 source, Collider target, float attackRange)
    {
        Vector3 closestPoint = target.ClosestPoint(source);
        Vector3 vector = (closestPoint - source);

        float distance = vector.magnitude;
        if (distance > attackRange) return false;

        if (Physics.Raycast(source, vector.normalized, out RaycastHit hit, distance, obstacleLayer))
        {
            return false;
        }
        return true;
    }
}
