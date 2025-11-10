using UnityEngine;
using Pathfinding;
using System;

public class AINavigator : MonoBehaviour
{
    public RichAI AI { get; private set; }
    public Transform Destination { get; private set; }

    public float moveAxisSmoothTime = 0.08f;
    public Vector2 MoveAxis { get; private set; }

    void Awake()
    {
        AI = GetComponent<RichAI>();
        Destination = GetComponent<AIDestinationSetter>().target;
        Destination.name = $"{this.name}_{Destination.name}";
        Destination.parent = null;

        AI.simulateMovement = false;
    }

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        UpdateMoveDirection();
    }

    private Vector2 moveAxisVelocity = Vector2.zero;
    void UpdateMoveDirection()
    {
        Vector3 nextPosition;
        Quaternion nextRotation;
        AI.MovementUpdate(Time.fixedDeltaTime, out nextPosition, out nextRotation);

        Vector3 deltaVelocity = nextPosition - this.transform.position;
        deltaVelocity.y = 0;
        deltaVelocity.Normalize();

        float directionX = Vector3.Dot(transform.right, deltaVelocity);
        float directionY = Vector3.Dot(transform.forward, deltaVelocity);
        Vector2 targetAxis = new Vector2(directionX, directionY);

        MoveAxis = Vector2.SmoothDamp(
            MoveAxis,
            targetAxis,
            ref moveAxisVelocity,
            moveAxisSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );
    }

    /// <summary>
    /// AI의 Destination을 설정한다. 
    /// </summary>
    /// <param name="position"> 이동할 위치</param>
    public void SetDestination(Vector3 position)
    {
        Destination.position = position;
    }
}
