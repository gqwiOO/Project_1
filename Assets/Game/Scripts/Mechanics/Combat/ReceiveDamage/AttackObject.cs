﻿using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.Const;
using Game.Scripts.Mechanics.Combat.Data;
using Game.Scripts.Mechanics.Hp;
using UnityEngine;

namespace Game.Scripts.Mechanics.Combat.ReceiveDamage
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class AttackObject: MonoBehaviour
    {
        [SerializeField] protected Animator animator;

        [SerializeField] protected Rigidbody2D rb;

        [SerializeField] private int destroyOnDeathTimerSeconds = 20;

        private IHp _hpSystem;
        
        public IHp HpSystem => _hpSystem;
        private AttackData AttackData { get; set; }
        public EntityData EntityData { get; private set; }
        public bool IsDead { get; private set; }

        private bool CanReceivePunch { get; set; }
        public virtual bool CanBeAttacked => !IsDead && CanReceivePunch;

        public event Action<float> OnReceiveDamage;
        public event Action<AttackObject> OnDied;
        
        private void OnValidate()
        {
            rb ??= GetComponent<Rigidbody2D>();
            ValidateHook();
        }
        protected virtual void ValidateHook() { }

        public void Init(EntityData entityData)
        {
            _hpSystem = new Hp.Hp(entityData.MaxHp);
            Debug.Log($"maxHp : {entityData.MaxHp}");
            EntityData = entityData;

            InitHook();
        }

        protected virtual void InitHook() { }

        private void Knockback(AttackData attackArgs)
        {
            StartKnockbackHook();
            var direction = GetDirection(attackArgs);

            var knockback = 1;

            AsyncKnockback(direction, knockback).Forget();
        }

        protected virtual void StartKnockbackHook() { }

        private async UniTask AsyncKnockback(Vector2 direction, float knockback)
        {
            var target = rb.position + direction * knockback;
            while (Vector3.Distance(rb.position, target) > 0.7f)
            {
                rb.MovePosition(Vector3.Lerp(rb.position, target, 0.2f));
                await UniTask.NextFrame();
            }
        }

        private static Vector3 GetDirection(AttackData attackArgs)
        {
            Vector3 direction = attackArgs.Attacker.transform.forward;
            direction.y = 0;
            direction.Normalize();
            return direction;
        }

        public void TriggerDamage(AttackData attackData)
        {
            AttackData = attackData;
            ReceiveDamage(attackData);
        }


        private void ReceiveDamage(AttackData attackData)
        {
            ReceiveDamageHook(attackData);
            
            DecreaseHp();
            OnReceiveDamage?.Invoke(attackData.Damage);
            Knockback(AttackData);

            if (_hpSystem.Current == 0)
                Die();
        }

        private void DecreaseHp() => _hpSystem.Damage(AttackData.Damage);

        protected virtual void ReceiveDamageHook(AttackData attackData) { }
        
        private void Die()
        {
            DieHook();
            
            animator.SetTrigger(AnimatorId.DiedTrigger);
            OnDied?.Invoke(this);
            IsDead = true;
            DestroyOnTimerAsync().Forget();
        }

        protected virtual void DieHook() { }

        private async UniTask DestroyOnTimerAsync()
        {
            await UniTask.Delay(destroyOnDeathTimerSeconds * 1000, cancellationToken: gameObject.GetCancellationTokenOnDestroy());
            Destroy(gameObject);
        }
    }
}