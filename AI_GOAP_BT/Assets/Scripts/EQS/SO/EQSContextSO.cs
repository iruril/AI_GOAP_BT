using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EQS_ctx",menuName = "AI/EQS Context", order = int.MaxValue)]
public class EQSContextSO : ScriptableObject
{
    public List<EnvQueryTestDistance> Dists;
    public List<EnvQueryTestPathFinding> Paths;
    public List<EnvQueryTestDot> Dots;
    public List<EnvQueryTestTrace> Traces;
}
