using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.AI.States;
using Assets._Project.Develop.Runtime.Gameplay.Features.InputFeature;
using Assets._Project.Develop.Runtime.Utilities.Conditions;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using Assets._Project.Develop.Runtime.Utilities.Timer;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AI
{
    public class BrainsFactory
    {
        private const float RandomTeleportCooldown = 2f;
        private const float SmartTeleportCooldown = 3f;
        private const float MinEnergyPercentForSmartTeleport = 40f;
        private const float SmartTeleportDistanceToTarget = 1.5f;

        private readonly IObjectResolver _container;
        private readonly TimerServiceFactory _timerServiceFactory;
        private readonly AIBrainsContext _brainsContext;
        private readonly IInputService _inputService;
        private readonly EntitiesLifeContext _entitiesLifeContext;

        public BrainsFactory(IObjectResolver container)
        {
            _container = container;
            _timerServiceFactory = _container.Resolve<TimerServiceFactory>();
            _brainsContext = _container.Resolve<AIBrainsContext>();
            _inputService = _container.Resolve<IInputService>();
            _entitiesLifeContext = _container.Resolve<EntitiesLifeContext>();
        }

        public StateMachineBrain CreateManualHeroBrain(Entity entity)
        {
            AIStateMachine combatState = CreateManualAttackStateMachine(entity);

            PlayerInputMovementState movementState = new PlayerInputMovementState(entity, _inputService);

            ICondition fromMovementToCombatStateCondition = new FuncCondition(() => _inputService.Direction == Vector3.zero);
            ICondition fromCombatToMovementStateCondition = new FuncCondition(() => _inputService.Direction != Vector3.zero);

            AIStateMachine behaviour = new AIStateMachine();

            behaviour.AddState(movementState);
            behaviour.AddState(combatState);

            behaviour.AddTransition(movementState, combatState, fromMovementToCombatStateCondition);
            behaviour.AddTransition(combatState, movementState, fromCombatToMovementStateCondition);

            StateMachineBrain brain = new StateMachineBrain(behaviour);
            _brainsContext.SetFor(entity, brain);

            return brain;
        }

        public StateMachineBrain CreateRandomTeleportBrain(Entity entity)
        {
            AIStateMachine stateMachine = CreateRandomTeleportStateMachine(entity);
            StateMachineBrain brain = new StateMachineBrain(stateMachine);

            _brainsContext.SetFor(entity, brain);

            return brain;
        }

        public StateMachineBrain CreateSmartTeleportBrain(Entity entity)
        {
            AIStateMachine behaviour = CreateSmartTeleportStateMachine(entity);

            FindTargetState findTargetState = new FindTargetState(new LowestHealthTargetSelector(entity), _entitiesLifeContext, entity);
            AIParallelState parallelState = new AIParallelState(findTargetState, behaviour);

            // ParallelState does not dispose nested machines, so the root owns the behaviour with its timer.
            AIStateMachine rootStateMachine = new AIStateMachine(new List<IDisposable> { behaviour });
            rootStateMachine.AddState(parallelState);

            StateMachineBrain brain = new StateMachineBrain(rootStateMachine);
            _brainsContext.SetFor(entity, brain);

            return brain;
        }

        private AIStateMachine CreateManualAttackStateMachine(Entity entity)
        {
            PlayerInputRotationState rotationState = new PlayerInputRotationState(entity, _inputService);

            AttackTriggerState attackTriggerState = new AttackTriggerState(entity);

            ICondition canAttack = entity.CanStartAttack;

            ICompositeCondition fromRotationToAttackCondition = new CompositeCondition()
                .Add(new FuncCondition(() => _inputService.IsAttackPressed))
                .Add(canAttack);

            ReactiveVariable<bool> inAttackProcess = entity.InAttackProcess;

            ICondition fromAttackToRotationStateCondition = new FuncCondition(() => inAttackProcess.Value == false);

            AIStateMachine stateMachine = new AIStateMachine();

            stateMachine.AddState(rotationState);
            stateMachine.AddState(attackTriggerState);

            stateMachine.AddTransition(rotationState, attackTriggerState, fromRotationToAttackCondition);
            stateMachine.AddTransition(attackTriggerState, rotationState, fromAttackToRotationStateCondition);

            return stateMachine;
        }

        private AIStateMachine CreateRandomTeleportStateMachine(Entity entity)
        {
            List<IDisposable> disposables = new List<IDisposable>();

            EmptyState waitState = new EmptyState();

            TeleportTriggerState teleportState = new TeleportTriggerState(entity);

            TimerService waitTimer = _timerServiceFactory.Create(RandomTeleportCooldown);
            disposables.Add(waitTimer);
            disposables.Add(waitState.Entered.Subscribe(waitTimer.Restart));

            ICompositeCondition fromWaitToTeleportCondition = new CompositeCondition()
                .Add(new FuncCondition(() => waitTimer.IsOver))
                .Add(entity.CanTeleport);

            ICondition fromTeleportToWaitCondition = new FuncCondition(() => true);

            AIStateMachine stateMachine = new AIStateMachine(disposables);

            stateMachine.AddState(waitState);
            stateMachine.AddState(teleportState);

            stateMachine.AddTransition(waitState, teleportState, fromWaitToTeleportCondition);
            stateMachine.AddTransition(teleportState, waitState, fromTeleportToWaitCondition);

            return stateMachine;
        }

        private AIStateMachine CreateSmartTeleportStateMachine(Entity entity)
        {
            List<IDisposable> disposables = new List<IDisposable>();

            EmptyState waitState = new EmptyState();

            TeleportToTargetState teleportState = new TeleportToTargetState(entity, SmartTeleportDistanceToTarget);

            EmptyState energyRestoreState = new EmptyState();

            TimerService waitTimer = _timerServiceFactory.Create(SmartTeleportCooldown);
            disposables.Add(waitTimer);
            disposables.Add(waitState.Entered.Subscribe(waitTimer.Restart));

            ReactiveVariable<float> currentEnergy = entity.CurrentEnergy;
            ReactiveVariable<float> maxEnergy = entity.MaxEnergy;
            ReactiveVariable<Entity> currentTarget = entity.CurrentTarget;

            ICondition energyIsLowCondition = new FuncCondition(
                () => currentEnergy.Value < maxEnergy.Value * MinEnergyPercentForSmartTeleport / 100f);

            ICondition energyIsRestoredCondition = new FuncCondition(
                () => currentEnergy.Value >= maxEnergy.Value * MinEnergyPercentForSmartTeleport / 100f);

            ICompositeCondition fromWaitToTeleportCondition = new CompositeCondition()
                .Add(new FuncCondition(() => waitTimer.IsOver))
                .Add(new FuncCondition(() => currentTarget.Value != null))
                .Add(entity.CanTeleport);

            ICondition fromTeleportToWaitCondition = new FuncCondition(() => true);

            AIStateMachine stateMachine = new AIStateMachine(disposables);

            stateMachine.AddState(waitState);
            stateMachine.AddState(teleportState);
            stateMachine.AddState(energyRestoreState);

            stateMachine.AddTransition(waitState, energyRestoreState, energyIsLowCondition);
            stateMachine.AddTransition(waitState, teleportState, fromWaitToTeleportCondition);
            stateMachine.AddTransition(teleportState, waitState, fromTeleportToWaitCondition);
            stateMachine.AddTransition(energyRestoreState, waitState, energyIsRestoredCondition);

            return stateMachine;
        }
    }
}
