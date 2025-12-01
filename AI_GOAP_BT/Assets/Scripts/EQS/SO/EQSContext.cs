using System.Collections.Generic;

[System.Serializable]
public class EQSContext
{
    public List<EnvQueryTestDistance> Dists;
    public List<EnvQueryTestPathFinding> Paths;
    public List<EnvQueryTestDot> Dots;
    public List<EnvQueryTestTrace> Traces;
}
