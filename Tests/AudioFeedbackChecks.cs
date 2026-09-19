using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using TMPro;
using Assets._Project.Develop.Runtime.Gameplay;
using Assets._Project.Develop.Runtime.Gameplay.Presentation;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Meta.Presentation;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataRepository;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.KeysStorage;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.Serializers;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using Assets._Project.Develop.Runtime.Utilities.Input;

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
            var repository = new DelayedWriteRepository(new LocalFileDataRepository(scratchPath, "json"));
            var saveLoad = new SaveLoadService(new JsonSerializer(), new MapDataKeysStorage(),
                repository);
            var provider = new PlayerDataProvider(saveLoad, rules);
            var wallet = new WalletService(new Dictionary<CurrencyTypes, ReactiveVariable<int>>
            {
                [CurrencyTypes.Gold] = new ReactiveVariable<int>()
            });
            var statistics = new StatisticsService();
            progressService = new PlayerProgressService(provider, wallet, statistics,
                rules, default);
            await progressService.Initialize();
            statistics.RecordWin();
            statistics.RecordLoss();

            menuRoot = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/UI/MainMenuScreen.prefab");
            var controller = menuRoot.AddComponent<MainMenuController>();
            SetField(controller, "_view", menuRoot.GetComponentInChildren<TerminalView>(true));
            var walletView = menuRoot.GetComponentInChildren<WalletPanelView>(true);
            SetField(controller, "_walletView", walletView);
            SetField(controller, "_keyboard", menuRoot.AddComponent<TerminalKeyboard>());
            var audio = new RecordingAudio();
            controller.Initialize(null, audio, progressService);
            controller.Run();
            const string recordedCount = "1";
            Require(Text(walletView, "_gold") == initialGold.ToString() &&
                    Text(walletView, "_wins") == recordedCount && Text(walletView, "_losses") == recordedCount,
                "menu renders initial wallet and service-owned statistics", results);
            MethodInfo resetStatisticsMethod = typeof(MainMenuController).GetMethod("ResetStatisticsAsync", PrivateInstanceFlags);

            UniTaskCompletionSource resetWrite = repository.DelayNextWrite();
            Task resetOperation = ((UniTask)resetStatisticsMethod.Invoke(controller, null)).AsTask();
            const string resetValue = "0";
            bool resetRenderedBeforeSave = resetOperation.IsCompleted == false &&
                                          Text(walletView, "_gold") == resetValue &&
                                          Text(walletView, "_wins") == resetValue &&
                                          Text(walletView, "_losses") == resetValue &&
                                          walletView.ResetButton.interactable;
            resetWrite.TrySetResult();
            await resetOperation;
            Require(resetRenderedBeforeSave,
                "paid reset updates UI and interaction before the write completes", results);
            Require(progressService.Snapshot.Gold == 0, "successful reset exercises the actual paid-reset path", results);
            Require(Text(walletView, "_gold") == resetValue && Text(walletView, "_wins") == resetValue &&
                    Text(walletView, "_losses") == resetValue && walletView.ResetButton.interactable,
                "paid reset updates the existing prefab UI and restores interaction", results);
            RequireSingleCue(audio, AudioCue.Key,
                "successful statistics reset uses button feedback without victory voice", results);

            audio.Cues.Clear();
            await (UniTask)resetStatisticsMethod.Invoke(controller, null);
            RequireSingleCue(audio, AudioCue.Error,
                "insufficient funds use error feedback without victory voice", results);

            string savePath = Path.Combine(scratchPath, "player.json");
            string beforeFailedWrite = File.ReadAllText(savePath);
            UniTaskCompletionSource failedWrite = repository.DelayNextWrite();
            Task<ProgressOperationResult> wonOperation = progressService.RecordResultAsync(SequenceState.Won).AsTask();
            const string oneWin = "1";
            bool resultRenderedBeforeSave = wonOperation.IsCompleted == false &&
                                           Text(walletView, "_gold") == winReward.ToString() &&
                                           Text(walletView, "_wins") == oneWin;
            failedWrite.TrySetException(new IOException("Expected isolated delayed write failure"));
            ProgressOperationResult failedResult = await wonOperation;
            Require(resultRenderedBeforeSave,
                "round result updates UI before the write completes", results);
            Require(failedResult.Status == ProgressOperationStatus.SavedInMemoryOnly &&
                    progressService.Snapshot.Equals(new ProgressSnapshot(winReward, 1, 0)) &&
                    Text(walletView, "_status") == PlayerProgressService.SaveErrorMessage &&
                    File.ReadAllText(savePath) == beforeFailedWrite,
                "late write failure preserves progress and prior file while notifying UI", results);

            UniTaskCompletionSource retryWrite = repository.DelayNextWrite();
            Task<ProgressOperationResult> lostOperation = progressService.RecordResultAsync(SequenceState.Lost).AsTask();
            int expectedGold = winReward - lossPenalty;
            bool retryRenderedBeforeSave = lostOperation.IsCompleted == false &&
                                          Text(walletView, "_gold") == expectedGold.ToString() &&
                                          Text(walletView, "_status") == PlayerProgressService.SaveErrorMessage;
            retryWrite.TrySetResult();
            ProgressOperationResult retryResult = await lostOperation;
            Require(retryRenderedBeforeSave,
                "next result updates values while retaining the unresolved save error", results);
            var savedProgress = new JsonSerializer().Deserialize<PlayerData>(File.ReadAllText(savePath));
            Require(retryResult.Status == ProgressOperationStatus.Completed && progressService.Error == null &&
                    Text(walletView, "_status") == "> Система готова" &&
                    savedProgress.Gold == expectedGold && savedProgress.Wins == 1 && savedProgress.Losses == 1,
                "successful retry persists accumulated progress and clears the UI error", results);

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

    private static string Text(object owner, string fieldName)
        => ((TMP_Text)owner.GetType().GetField(fieldName, PrivateInstanceFlags).GetValue(owner)).text;

    private static void Require(bool condition, string label, List<string> results)
    {
        if (condition == false)
            throw new Exception(label);

        results.Add("PASS: " + label);
    }

    private sealed class DelayedWriteRepository : IDataRepository
    {
        private readonly IDataRepository _inner;
        private UniTaskCompletionSource _nextWrite;

        public DelayedWriteRepository(IDataRepository inner) => _inner = inner;

        public UniTaskCompletionSource DelayNextWrite()
        {
            _nextWrite = new UniTaskCompletionSource();
            return _nextWrite;
        }

        public UniTask<string> ReadAsync(string key, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(key, cancellationToken);

        public async UniTask WriteAsync(string key, string serializedData,
            CancellationToken cancellationToken = default)
        {
            UniTaskCompletionSource pendingWrite = _nextWrite;
            _nextWrite = null;

            if (pendingWrite != null)
                await pendingWrite.Task;

            await _inner.WriteAsync(key, serializedData, cancellationToken);
        }

        public UniTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
            => _inner.ExistsAsync(key, cancellationToken);
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
