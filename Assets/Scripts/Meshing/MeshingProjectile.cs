using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class MeshingProjectile : MonoBehaviour
{
    private float lifeTime;
    private ObjectPool<MeshingProjectile> activeObjectPool;
    public Rigidbody rb;
    private Coroutine lifeTimeCoroutine;
    public void Initialize(ObjectPool<MeshingProjectile> pool, float duration)
    {
        activeObjectPool = pool;
        lifeTime = duration;
        
        if (lifeTimeCoroutine != null)
        {
            StopCoroutine(lifeTimeCoroutine);
        }
        lifeTimeCoroutine = StartCoroutine(LifetimeCoroutine());
    }

    private IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(lifeTime);
        lifeTimeCoroutine = null;
        activeObjectPool.Release(this);
    }
}
