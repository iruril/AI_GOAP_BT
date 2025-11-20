using UnityEngine;
using System;
using Random = System.Random;

public class MathUtility
{
    /// <summary>
    /// 두 쿼터니언 간의 사잇각을 구한 뒤, 오일러 값으로 반환한다
    /// </summary>
    /// <param name="from">쿼터니언 각 출발</param>
    /// <param name="to">쿼터니언 각 도착</param>
    /// <returns>두 쿼터니언의 사잇각의 float 오일러 값</returns>
    public static float CalculateAngle(Quaternion from, Quaternion to)
    {
        Vector3 fromDir = from * Vector3.forward;
        Vector3 toDir = to * Vector3.forward;

        return Vector3.Angle(fromDir, toDir);
    }

    /// <summary>
    /// float배열을 Vector3 값으로 변경해 반환한다. Json Data를 파싱하기 위해 사용한다.
    /// </summary>
    /// <param name="array"> float배열 </param>
    /// <returns>배열의 길이가 3 이상이면 변환된 Vector3를, 아니면 Vector3.zero를 반환한다</returns>
    public static Vector3 ArrayToVector3(float[] array)
    {
        Vector3 vector = Vector3.zero;
        if (array.Length >= 3)
        {
            vector.x = array[0];
            vector.y = array[1];
            vector.z = array[2];
        }
        return vector;
    }

    /// <summary>
    /// Quaternion의 SmoothDamp를 연산한다
    /// </summary>
    /// <param name="currentRot"> 현재 회전값</param>
    /// <param name="targetRot"> 목표 회전값 </param>
    /// <param name="currentVelocity"> 레퍼런스 회전값 </param>
    /// <param name="smoothTime"> 부드럽게 할 시간값 </param>
    /// <returns> 연산된 회전값 </returns>
    public static Quaternion SmoothDamp(Quaternion currentRot, Quaternion targetRot, ref Quaternion currentVelocity, float smoothTime)
    {
        if (Time.deltaTime < Mathf.Epsilon) return currentRot;
        if (smoothTime == 0) return targetRot;

        Vector3 current = currentRot.eulerAngles;
        Vector3 target = targetRot.eulerAngles;
        return Quaternion.Euler(
          Mathf.SmoothDampAngle(current.x, target.x, ref currentVelocity.x, smoothTime),
          Mathf.SmoothDampAngle(current.y, target.y, ref currentVelocity.y, smoothTime),
          Mathf.SmoothDampAngle(current.z, target.z, ref currentVelocity.z, smoothTime)
          );
    }

    /// <summary>
    /// 가우시안 분포값(정규분포)을 반환한다.
    /// </summary>
    /// <param name="mean"> 평균값, 0이면 표준정규분포에 가깝다 </param>
    /// <param name="standard"> 표준편차 값 </param>
    /// <returns> 표준편차에 의한 출현한 값 </returns>
    public static float SampleGaussian(float mean, float standard)
    {
        Random random = new();

        double x1 = 1 - random.NextDouble();
        double x2 = 1 - random.NextDouble();

        double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
        return (float)y1 * standard + mean;
    }/// <summary>
     /// focus의 값을 기준으로 2차원 벡터 축 입력을 받았을 시 라디안 회전값을 반환한다.
     /// </summary>
     /// <param name="focus"> 기준이 되는 회전값. ex) camRotationY </param>
     /// <param name="axis"> 입력된 축 값. axis.x는 horizontal, axis.y는 vertical이다. </param>
     /// <returns> Rad.Rotation </returns>
    public static float CalculateRotationAngle(float focus, Vector2 axis)
    {
        return focus + Mathf.Atan2(axis.x, axis.y) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// 두 회전값이 서로 반대방향을 바라보는지를 검사한다.
    /// </summary>
    /// <param name="prevDirection"> 검사할 첫번째 회전값 </param>
    /// <param name="nextDirection"> 검사할 두번째 회전값 </param>
    /// <param name="error"> 오차범위. 이는 작을수록 더욱 정확히 반대방향인지를 검사한다. </param>
    /// <returns> 검사결과 참/거짓. </returns>
    public static bool IsOppositeDirection(Quaternion prevDirection, Quaternion nextDirection, float error)
    {
        float maxError = 180 - error;
        float minError = error - 180;
        float angle = CalculateAngle(prevDirection, nextDirection);
        return angle > maxError || angle < minError;
    }

    public static bool IsOppositeDirection(Vector3 prevDirection, Vector3 nextDirection, float error)
    {
        float maxError = 180 - error;
        float minError = error - 180;
        float angle = Vector3.Angle(prevDirection, nextDirection);
        return angle > maxError || angle < minError;
    }

    public static bool IsSameDirection(Vector3 prevDirection, Vector3 nextDirection, float error)
    {
        float angle = Vector3.Angle(prevDirection, nextDirection);
        return angle < error;
    }

    public static bool IsRightDirection(Vector3 prevDirection, Vector3 nextDirection, float error)
    {
        float angle = Vector3.SignedAngle(prevDirection, nextDirection, Vector3.up);
        return angle > error;
    }

    public static bool IsLeftDirection(Vector3 prevDirection, Vector3 nextDirection, float error)
    {
        float angle = Vector3.SignedAngle(prevDirection, nextDirection, Vector3.up);
        return angle < -error;
    }

    public static bool IsHeadingForward(Vector3 forward, Vector3 direction, float angle)
    {
        forward.y = 0;
        direction.y = 0;

        if (forward.sqrMagnitude < 0.001f || direction.sqrMagnitude < 0.001f)
            return false;

        float threshold = Mathf.Cos(angle * Mathf.Deg2Rad);
        float dot = Vector3.Dot(forward.normalized, direction.normalized);
        return dot >= threshold;
    }

    public static Vector3 GetRandomPositionInCircle(Vector3 position, float radius)
    {
        Vector3 normalCirclePoint = UnityEngine.Random.onUnitSphere;
        normalCirclePoint.y = 0;

        float r = UnityEngine.Random.Range(0.0f, radius);
        return position + (normalCirclePoint * r);
    }
}
