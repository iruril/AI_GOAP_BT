using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[System.Serializable]
public class EnvQueryTestPathFinding : EnvQueryTest
{
    public Transform Start;

    private const float pathPossibleScore = 1.0f;
    private const float pathNotPossibleScore = 0.0f;

    public override void RunTest(int currentTest, List<EnvQueryItem> envQueryItems)
    {
        if (Start == null || envQueryItems == null)
        {
            foreach (EnvQueryItem item in envQueryItems)
            {
                item.TestResults[currentTest] = pathNotPossibleScore;
            }
            return;
        }

        foreach (EnvQueryItem item in envQueryItems)
        {
            Vector3 startPos = Start.position;
            Vector3 endPos = item.GetWorldPosition();

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