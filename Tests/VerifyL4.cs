using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets._Project.Develop.Editor;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Mono;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.AssetsManagement;
using Assets._Project.Develop.Runtime.Utilities.Layers;
using UnityEditor;
using UnityEngine;

public static class VerifyL4
{
    private const float Tick = 0.02f;
    private const string APIPath = "Assets/_Project/Develop/Runtime/Gameplay/EntitiesCore/Generated/EntityAPI.cs";

    public static string Main()
    {
        Require(EditorUtility.scriptCompilationFailed == false, "current scripts compile");
        var life = new EntitiesLifeContext();
        var views = new MonoEntitiesFactory(new ResourcesAssetLoader(), life);
        views.Initialize();
        var events = new List<string>();
        var first = new ProbeSystem(events);
        var late = new ProbeSystem(events);
        var entity = new Entity().AddSystem(first);
        life.Added += added => events.Add("added");
        life.Released += released => events.Add("released");
        entity.Update(Tick);
        Require(first.UpdateCount == 0, "no update before initialization");
        life.Add(entity);
        Require(events.SequenceEqual(new[] { "init", "added" }), "init precedes Added");
        entity.AddSystem(late);
        Require(late.InitCount == 1, "late system initializes once");
        first.OnTick = () => life.Release(entity);
        life.FixedUpdate(Tick);
        Require(first.FixedCount == 1 && first.UpdateCount == 0, "fixed callbacks are separate");
        life.Update(Tick);
        Require(first.UpdateCount == 1 && late.UpdateCount == 1, "release waits for all systems");
        Require(events.TakeLast(3).SequenceEqual(new[] { "dispose", "dispose", "released" }), "dispose precedes Released");
        entity.Update(Tick);
        entity.FixedUpdate(Tick);
        life.Update(Tick);
        life.FixedUpdate(Tick);
        Require(first.UpdateCount == 1 && first.FixedCount == 1, "disposed entity does not tick");
        life.Dispose();
        views.Dispose();
        Require(first.DisposeCount == 1 && late.DisposeCount == 1, "release and context teardown do not double-dispose");

        string before = File.ReadAllText(APIPath);
        DateTime timestamp = File.GetLastWriteTimeUtc(APIPath);
        EntityAPIGenerator.Generate();
        EntityAPIGenerator.Generate();
        Require(before == File.ReadAllText(APIPath) && timestamp == File.GetLastWriteTimeUtc(APIPath), "generator is idempotent");
        Require(typeof(Entity).GetMethod("AddMoveDirection", Type.EmptyTypes) != null, "reactive no-arg API");
        Require(typeof(Entity).GetMethod("AddRigidbody", Type.EmptyTypes) == null, "no automatic Unity Object construction");
        VerifyPrefab("Entities/RigidbodyCharacter", true);
        VerifyPrefab("Entities/CharacterControllerCharacter", false);
        const string result = "PASS L4: current compilation, headless entity lifecycle, late init, deferred release, fixed callbacks, disposal, generator idempotence and both Resources prefabs";
        Debug.Log(result);

        return result;
    }

    public static string VerifyLayers()
    {
        const int layerCount = 32;
        const int fieldsPerLayer = 2;
        const string prefix = "Layer";
        const string outputPath = "Assets/_Project/Develop/Runtime/Utilities/Layers/UnityLayers.cs";
        Require(EditorUtility.scriptCompilationFailed == false, "generated layers compile");
        string before = File.ReadAllText(outputPath);
        DateTime timestamp = File.GetLastWriteTimeUtc(outputPath);
        UnityLayersGenerator.Generate();
        UnityLayersGenerator.Generate();
        Require(before == File.ReadAllText(outputPath) && timestamp == File.GetLastWriteTimeUtc(outputPath), "layer generation is idempotent");

        var fields = typeof(UnityLayers).GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(field => field.Name);
        var actualLayers = new HashSet<int>();
        var expectedLayers = Enumerable.Range(0, layerCount).Where(layer => string.IsNullOrEmpty(LayerMask.LayerToName(layer)) == false).ToArray();

        foreach (FieldInfo field in fields.Values)
        {
            Require(field.IsInitOnly && field.FieldType == typeof(int), "layer fields are static readonly int");
            string maskName = "LayerMask" + field.Name.Substring(prefix.Length);

            if (fields.TryGetValue(maskName, out FieldInfo maskField) == false)
                continue;

            int layer = (int)field.GetValue(null);
            Require(actualLayers.Add(layer), "one pair per named layer");
            Require((int)maskField.GetValue(null) == (1 << layer), "mask matches layer bit");
        }

        Require(actualLayers.SetEquals(expectedLayers) && fields.Count == expectedLayers.Length * fieldsPerLayer, "all and only named layers are generated");
        Require(UnityLayers.LayerIgnoreRaycast == LayerMask.NameToLayer("Ignore Raycast"), "spaces are removed from identifier, not from layer lookup");

        MethodInfo getName = typeof(UnityLayersGenerator).GetMethod("GetUniqueName", BindingFlags.NonPublic | BindingFlags.Static);
        var usedNames = new HashSet<string>();
        string[] names = { "A B", "AB", "MaskAB", "123", "class", "!!!" };

        for (int index = 0; index < names.Length; index++)
        {
            string name = (string)getName.Invoke(null, new object[] { names[index], index, usedNames });
            Require(name.Length > 0 && name.All(character => char.IsLetterOrDigit(character) || character == '_'), "valid identifier suffix");
        }

        Require(usedNames.Count == names.Length * fieldsPerLayer, "unique layer/mask identifiers after normalization");
        const string result = "PASS UNITY LAYERS: generated assembly, all named layers and masks, valid unique identifiers, repeat generation leaves content and timestamp unchanged";
        Debug.Log(result);

        return result;
    }

    private static void VerifyPrefab(string path, bool usesRigidbody)
    {
        MonoEntity prefab = new ResourcesAssetLoader().Load<MonoEntity>(path);
        Require(prefab != null, "prefab loads through ResourcesAssetLoader: " + path);
        Require(prefab.GetComponentsInChildren<Collider>().Length == 1, "only one character collider");
        Rigidbody rigidbody = prefab.GetComponent<Rigidbody>();
        CharacterController controller = prefab.GetComponent<CharacterController>();

        if (usesRigidbody)
        {
            Require(rigidbody != null && controller == null, "Rigidbody prefab composition");
            Require(rigidbody.useGravity == false && rigidbody.isKinematic == false, "flat scene dynamic body");
            Require((rigidbody.constraints & RigidbodyConstraints.FreezeRotationY) == 0, "Y rotation enabled");
        }
        else
        {
            Require(controller != null && rigidbody == null, "CharacterController prefab composition");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (condition == false)
            throw new InvalidOperationException("L4 verification failed: " + message);
    }

    private sealed class ProbeSystem : IInitializableSystem, IUpdatableSystem, IFixedUpdatableSystem, IDisposableSystem
    {
        private readonly List<string> _events;

        public Action OnTick;
        public int InitCount;
        public int UpdateCount;
        public int FixedCount;
        public int DisposeCount;

        public ProbeSystem(List<string> events) => _events = events;

        public void OnInit(Entity entity)
        {
            InitCount++;
            _events.Add("init");
        }

        public void OnUpdate(float deltaTime)
        {
            UpdateCount++;
            OnTick?.Invoke();
        }

        public void OnFixedUpdate(float deltaTime) => FixedCount++;

        public void OnDispose()
        {
            DisposeCount++;
            _events.Add("dispose");
        }
    }
}
