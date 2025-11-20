using UnityEngine;
using MEC;
using System.Collections.Generic;

public class VFX_Object : MonoBehaviour
{
    [SerializeField]
    private float _lifeTime;
    public float LifeTime { get { return _lifeTime; } }

    private ParticleSystem _particle = null;
    private CoroutineHandle _lifeRoutine;

    protected virtual void Awake()
    {
        if (transform.GetChild(0).TryGetComponent<ParticleSystem>(out var particle))
        {
            _particle = particle;
        }
        if (_lifeTime == 0)
        {
            if (_particle != null) _lifeTime = _particle.main.duration;
            else _lifeTime = 10f;
        }
    }

    protected virtual void OnEnable()
    {
        if (_particle != null) _particle.Play();
        _lifeRoutine = Timing.RunCoroutine(LifeTimer());
    }

    private IEnumerator<float> LifeTimer()
    {
        yield return Timing.WaitForSeconds(_lifeTime);
        gameObject.SetActive(false);
    }

    protected virtual void OnDisable()
    {
        Timing.KillCoroutines(_lifeRoutine);
        EffectPoolManager.ReturnToPool(gameObject);
    }
}