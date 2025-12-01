using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using System.Linq;

public class EnvQuery : MonoBehaviour
{
	public enum EnvQueryGeneratorType
	{
		OnCircle,
		SimpleGrid
    }

    public EnvQueryItem BestItem { get; private set; }

    public EnvQueryGeneratorType GeneratorType = EnvQueryGeneratorType.OnCircle;
    public float Radius;
    public float SpaceBetween;
    public GameObject CenterOfItems;
    [SerializedDictionary("Context Name", "Context SO")]
    public SerializedDictionary<string, EQSContext> Contexts = new();
	private EQSContext currentCTX;

    private int testCount = 0;

	private GameObject querier;
	private EnvQueryGenerator generator;
	private List<EnvQueryItem> eqsItems;

	void Start()
    {
        Init();
        LoadContext(Contexts.First().Value);
    }

    private void Init()
    {
        if (querier == null)
        {
            querier = gameObject;
        }
        if (CenterOfItems == null)
        {
            CenterOfItems = querier;
        }

        if (GeneratorType == EnvQueryGeneratorType.OnCircle)
        {
            generator = new EnvQueryGeneratorOnCircle(Radius, SpaceBetween);
        }
        else if (GeneratorType == EnvQueryGeneratorType.SimpleGrid)
        {
            generator = new EnvQueryGeneratorSimpleGrid(Radius, SpaceBetween);
        }
    }

    public void LoadContext(string contextName)
    {
		if (!Contexts.ContainsKey(contextName))
		{
			Debug.LogWarning($"There's no such key : {contextName}");
			return;
		}
        currentCTX = Contexts[contextName];
        ApplyContext();
    }

    public void LoadContext(EQSContext context)
    {
        currentCTX = context;
        ApplyContext();
    }

    private void ApplyContext()
    {
        testCount = currentCTX.Dists.Count +
                    currentCTX.Paths.Count +
                    currentCTX.Dots.Count +
                    currentCTX.Traces.Count;

        if (CenterOfItems != null && generator != null)
        {
            eqsItems = generator.GenerateItems(testCount, CenterOfItems.transform);
        }
        else
        {
            eqsItems = new List<EnvQueryItem>();
        }
    }

    public void TickEQS()
    {
        ResetScore();
        foreach (EnvQueryItem item in eqsItems)
        {
            item.ApplyAstarProjection();
        }

        RunEQSTests(currentCTX.Dists);
        RunEQSTests(currentCTX.Paths);
        RunEQSTests(currentCTX.Dots);
        RunEQSTests(currentCTX.Traces);

        FinalizeEQS();
    }

    private void RunEQSTests<T>(List<T> tests) where T : EnvQueryTest
    {
        if (tests.Count == 0) return;

        for (int currentTest = 0; currentTest < tests.Count; currentTest++)
        {
            tests[currentTest].RunTest(currentTest, eqsItems);
            tests[currentTest].NormalizeItemScores(currentTest, eqsItems);
        }
    }

    private void ResetScore()
    {
		foreach(EnvQueryItem item in eqsItems)
		{
			item.Score = 0.0f;
		}
	}

	private void FinalizeEQS()
	{
		NormalizeScore(); 
		
		EnvQueryItem best = null;
        float bestScore = float.NegativeInfinity;

        var span = eqsItems.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            ref var item = ref span[i];
            if (!item.IsValid) continue;

            if (item.Score > bestScore)
            {
                bestScore = item.Score;
                best = item;
            }
        }

        BestItem = best;
    }

	private void NormalizeScore()
	{
        if(eqsItems == null || eqsItems.Count < 1)
        {
            return;
        }

		float maxScore = eqsItems[0].Score;
		float minScore = eqsItems[0].Score;

		foreach(EnvQueryItem item in eqsItems)
		{
			if(item.Score > maxScore)
			{
				maxScore = item.Score;
			}
			if(item.Score < minScore)
			{
				minScore = item.Score;
			}
		}

		if(maxScore != minScore)
		{
			foreach(EnvQueryItem item in eqsItems)
			{
				item.Score = (item.Score - minScore) / (maxScore - minScore);
			}
		}
	}

#if UNITY_EDITOR
    //private void OnDrawGizmos()
    //{
    //    if (isActiveAndEnabled && eqsItems != null && BestItem != null)
    //    {
    //        foreach (EnvQueryItem item in eqsItems)
    //        {
    //            if (item.IsValid)
    //            {
    //                Gizmos.color = Color.HSVToRGB((item.Score / 2.0f), 1.0f, 1.0f);
    //                Gizmos.DrawWireSphere(item.GetWorldPosition(), 0.25f);
    //                UnityEditor.Handles.Label(item.GetWorldPosition(), ((int)(item.Score * 100f)).ToString());
    //            }
    //        }
    //    }
    //    if (isActiveAndEnabled && BestItem != null && BestItem != null)
    //    {
    //        Gizmos.color = Color.blue;
    //        Gizmos.DrawSphere(BestItem.GetWorldPosition(), 0.25f);
    //    }
    //}
#endif
}
