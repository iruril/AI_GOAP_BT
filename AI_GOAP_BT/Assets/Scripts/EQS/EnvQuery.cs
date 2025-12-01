using System.Collections.Generic;
using UnityEngine;
using NoAlloq;
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
	public GameObject CenterOfItems;
	[SerializedDictionary("Context Name", "Context SO")]
	private SerializedDictionary<string, EQSContextSO> Contexts = new();
	private EQSContextSO currentCTX;

    private int testCount = 0;

	private GameObject querier;
	private EnvQueryGenerator generator;
	private List<EnvQueryItem> eqsItems;
	private List<EnvQueryItem> eqsItemsRef;

	void Start()
	{
		if(querier == null)
		{
			querier = gameObject;
		}
		if(CenterOfItems == null)
		{
			CenterOfItems = querier;
        }

		LoadContext(Contexts.First().Value);
    }
    public void LoadContext(EQSContextSO context)
    {
        currentCTX = context;
        InitializeQuery();
    }

    private void InitializeQuery()
    {
        testCount = currentCTX.Distances.Count +
                    currentCTX.Paths.Count +
                    currentCTX.Dots.Count +
                    currentCTX.Traces.Count;

        if (GeneratorType == EnvQueryGeneratorType.OnCircle)
        {
            generator = new EnvQueryGeneratorOnCircle(currentCTX.Radius, currentCTX.SpaceBetween);
        }
        else if (GeneratorType == EnvQueryGeneratorType.SimpleGrid)
        {
            generator = new EnvQueryGeneratorSimpleGrid(currentCTX.Radius, currentCTX.SpaceBetween);
        }

        if (CenterOfItems != null && generator != null)
        {
            eqsItems = generator.GenerateItems(testCount, CenterOfItems.transform);
        }
        else
        {
            eqsItems = new List<EnvQueryItem>();
        }

        eqsItemsRef = eqsItems.GetRange(0, eqsItems.Count);
    }

    public void TickEQS()
    {
        ResetScore();
        foreach (EnvQueryItem item in eqsItems)
        {
            item.ApplyAstarProjection();
        }

        RunEQSTests(currentCTX.Distances);
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
		BestItem = eqsItems.AsSpan().Where(x => x.IsValid)
			.OrderByDescending(eqsItemsRef.AsSpan(), x => x.Score)
			.FirstOrDefault();
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
	//	if (isActiveAndEnabled && _eqsItems != null)
	//	{
	//		foreach (EnvQueryItem item in _eqsItems)
	//		{
	//			if (item.IsValid)
	//			{
	//				Gizmos.color = Color.HSVToRGB((item.Score / 2.0f), 1.0f, 1.0f);
	//				Gizmos.DrawWireSphere(item.GetWorldPosition(), 0.25f);
	//				UnityEditor.Handles.Label(item.GetWorldPosition(), ((int)(item.Score * 100f)).ToString());
	//			}
	//		}
	//	}
	//	if (isActiveAndEnabled && BestItem != null)
	//	{
	//		Gizmos.color = Color.blue;
	//		Gizmos.DrawSphere(BestItem.GetWorldPosition(), 0.25f);
	//	}
	//}
#endif
}
