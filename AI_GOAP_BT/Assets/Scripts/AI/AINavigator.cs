using UnityEngine;
using Pathfinding;

public class AINavigator : MonoBehaviour
{
    public RichAI AI { get; private set; }
    private Transform destination;

    public Vector2 MoveAxis { get; private set; }

    void Awake()
    {
        AI = GetComponent<RichAI>();
        destination = GetComponent<AIDestinationSetter>().target;
        destination.name = $"{this.name}_{destination.name}";
        destination.parent = null;

        AI.simulateMovement = false;
    }

    void Start()
    {
        
    }

    void Update()
    {
        UpdateMoveDirection();
    }

    void UpdateMoveDirection()
    {
        Vector3 nextPosition;
        Quaternion nextRotation;
        AI.MovementUpdate(Time.deltaTime, out nextPosition, out nextRotation);

        Vector3 deltaVelocity = nextPosition - this.transform.position;
        deltaVelocity.y = 0;
        deltaVelocity.Normalize();

        float directionX = Vector3.Dot(transform.right, deltaVelocity);
        float directionY = Vector3.Dot(transform.forward, deltaVelocity);
        MoveAxis = new Vector2(directionX, directionY);
    }


    /// <summary>
    /// AI의 Destination을 설정한다. 
    /// </summary>
    /// <param name="position"> 이동할 위치</param>
    public void SetDestination(Vector3 position)
    {
        destination.position = position;
    }
}
