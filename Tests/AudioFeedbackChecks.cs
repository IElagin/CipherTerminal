using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Assets._Project.Develop.Runtime.Gameplay;
using Assets._Project.Develop.Runtime.Gameplay.Presentation;
using Assets._Project.Develop.Runtime.Meta.Presentation;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataRepository;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.KeysStorage;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.Serializers;

public static class AudioFeedbackChecks
{
    private const BindingFlags PrivateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    public static async Task<string> RunAsync()
    {
        string scratchPath = Path.Combine(Path.GetTempPath(), "cipher-audio-check-" + Guid.NewGuid().ToString("N"));
        GameObject menuRoot = null;
        PlayerProgressService progressService = null;
        var results = new List<string>();

        try
        {
            const int initialGold = 25;
            const int winReward = 10;
            const int lossPenalty = 5;
            const int resetCost = 25;
            var rules = new EconomyRules(initialGold, winReward, lossPenalty, resetCost);
            var saveLoad = new SaveLoadService(new JsonSerializer(), new MapDataKeysStorage(),
                new LocalFileDataRepository(scratchPath, "json"));
            var provider = new PlayerDataProvider(saveLoad, rules);
            progressService = new PlayerProgressService(provider, new WalletService(), new StatisticsService(),
                rules, default);
            await progressService.Initialize();

            menuRoot = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/UI/MainMenuScreen.prefab");
            var controller = menuRoot.AddComponent<MainMenuController>();
            SetField(controller, "_view", menuRoot.GetComponentInChildren<TerminalView>(true));
            SetField(controller, "_walletView", menuRoot.GetComponentInChildren<WalletPanelView>(true));
            var audio = new RecordingAudio();
            controller.Initialize(null, audio, progressService);
            MethodInfo resetStatisticsMethod = typeof(MainMenuController).GetMethod("ResetStatisticsAsync", PrivateInstanceFlags);

            await (UniTask)resetStatisticsMethod.Invoke(controller, null);
            Require(progressService.Snapshot.Gold == 0, "successful reset exercises the actual paid-reset path", results);
            RequireSingleCue(audio, AudioCue.Key,
                "successful statistics reset uses button feedback without victory voice", results);

            audio.Cues.Clear();
            await (UniTask)resetStatisticsMethod.Invoke(controller, null);
            RequireSingleCue(audio, AudioCue.Error,
                "insufficient funds use error feedback without victory voice", results);

            var gameplay = menuRoot.AddComponent<GameplayController>();
            gameplay.Initialize(null, null, audio);
            MethodInfo inputEvaluatedMethod = typeof(GameplayController).GetMethod("OnInputEvaluated", PrivateInstanceFlags);
            audio.Cues.Clear();
            inputEvaluatedMethod.Invoke(gameplay, new object[] { InputEvaluation.Correct });
            inputEvaluatedMethod.Invoke(gameplay, new object[] { InputEvaluation.Incorrect });
            const int ordinaryFeedbackCount = 2;
            const int errorCueIndex = 1;
            Require(audio.Cues.Count == ordinaryFeedbackCount && audio.Cues[0] == AudioCue.Key && audio.Cues[errorCueIndex] == AudioCue.Error,
                "ordinary input and loss do not request victory voice", results);

            audio.Cues.Clear();
            inputEvaluatedMethod.Invoke(gameplay, new object[] { InputEvaluation.Completed });
            RequireSingleCue(audio, AudioCue.Success,
                "completed sequence still requests victory voice", results);
            return string.Join("\n", results);
        }
        finally
        {
            if (menuRoot != null)
                PrefabUtility.UnloadPrefabContents(menuRoot);

            progressService?.Dispose();

            if (Directory.Exists(scratchPath))
                Directory.Delete(scratchPath, true);
        }
    }

    private static void RequireSingleCue(RecordingAudio audio, AudioCue expected, string label, List<string> results)
    {
        const int singleCueCount = 1;
        Require(audio.Cues.Count == singleCueCount && audio.Cues[0] == expected, label, results);
    }

    private static void SetField(object owner, string fieldName, object value)
        => owner.GetType().GetField(fieldName, PrivateInstanceFlags).SetValue(owner, value);

    private static void Require(bool condition, string label, List<string> results)
    {
        if (condition == false)
            throw new Exception(label);

        results.Add("PASS: " + label);
    }

    private sealed class RecordingAudio : IAudioService
    {
        public readonly List<AudioCue> Cues = new List<AudioCue>();

        public void Initialize(AudioCatalog catalog)
        {
        }

        public void Play(AudioCue cue) => Cues.Add(cue);
    }
}
