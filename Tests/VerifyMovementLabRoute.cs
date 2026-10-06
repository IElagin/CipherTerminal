using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Enemies;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Hero;
using Assets._Project.Develop.Runtime.Gameplay.Common;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Mono;
using Assets._Project.Develop.Runtime.Gameplay.Features.EnemiesFeature;
using Assets._Project.Develop.Runtime.Gameplay.Features.MovementFeature;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Gameplay.MovementLab;
using Assets._Project.Develop.Runtime.UI.MovementLab;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Assets._Project.Develop.Runtime.UI.MainMenu;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class VerifyMovementLabRoute
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const int ExpectedCharacters = 3;
    private const int TimeoutSeconds = 30;
    private const float MovementSeconds = 0.5f;
    private const float StopSeconds = 0.1f;

    public static Task<string> Main() => Run().AsTask();

    private static async UniTask<string> Run()
    {
        if (EditorApplication.isPlaying == false || SceneManager.GetActiveScene().name != Scenes.MainMenu)
            throw new InvalidOperationException("Run once in Play Mode after EntryPoint reaches MainMenu");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        await EnterGameplay(timeout.Token);
        MovementLabBootstrap bootstrap = Object.FindAnyObjectByType<MovementLabBootstrap>();
        var session = (MovementLabSession)typeof(MovementLabBootstrap).GetField("_session", PrivateInstance).GetValue(bootstrap);
        RequireCharacterCount(ExpectedCharacters);
        RequireSingleAudioListener();
        await VerifyCharacterConfigs(session, timeout.Token);
        await VerifySimultaneousMovement(session, timeout.Token);
        var firstViews = Object.FindObjectsByType<MonoEntity>().Select(view => view.GetEntityId()).ToArray();
        MovementLabScreenView hud = Object.FindAnyObjectByType<MovementLabScreenView>();
        ((Button)typeof(MovementLabScreenView).GetField("_backButton", PrivateInstance).GetValue(hud)).onClick.Invoke();
        await UniTask.WaitUntil(() => SceneManager.GetActiveScene().name == Scenes.MainMenu
            && Object.FindAnyObjectByType<MainMenuScreenView>() != null, cancellationToken: timeout.Token);
        RequireCharacterCount(0);
        RequireSingleAudioListener();

        var previousLife = (EntitiesLifeContext)typeof(MovementLabSession).GetField("_lifeContext", PrivateInstance).GetValue(session);

        if (((IList)typeof(EntitiesLifeContext).GetField("_entities", PrivateInstance).GetValue(previousLife)).Count != 0)
            throw new InvalidOperationException("Scene scope must release the previous entities");

        var views = GetViews(session);

        if (views.Count != 0)
            throw new InvalidOperationException("Previous session retained Unity views");

        await EnterGameplay(timeout.Token);
        RequireCharacterCount(ExpectedCharacters);
        RequireSingleAudioListener();
        bool reusedOldView = Object.FindObjectsByType<MonoEntity>().Any(view => firstViews.Contains(view.GetEntityId()));

        if (reusedOldView)
            throw new InvalidOperationException("Old character view survived the scene transition");

        bootstrap = Object.FindAnyObjectByType<MovementLabBootstrap>();
        session = (MovementLabSession)typeof(MovementLabBootstrap).GetField("_session", PrivateInstance).GetValue(bootstrap);
        await VerifyTeleporter(session, timeout.Token);

        const string result = "PASS L4/L5 ROUTE AND CONFIGS: Hero/Basic/Fast settings from provider, independent mutable entity data; MainMenu lab button -> MovementLab (3 views, both L4 characters move and rotate from queued keyboard input) -> HUD back button -> MainMenu (0 views, scope released entities and map cleared) -> lab (3 fresh views) -> teleporter T/Space, energy, area damage, deaths and self-release. Physical keyboard acceptance remains manual.";
        Debug.Log(result);

        return result;
    }

    private static void RequireSingleAudioListener()
    {
        int count = Object.FindObjectsByType<AudioListener>().Count(listener => listener.isActiveAndEnabled);

        if (count != 1)
            throw new InvalidOperationException($"Expected one active AudioListener, found {count}");
    }

    private static async UniTask VerifyCharacterConfigs(MovementLabSession session, CancellationToken token)
    {
        const float speedChange = 1;
        var factory = (EntitiesFactory)typeof(MovementLabSession).GetField("_entitiesFactory", PrivateInstance).GetValue(session);
        var provider = (ConfigsProviderService)typeof(EntitiesFactory).GetField("_configsProvider", PrivateInstance).GetValue(factory);
        var life = (EntitiesLifeContext)typeof(MovementLabSession).GetField("_lifeContext", PrivateInstance).GetValue(session);
        var views = GetViews(session);
        var hero = (Entity)typeof(MovementLabSession).GetField("_hero", PrivateInstance).GetValue(session);
        Entity basic = views.Keys.Cast<Entity>()
            .Single(entity => entity.HasComponent<RigidbodyComponent>() && entity.HasComponent<MoveSpeed>());
        HeroConfig heroConfig = provider.GetConfig<HeroConfig>();
        EnemiesListConfig enemies = provider.GetConfig<EnemiesListConfig>();
        EnemyConfig basicConfig = enemies.GetConfigFor(EnemyTypes.Basic);
        EnemyConfig fastConfig = enemies.GetConfigFor(EnemyTypes.Fast);
        RequireSpeeds(hero, heroConfig.MoveSpeed, heroConfig.RotationSpeed);
        RequireSpeeds(basic, basicConfig.MoveSpeed, basicConfig.RotationSpeed);

        if (hero.HasComponent<CharacterControllerComponent>() == false || hero.HasComponent<RigidbodyComponent>())
            throw new InvalidOperationException("Hero must use CharacterController without Rigidbody");

        if (basicConfig.MoveSpeed == fastConfig.MoveSpeed)
            throw new InvalidOperationException("Basic and Fast fixtures must exercise different config values");

        var fastPosition = new Vector3(6, 0, 0);
        var secondFastPosition = new Vector3(8, 0, 0);
        Entity fast = factory.CreateEnemy(EnemyTypes.Fast, fastPosition);
        Entity secondFast = factory.CreateEnemy(EnemyTypes.Fast, secondFastPosition);
        RequireSpeeds(fast, fastConfig.MoveSpeed, fastConfig.RotationSpeed);
        RequireSpeeds(secondFast, fastConfig.MoveSpeed, fastConfig.RotationSpeed);
        float configuredSpeed = fastConfig.MoveSpeed;
        fast.MoveSpeed.Value += speedChange;

        if (secondFast.MoveSpeed.Value != configuredSpeed || fastConfig.MoveSpeed != configuredSpeed)
            throw new InvalidOperationException("Mutable entity speed leaked into another entity or shared config");

        life.Release(fast);
        life.Release(secondFast);
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
        await UniTask.WaitUntil(() => Object.FindObjectsByType<MonoEntity>().Length == ExpectedCharacters,
            cancellationToken: token);
        Debug.Log("PASS CHARACTER CONFIGS: Hero uses CharacterController; Basic/Fast enum selects provider settings; repeated Fast spawns have independent data");
    }

    private static async UniTask VerifySimultaneousMovement(MovementLabSession session, CancellationToken token)
    {
        var hero = (Entity)typeof(MovementLabSession).GetField("_hero", PrivateInstance).GetValue(session);
        var enemy = (Entity)typeof(MovementLabSession).GetField("_enemy", PrivateInstance).GetValue(session);
        Transform heroTransform = hero.Transform;
        Transform enemyTransform = enemy.Rigidbody.transform;
        Vector3 heroStart = heroTransform.position;
        Vector3 enemyStart = enemyTransform.position;
        Keyboard keyboard = Keyboard.current;
        bool temporaryKeyboard = keyboard == null;
        if (temporaryKeyboard) keyboard = InputSystem.AddDevice<Keyboard>();

        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            await UniTask.Delay(TimeSpan.FromSeconds(MovementSeconds), cancellationToken: token);
            if (heroTransform.position.x <= heroStart.x + .1f || enemyTransform.position.x <= enemyStart.x + .1f)
                throw new InvalidOperationException("Both characters must move from the same input");
            if (Vector3.Dot(heroTransform.forward, Vector3.right) < .9f || Vector3.Dot(enemyTransform.forward, Vector3.right) < .9f)
                throw new InvalidOperationException("Both character rotation implementations must turn right");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            await UniTask.Delay(TimeSpan.FromSeconds(MovementSeconds), cancellationToken: token);
            if (heroTransform.position.z <= heroStart.z + .1f || enemyTransform.position.z <= enemyStart.z + .1f)
                throw new InvalidOperationException("Both characters must respond to the changed direction");
            if (Vector3.Dot(heroTransform.forward, Vector3.forward) < .9f || Vector3.Dot(enemyTransform.forward, Vector3.forward) < .9f)
                throw new InvalidOperationException("Both characters must turn toward the changed direction");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await UniTask.Delay(TimeSpan.FromSeconds(StopSeconds), cancellationToken: token);
            if (hero.MoveDirection.Value != Vector3.zero || enemy.MoveDirection.Value != Vector3.zero
                || enemy.Rigidbody.linearVelocity.sqrMagnitude > .001f)
                throw new InvalidOperationException("Releasing input must stop both characters");
            Debug.Log("PASS BOTH CHARACTERS: queued D/W input moves and rotates both; release stops both");
        }
        finally
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            if (temporaryKeyboard) InputSystem.RemoveDevice(keyboard);
        }
    }

    private static async UniTask VerifyTeleporter(MovementLabSession session, CancellationToken token)
    {
        const float teleportCost = 25;
        const float areaDamage = 30;
        const float testDamage = 50;
        var hero = (Entity)typeof(MovementLabSession).GetField("_hero", PrivateInstance).GetValue(session);
        var enemy = (Entity)typeof(MovementLabSession).GetField("_enemy", PrivateInstance).GetValue(session);
        var teleporter = (Entity)typeof(MovementLabSession).GetField("_teleporter", PrivateInstance).GetValue(session);

        if (teleporter.HasComponent<MoveSpeed>() || teleporter.HasComponent<RotationSpeed>())
            throw new InvalidOperationException("Teleporter must not move or rotate");

        if (teleporter.Rigidbody.GetComponent<Collider>().excludeLayers.value != UnityLayersMask())
            throw new InvalidOperationException("Teleporter collider must ignore other characters");

        // The random destination is not controlled; widen only the damage radius so every live target is inside it.
        teleporter.AreaDamageRadius.Value = 100;
        Vector3 start = teleporter.Rigidbody.position;
        teleporter.EnergyRegenerationCurrentTime.Value = teleporter.EnergyRegenerationInitialTime.Value;
        await PressKey(Key.T, token);
        float teleportedDistance = Vector3.Distance(start, teleporter.Rigidbody.position);

        if (teleportedDistance > teleporter.TeleportRadius.Value + .01f || teleporter.CurrentEnergy.Value != 100 - teleportCost)
            throw new InvalidOperationException("T must teleport within radius N and spend X energy");

        if (hero.CurrentHealth.Value != 100 - areaDamage || enemy.CurrentHealth.Value != 100 - areaDamage
            || teleporter.CurrentHealth.Value != 100)
            throw new InvalidOperationException("Teleport must damage every other entity in radius M once and skip itself");

        await UniTask.WaitUntil(() => teleporter.CurrentEnergy.Value != 100 - teleportCost, cancellationToken: token);

        if (teleporter.CurrentEnergy.Value != 100 - teleportCost + 10)
            throw new InvalidOperationException("Energy must regenerate by 10% of max every Z seconds");

        for (int index = 0; index < 4; index++)
            teleporter.TeleportRequest.Invoke();

        if (teleporter.CurrentEnergy.Value >= teleportCost)
            throw new InvalidOperationException("Teleport must stop when energy is below its cost");

        await UniTask.DelayFrame(2, cancellationToken: token);

        if (hero.IsDead.Value == false || enemy.IsDead.Value == false)
            throw new InvalidOperationException("Repeated area damage must kill both L4 characters");

        await PressKey(Key.Space, token);

        if (teleporter.CurrentHealth.Value != 100 - testDamage)
            throw new InvalidOperationException("Space must damage the teleporter");

        await PressKey(Key.Space, token);

        if (teleporter.IsDead.Value == false)
            throw new InvalidOperationException("Teleporter must die at zero health");

        teleporter.TeleportRequest.Invoke();
        await UniTask.WaitUntil(() => Object.FindObjectsByType<MonoEntity>().Length == 0, cancellationToken: token);

        if (GetViews(session).Count != 0)
            throw new InvalidOperationException("Dead entities must release their views");

        Debug.Log("PASS TELEPORTER: no movement components, ghost collider, T teleports within N for X energy, area damage D skips self, 10% regeneration every Z, low-energy refusal, Space damage, deaths and self-release of all three entities");
    }

    private static int UnityLayersMask() => 1 << LayerMask.NameToLayer("Characters");

    private static async UniTask PressKey(Key key, CancellationToken token)
    {
        Keyboard keyboard = Keyboard.current;
        bool temporaryKeyboard = keyboard == null;
        if (temporaryKeyboard) keyboard = InputSystem.AddDevice<Keyboard>();

        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            await UniTask.DelayFrame(3, cancellationToken: token);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await UniTask.DelayFrame(3, cancellationToken: token);
        }
        finally
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            if (temporaryKeyboard) InputSystem.RemoveDevice(keyboard);
        }
    }

    private static IDictionary GetViews(MovementLabSession session)
    {
        var factory = (EntitiesFactory)typeof(MovementLabSession).GetField("_entitiesFactory", PrivateInstance).GetValue(session);
        var monoFactory = (MonoEntitiesFactory)typeof(EntitiesFactory).GetField("_monoEntitiesFactory", PrivateInstance).GetValue(factory);

        return (IDictionary)typeof(MonoEntitiesFactory).GetField("_views", PrivateInstance).GetValue(monoFactory);
    }

    private static void RequireSpeeds(Entity entity, float moveSpeed, float rotationSpeed)
    {
        if (entity.MoveSpeed.Value != moveSpeed || entity.RotationSpeed.Value != rotationSpeed)
            throw new InvalidOperationException("Entity speeds differ from the selected provider config");
    }

    private static async UniTask EnterGameplay(CancellationToken token)
    {
        MainMenuScreenView menu = Object.FindAnyObjectByType<MainMenuScreenView>();
        ((Button)typeof(MainMenuScreenView).GetField("_movementLabButton", PrivateInstance).GetValue(menu)).onClick.Invoke();
        await UniTask.WaitUntil(() => SceneManager.GetActiveScene().name == Scenes.MovementLab
            && Object.FindObjectsByType<MonoEntity>().Length == ExpectedCharacters,
            cancellationToken: token);
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
    }

    private static void RequireCharacterCount(int expected)
    {
        int actual = Object.FindObjectsByType<MonoEntity>().Length;

        if (actual != expected)
            throw new InvalidOperationException($"Expected {expected} character views, found {actual}");
    }
}
