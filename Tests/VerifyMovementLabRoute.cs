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
    private const int ExpectedCharacters = 2;
    private const int TimeoutSeconds = 20;

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

        if ((bool)typeof(MovementLabSession).GetField("_isRunning", PrivateInstance).GetValue(session))
            throw new InvalidOperationException("Previous session is still running");

        var factory = (MonoEntitiesFactory)typeof(MovementLabSession).GetField("_monoEntitiesFactory", PrivateInstance).GetValue(session);
        var views = (IDictionary)typeof(MonoEntitiesFactory).GetField("_views", PrivateInstance).GetValue(factory);

        if (views.Count != 0)
            throw new InvalidOperationException("Previous session retained Unity views");

        await EnterGameplay(timeout.Token);
        RequireCharacterCount(ExpectedCharacters);
        RequireSingleAudioListener();
        bool reusedOldView = Object.FindObjectsByType<MonoEntity>().Any(view => firstViews.Contains(view.GetEntityId()));

        if (reusedOldView)
            throw new InvalidOperationException("Old character view survived the scene transition");

        const string result = "PASS L4 ROUTE AND CONFIGS: Hero/Basic/Fast settings from provider, independent mutable entity data; MainMenu lab button -> MovementLab (2 views, both move and rotate from queued keyboard input) -> HUD back button -> MainMenu (0 views, session stopped and map cleared) -> lab (2 fresh views). Physical keyboard acceptance remains manual.";
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
        var monoFactory = (MonoEntitiesFactory)typeof(MovementLabSession).GetField("_monoEntitiesFactory", PrivateInstance).GetValue(session);
        var views = (IDictionary)typeof(MonoEntitiesFactory).GetField("_views", PrivateInstance).GetValue(monoFactory);
        var hero = (Entity)typeof(MovementLabSession).GetField("_hero", PrivateInstance).GetValue(session);
        Entity basic = views.Keys.Cast<Entity>().Single(entity => entity.HasComponent<RigidbodyComponent>());
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
            await UniTask.DelayFrame(25, cancellationToken: token);
            if (heroTransform.position.x <= heroStart.x + .1f || enemyTransform.position.x <= enemyStart.x + .1f)
                throw new InvalidOperationException("Both characters must move from the same input");
            if (Vector3.Dot(heroTransform.forward, Vector3.right) < .9f || Vector3.Dot(enemyTransform.forward, Vector3.right) < .9f)
                throw new InvalidOperationException("Both character rotation implementations must turn right");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            await UniTask.DelayFrame(25, cancellationToken: token);
            if (heroTransform.position.z <= heroStart.z + .1f || enemyTransform.position.z <= enemyStart.z + .1f)
                throw new InvalidOperationException("Both characters must respond to the changed direction");
            if (Vector3.Dot(heroTransform.forward, Vector3.forward) < .9f || Vector3.Dot(enemyTransform.forward, Vector3.forward) < .9f)
                throw new InvalidOperationException("Both characters must turn toward the changed direction");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await UniTask.DelayFrame(5, cancellationToken: token);
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
