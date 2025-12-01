using System.Collections.Generic;
using UnityEngine;
using NoAlloq;
using System.Collections;

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
	public float Radius = 4.0f;
	public float SpaceBetween = 1.0f;
	[SerializeField] private float _tickInterval = 0.05f;

    public List<EnvQueryTestDistance> EnvQueryTestDistances = new();
    public List<EnvQueryTestPathFinding> EnvQueryTestPathFindings = new();
    public List<EnvQueryTestDot> EnvQueryTestDots = new();
    public List<EnvQueryTestTrace> EnvQueryTestTraces = new();
	private int _testCount = 0;

	private GameObject _querier;
	private EnvQueryGenerator _generator;
	private List<EnvQueryItem> _eqsItems;
	private List<EnvQueryItem> _eqsItemsRef;

	void Start()
	{
		if(_querier == null)
		{
			_querier = gameObject;
		}
		if(CenterOfItems == null)
		{
			CenterOfItems = _querier;
        }

		_testCount = EnvQueryTestDistances.Count + 
			EnvQueryTestPathFindings.Count + 
			EnvQueryTestDots.Count + 
			EnvQueryTestTraces.Count;
    }

	/// <summary>
	/// 임의의 값으로 EQS를 초기화한다.
	/// </summary>
	/// <param name="radius"> EQS 범위 </param>
	/// <param name="spaceBetween"> EQS 포인트들 간 간격 </param>
    public void InitializeQuery(float radius)
	{
		StopAllCoroutines();

        Radius = radius;

        if (GeneratorType == EnvQueryGeneratorType.OnCircle)
        {
            _generator = new EnvQueryGeneratorOnCircle(Radius, SpaceBetween);
        }
        else if (GeneratorType == EnvQueryGeneratorType.SimpleGrid)
        {
            _generator = new EnvQueryGeneratorSimpleGrid(Radius, SpaceBetween);
        }

        if (CenterOfItems != null && _generator != null)
        {
            _eqsItems = _generator.GenerateItems(_testCount, CenterOfItems.transform);
        }
        else
        {
            _eqsItems = new List<EnvQueryItem>();
        }

        _eqsItemsRef = _eqsItems.GetRange(0, _eqsItems.Count);
    }

	public void StartQuery()
	{
        StartCoroutine(InvestigateEQS());
    }

    private IEnumerator InvestigateEQS()
    {
        while (true)
        {
            ResetScore();
            foreach (EnvQueryItem item in _eqsItems)
            {
                item.UpdateNavMeshProjection();
            }

            RunEQSTests(EnvQueryTestDistances);
            RunEQSTests(EnvQueryTestPathFindings);
            RunEQSTests(EnvQueryTestDots);
            RunEQSTests(EnvQueryTestTraces);

            FinalizeEQS();
            yield return new WaitForSeconds(_tickInterval);
        }
    }


    private void RunEQSTests<T>(List<T> tests) where T : EnvQueryTest
    {
        if (tests.Count == 0) return;

        for (int currentTest = 0; currentTest < tests.Count; currentTest++)
        {
            tests[currentTest].RunTest(currentTest, _eqsItems);
            tests[currentTest].NormalizeItemScores(currentTest, _eqsItems);
        }
    }

    private void ResetScore()
    {
		foreach(EnvQueryItem item in _eqsItems)
		{
			item.Score = 0.0f;
		}
	}

	private void FinalizeEQS()
	{
		NormalizeScore();
		BestItem = _eqsItems.AsSpan().Where(x => x.IsValid)
			.OrderByDescending(_eqsItemsRef.AsSpan(), x => x.Score)
			.FirstOrDefault();
	}

	private void NormalizeScore()
	{
        if(_eqsItems == null || _eqsItems.Count < 1)
        {
            return;
        }

		float maxScore = _eqsItems[0].Score;
		float minScore = _eqsItems[0].Score;

		foreach(EnvQueryItem item in _eqsItems)
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
			foreach(EnvQueryItem item in _eqsItems)
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
