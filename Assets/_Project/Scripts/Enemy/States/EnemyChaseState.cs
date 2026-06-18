using UnityEngine;
using BulletHeaven.Core;

namespace BulletHeaven.Enemy
{
    public class EnemyChaseState : IEnemyState
    {
        private readonly Enemy _enemy;
        private float _destinationTimer;
        private float _attackTimer;

        public EnemyChaseState(Enemy enemy)
        {
            _enemy = enemy;
        }

        public void Enter()
        {
            _destinationTimer = 0f;
            _attackTimer      = 0f;

            _enemy.Agent.enabled   = true;
            _enemy.Agent.isStopped = false;

            _enemy.AnimatedMesh?.PlayAnimation(_enemy.WalkAnimName, true);
        }

        public void Tick()
        {
            if (_enemy.PlayerTransform == null) return;

            TickDestination();
            UpdateAnimationSpeed();

            if (_attackTimer > 0f)
                _attackTimer -= Time.deltaTime;
        }

        public void Exit()
        {
            if (_enemy.Agent.enabled)
                _enemy.Agent.isStopped = true;
        }

        public void OnHitPlayer(GameObject player)  // not in interface — Chase-only behaviour
        {
            if (_attackTimer > 0f) return;

            _attackTimer = _enemy.AttackInterval;

            if (player.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(_enemy.Damage);
        }

        private void TickDestination()
        {
            _destinationTimer -= Time.deltaTime;
            if (_destinationTimer > 0f) return;

            _destinationTimer = _enemy.DestinationUpdateInterval;

            if (_enemy.Agent.enabled && _enemy.Agent.isOnNavMesh)
                _enemy.Agent.SetDestination(_enemy.PlayerTransform.position);
        }

        private void UpdateAnimationSpeed()
        {
            if (_enemy.AnimatedMesh == null || !_enemy.Agent.enabled) return;

            float normalizedSpeed = _enemy.Agent.velocity.magnitude / _enemy.Agent.speed;
            _enemy.AnimatedMesh.SetSpeedMultiplier(normalizedSpeed);
        }
    }
}
