using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

public class EnvQueryItem
{
    public float Score;
    public bool IsValid;
    public float[] TestResults;

    private Transform _centerOfItems; // 기준 위치
    private Vector3 _location; // 상대 위치
    private Vector3 _navLocation; // 상대 위치(NavMesh 투영 후)

    public EnvQueryItem(int numTests, Vector3 location, Transform centerOfItems)
    {
        Score = 0.0f;
        IsValid = true;
        TestResults = new float[numTests];
        this._centerOfItems = centerOfItems;
        this._location = location;
        this._navLocation = location;
    }

    public Vector3 GetWorldPosition()
    {
        return _centerOfItems.position + _navLocation;
    }

    public void ApplyAstarProjection()
    {
        IsValid = true;
        Vector3 worldPosition = _centerOfItems.position + _location;
        Vector3 navMeshPosition = GetNearestPosition(worldPosition, 2f, ref IsValid);

        if (navMeshPosition != worldPosition)
        {
            _navLocation = navMeshPosition - _centerOfItems.position;
        }
        else
        {
            _navLocation = _location;
        }
    }

    private Vector3 GetNearestPosition(Vector3 position, float maxDistance, ref bool result)
    {
        NNInfo nearestNode = AstarPath.active.GetNearest(position);
        if (nearestNode.node != null && (nearestNode.position - position).sqrMagnitude <= maxDistance * maxDistance)
        {
            result = true;
            return nearestNode.position;
        }
        else
        {
            result = false;
            return position;
        }
    }
}