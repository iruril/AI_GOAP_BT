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

    private const float _pathNotFoundScore = -10000f;
    private const float _pathExistScore = 1.0f;
    private const float _pathNotExistScore = 0.0f;

    public override void RunTest(int currentTest, List<EnvQueryItem> envQueryItems)
    {
        if (Target == null || envQueryItems == null)
        {
            foreach (EnvQueryItem item in envQueryItems)
            {
                item.TestResults[currentTest] = _pathNotExistScore;
            }
            return;
        }

        foreach (EnvQueryItem item in envQueryItems)
        {
            Vector3 startPos = item.GetWorldPosition();
            Vector3 endPos = Target.position;

            ABPath path = ABPath.Construct(startPos, endPos, (Path p) => OnPathComplete(p, item, currentTest));
        }
    }

    private void OnPathComplete(Path p, EnvQueryItem item, int currentTest)
    {
        ABPath path = p as ABPath;

        if (path == null)
        {
            Debug.LogError("Path is not of type ABPath");
            item.TestResults[currentTest] = _pathNotExistScore;
            return;
        }

        if (PathFindingType == PathFindingTestType.PathExist)
        {
            item.TestResults[currentTest] = (path.CompleteState == PathCompleteState.Complete) ? _pathExistScore : _pathNotExistScore;
        }
        else if (PathFindingType == PathFindingTestType.PathLength)
        {
            if (path.CompleteState == PathCompleteState.Complete)
            {
                item.TestResults[currentTest] = -path.GetTotalLength();
            }
            else
            {
                item.TestResults[currentTest] = _pathNotFoundScore;
            }
        }
    }
}