using System.Collections;
using UnityEngine;

namespace BulletHeaven.Enemy
{
    public class EnemyDeadState : IEnemyState
    {
        private readonly Enemy _enemy;
        private Coroutine _fallback;

        public EnemyDeadState(Enemy enemy)
        {
            _enemy = enemy;
        }

        public void Enter()
        {
            if (_enemy.HitCollider != null)
                _enemy.HitCollider.enabled = false;

            if (_enemy.Agent.enabled)
            {
                _enemy.Agent.isStopped = true;
                _enemy.Agent.enabled   = false;
            }

            if (_enemy.AnimatedMesh != null)
            {
                _enemy.AnimatedMesh.OnAnimationFinished = OnDeathAnimationFinished;
                _enemy.AnimatedMesh.PlayAnimation(_enemy.DeathAnimName, false);
            }

            _fallback = _enemy.StartCoroutine(FallbackRoutine());
        }

        public void Tick() { }

        public void Exit() { }

        private void OnDeathAnimationFinished()
        {
            if (_enemy.AnimatedMesh != null)
                _enemy.AnimatedMesh.OnAnimationFinished = null;

            if (_fallback != null)
            {
                _enemy.StopCoroutine(_fallback);
                _fallback = null;
            }

            _enemy.Release();
        }

        private IEnumerator FallbackRoutine()
        {
            yield return new WaitForSeconds(3f);
            _fallback = null;

            if (_enemy.AnimatedMesh != null)
                _enemy.AnimatedMesh.OnAnimationFinished = null;

            _enemy.Release();
        }
    }
}
