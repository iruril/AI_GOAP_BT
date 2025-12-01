using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EQS_ctx",menuName = "AI/EQS Context", order = int.MaxValue)]
public class EQSContextSO : ScriptableObject
{
    public EnvQuery.EnvQueryGeneratorType GeneratorType;
    public float Radius;
    public float SpaceBetween;

    public List<EnvQueryTestDistance> Distances;
    public List<EnvQueryTestPathFinding> Paths;
    public List<EnvQueryTestDot> Dots;
    public List<EnvQueryTestTrace> Traces;
}
