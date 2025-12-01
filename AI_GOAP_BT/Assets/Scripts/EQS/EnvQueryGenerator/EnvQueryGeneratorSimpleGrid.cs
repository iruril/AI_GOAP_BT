using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvQueryGeneratorSimpleGrid : EnvQueryGenerator
{
	private float _radius;
	private float _spaceBetween;

	public EnvQueryGeneratorSimpleGrid()
	{
		this._radius = 4.0f;
		this._spaceBetween = 1.0f;
	}

	public EnvQueryGeneratorSimpleGrid(float radius, float spaceBetween)
	{
		this._radius = radius;
		this._spaceBetween = spaceBetween;
	}

    public List<EnvQueryItem> GenerateItems(int numTests, Transform centerOfItems)
    {
        List<EnvQueryItem> items = new List<EnvQueryItem>();

		Vector3 position = Vector3.zero;
		items.Add(new EnvQueryItem(numTests, position, centerOfItems));

		int numOfSteps = (int)Mathf.Ceil(_radius / _spaceBetween);

		// First quadrant
		for(int xi = 0; xi < numOfSteps; xi++)
		{
			for(int zi = 0; zi < numOfSteps; zi++)
			{
				position.x = xi * _spaceBetween + _spaceBetween/2.0f;
				position.y = 0.0f;
				position.z = zi * _spaceBetween + _spaceBetween/2.0f;
				items.Add(new EnvQueryItem(numTests, position, centerOfItems));
			}
		}
		// Second quadrant
		for(int xi = 0; xi < numOfSteps; xi++)
		{
			for(int zi = 0; zi < numOfSteps; zi++)
			{
				position.x = -(xi * _spaceBetween + _spaceBetween/2.0f);
				position.y =   0.0f;
				position.z =   zi * _spaceBetween + _spaceBetween/2.0f;
				items.Add(new EnvQueryItem(numTests, position, centerOfItems));
			}
		}
		// Third quadrant
		for(int xi = 0; xi < numOfSteps; xi++)
		{
			for(int zi = 0; zi < numOfSteps; zi++)
			{
				position.x = -(xi * _spaceBetween + _spaceBetween/2.0f);
				position.y =   0.0f;
				position.z = -(zi * _spaceBetween + _spaceBetween/2.0f);
				items.Add(new EnvQueryItem(numTests, position, centerOfItems));
			}
		}
		// Fourth quadrant
		for(int xi = 0; xi < numOfSteps; xi++)
		{
			for(int zi = 0; zi < numOfSteps; zi++)
			{
				position.x =   xi * _spaceBetween + _spaceBetween/2.0f;
				position.y =   0.0f;
				position.z = -(zi * _spaceBetween + _spaceBetween/2.0f);
				items.Add(new EnvQueryItem(numTests, position, centerOfItems));
			}
		}

        return items;
    }
}