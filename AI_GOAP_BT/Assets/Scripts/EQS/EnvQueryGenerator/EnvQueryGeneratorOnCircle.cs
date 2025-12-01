using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvQueryGeneratorOnCircle : EnvQueryGenerator
{
	private float _radius;
	private float _spaceBetween;

	public EnvQueryGeneratorOnCircle()
	{
		this._radius = 4.0f;
		this._spaceBetween = 1.0f;
	}

	public EnvQueryGeneratorOnCircle(float radius, float spaceBetween)
	{
		this._radius = radius;
		this._spaceBetween = spaceBetween;
	}

    public List<EnvQueryItem> GenerateItems(int numTests, Transform centerOfItems)
    {
        List<EnvQueryItem> items = new List<EnvQueryItem>();

		Vector3 position = Vector3.zero;
		items.Add(new EnvQueryItem(numTests, position, centerOfItems));

		int numOfStepsForRadialDirection = (int)Mathf.Ceil(_radius / _spaceBetween);
		for(int ri = 1; ri <= numOfStepsForRadialDirection; ri++)
		{
			for(int k = 0; k < ri*8; k++)
			{
				float theta = 1.0f/ri * k * Mathf.PI/4.0f;
				position.x = ri * _spaceBetween * Mathf.Sin(theta);
				position.y = 0.0f;
				position.z = ri * _spaceBetween * Mathf.Cos(theta);
				items.Add(new EnvQueryItem(numTests, position, centerOfItems));
			}
		}

        return items;
    }
}