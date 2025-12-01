using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[System.Serializable]
public class EnvQueryTestPathFinding : EnvQueryTest
{
    public enum PathFindingTestType
    {
        PathExist,
        PathLength
    }

    public PathFindingTestType PathFindingType;
    public Transform Target;

    private const float pathPossibleScore = 1.0f;
    private const float pathNotPossibleScore = 0.0f;

    public override void RunTest(int currentTest, List<EnvQueryItem> envQueryItems)
    {
        if (Target == null || envQueryItems == null)
        {
            foreach (EnvQueryItem item in envQueryItems)
            {
                item.TestResults[currentTest] = pathNotPossibleScore;
            }
            return;
        }

        foreach (EnvQueryItem item in envQueryItems)
        {
            Vector3 startPos = item.GetWorldPosition();
            Vector3 endPos = Target.position;

            var startNode = AstarPath.active.GetNearest(startPos).node;
            var endNode = AstarPath.active.GetNearest(endPos).node;

            if (startNode == null || endNode == null || !startNode.Walkable || !endNode.Walkable)
            {
                item.TestResults[currentTest] = pathNotPossibleScore;
                continue;
            }

            bool possible = PathUtilities.IsPathPossible(startNode, endNode);

            item.TestResults[currentTest] = possible ? pathPossibleScore : pathNotPossibleScore;
        }
    }
}