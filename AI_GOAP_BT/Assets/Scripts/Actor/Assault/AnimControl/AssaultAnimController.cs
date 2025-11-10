using System.Collections.Generic;
using UnityEngine;
using MEC;
using UnityEngine.UI;
using System;

public class AssaultAnimController : MonoBehaviour
{
    public Animator Anim { get; private set; }
    private AINavigator navigator; 
    
    bool isMoving = false;
    bool wasMoving = false;

    void Awake()
    {
        Anim = GetComponent<Animator>();
        navigator = GetComponent<AINavigator>();
    }

    void Start()
    {
        
    }

    void OnAnimatorMove()
    {
        if (Time.deltaTime <= 0) return;

        Vector3 nextPosition;
        Quaternion nextRotation;
        navigator.AI.MovementUpdate(Time.deltaTime, out nextPosition, out nextRotation);

        Vector3 rootPosition = new Vector3(Anim.rootPosition.x, nextPosition.y, Anim.rootPosition.z);
        Quaternion rootRotation = Anim.rootRotation;

        if (!navigator.AI.updateRotation)
            navigator.AI.FinalizeMovement(rootPosition, rootRotation);
        else
            navigator.AI.FinalizeMovement(rootPosition, nextRotation);
    }

    void Update()
    {
        wasMoving = isMoving;
        isMoving = navigator.AI.hasPath
                  && navigator.AI.reachedDestination == false
                  && navigator.AI.desiredVelocity.sqrMagnitude >= 0.01f;

        if (!wasMoving && isMoving)
            ComputeInitialTurn();

        UpdateMoveAxis();
    }

    void UpdateMoveAxis()
    {
        Anim.SetFloat(AnimHash.XAxis, navigator.MoveAxis.x);
        Anim.SetFloat(AnimHash.YAxis, navigator.MoveAxis.y);
    }

    void ComputeInitialTurn()
    {
        Debug.Log($"{this.name} : Start Turn!");
        navigator.AI.updateRotation = false;
        Vector3 desiredDir = navigator.AI.steeringTarget - transform.position;
        desiredDir.y = 0f;
        desiredDir.Normalize();

        float angle = Vector3.Angle(transform.forward, desiredDir);
        float angleLerp = Mathf.InverseLerp(0f, 180f, angle);

        float turnDir = Mathf.Sign(Vector3.Cross(transform.forward, desiredDir).y); // -1 = ¿ÞÂÊ, +1 = ¿À¸¥ÂÊ  
        
        Anim.SetFloat(AnimHash.AngleLerp, angleLerp);
        if (turnDir > 0)
        {
            Anim.CrossFade(AnimHash.StartMove_R, 0.25f);
        }
        else
        {
            Anim.CrossFade(AnimHash.StartMove_L, 0.25f);
        }

        Timing.RunCoroutine(DelayedCrossFade
            (
                AnimHash.Strafe, 0.93f, 
                () => navigator.AI.updateRotation = true
            )
        );
    }

    IEnumerator<float> DelayedCrossFade(int targetState, float time, Action onEnd)
    {
        yield return Timing.WaitForSeconds(time);
        Anim.CrossFade(targetState, 0.25f);
        onEnd.Invoke();
    }
}
