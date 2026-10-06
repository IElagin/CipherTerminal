using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Animation
{
    public class CharacterAnimationSystem : IInitializableSystem, IDisposableSystem
    {
        private static readonly int IsMovingKey = Animator.StringToHash("IsMoving");
        private static readonly int IsDeadKey = Animator.StringToHash("IsDead");

        private Animator _animator;

        private ReactiveVariable<bool> _isMoving;
        private ReactiveVariable<bool> _isDead;

        private IDisposable _isMovingDisposable;
        private IDisposable _isDeadDisposable;

        public void OnInit(Entity entity)
        {
            _animator = entity.Animator;
            _isMoving = entity.IsMoving;
            _isDead = entity.IsDead;

            _isMovingDisposable = _isMoving.Subscribe(OnIsMovingChanged);
            _isDeadDisposable = _isDead.Subscribe(OnIsDeadChanged);
        }

        public void OnDispose()
        {
            _isMovingDisposable.Dispose();
            _isDeadDisposable.Dispose();
        }

        private void OnIsMovingChanged(bool oldValue, bool isMoving) => _animator.SetBool(IsMovingKey, isMoving);

        private void OnIsDeadChanged(bool oldValue, bool isDead) => _animator.SetBool(IsDeadKey, isDead);
    }
}
