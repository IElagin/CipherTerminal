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
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.UI.MainMenu;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.UI.ResetStatistics;
using UnityEngine.UI;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataRepository;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.KeysStorage;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.Serializers;
using Assets._Project.Develop.Runtime.Utilities.Reactive;

public static class AudioFeedbackChecks
{
    private const BindingFlags PrivateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    public static async Task<string> RunAsync()
    {
        string scratchPath = Path.Combine(Path.GetTempPath(), "cipher-audio-check-" + Guid.NewGuid().ToString("N"));
        GameObject menuRoot = null;
        GameObject popupRoot = null;
        PlayerProgressService progress = null;
        ProgressPanelPresenter panel = null;
        ResetStatisticsPopupPresenter popup = null;
        var results = new List<string>();
        try
        {
            var rules = new EconomyRules(100, 10, 5, 25);
            var repository = new DelayedWriteRepository(new LocalFileDataRepository(scratchPath, "json"));
            var saveLoad = new SaveLoadService(new JsonSerializer(), new MapDataKeysStorage(), repository);
            var provider = new PlayerDataProvider(saveLoad, rules);
            var wallet = new WalletService(new Dictionary<CurrencyTypes, ReactiveVariable<int>>
            {
                [CurrencyTypes.Gold] = new ReactiveVariable<int>()
            });
            var statistics = new StatisticsService();
            progress = new PlayerProgressService(provider, wallet, statistics, rules, default);
            await progress.Initialize();
            statistics.RecordWin();
            statistics.RecordLoss();
            menuRoot = PrefabUtility.LoadPrefabContents("Assets/_Project/Resources/UI/MainMenu/MainMenuScreen.prefab");
            popupRoot = PrefabUtility.LoadPrefabContents("Assets/_Project/Resources/UI/ResetStatistics/ResetStatisticsPopup.prefab");
            ProgressPanelView panelView = menuRoot.GetComponentInChildren<ProgressPanelView>(true);
            ResetStatisticsPopupView popupView = popupRoot.GetComponent<ResetStatisticsPopupView>();
            panel = new ProgressPanelPresenter(panelView, progress);
            panel.Initialize();
            panel.SetInteractable(false);
            var audio = new RecordingAudio();
            int closeRequests = 0;
            popup = new ResetStatisticsPopupPresenter(popupView, progress, audio);
            popup.CloseRequest += p => closeRequests++;
            popup.Initialize();
            popup.RequestCancel();
            Require(closeRequests == 1 && progress.Snapshot.Equals(new ProgressSnapshot(100, 1, 1)),
                "cancel requests close without charging or resetting statistics", results);
            popup.Dispose();
            popup = new ResetStatisticsPopupPresenter(popupView, progress, audio);
            closeRequests = 0;
            popup.CloseRequest += p => closeRequests++;
            popup.Initialize();
            Require(Text(panelView, "_gold") == "100" && Text(panelView, "_wins") == "1" && Text(panelView, "_losses") == "1",
                "permanent panel renders a coherent initial snapshot", results);
            UniTaskCompletionSource write = repository.DelayNextWrite();
            Task pending = Confirm(popup).AsTask();
            Require(pending.IsCompleted == false && progress.Snapshot.Equals(new ProgressSnapshot(75, 0, 0)) &&
                Text(panelView, "_gold") == "75" && Text(panelView, "_wins") == "0" && Text(panelView, "_losses") == "0",
                "confirmed reset updates the panel before persistence finishes", results);
            Require(Button(popupView, "_confirm").interactable == false && Button(popupView, "_cancel").interactable == false &&
                Button(panelView, "_resetButton").interactable == false,
                "progress notification cannot unlock popup or covered menu during saving", results);
            await Confirm(popup);
            popup.RequestCancel();
            Require(closeRequests == 0 && progress.Snapshot.Gold == 75,
                "repeat confirmation and Escape are blocked during saving", results);
            write.TrySetResult();
            await pending;
            Require(closeRequests == 1 && progress.Snapshot.Gold == 75,
                "successful paid reset closes exactly once", results);
            RequireSingleCue(audio, AudioCue.Key, "successful reset plays Key without victory voice", results);
            popup.Dispose();

            audio.Cues.Clear();
            popup = new ResetStatisticsPopupPresenter(popupView, progress, audio);
            popup.Initialize();
            string savePath = Path.Combine(scratchPath, "player.json");
            string beforeFailure = File.ReadAllText(savePath);
            UniTaskCompletionSource failedWrite = repository.DelayNextWrite();
            pending = Confirm(popup).AsTask();
            failedWrite.TrySetException(new IOException("Expected isolated delayed write failure"));
            await pending;
            Require(progress.Snapshot.Gold == 50 && Text(panelView, "_gold") == "50" &&
                Text(panelView, "_status") == PlayerProgressService.SaveErrorMessage && File.ReadAllText(savePath) == beforeFailure,
                "failed write retains one reset in memory, prior file and visible save error", results);
            Require(Button(popupView, "_confirm").interactable == false && Button(popupView, "_cancel").interactable &&
                Text(popupView, "_status").Contains("Статистика сброшена"),
                "memory-only success permits closing but never another charge", results);
            await Confirm(popup);
            Require(progress.Snapshot.Gold == 50, "repeated handler after failed save does not charge twice", results);
            RequireSingleCue(audio, AudioCue.Error, "save failure uses Error feedback", results);
            popup.Dispose();
            await progress.RecordResultAsync(SequenceState.Won);
            var saved = new JsonSerializer().Deserialize<PlayerData>(File.ReadAllText(savePath));
            Require(saved.Gold == 60 && saved.Wins == 1 && saved.Losses == 0 && progress.Error == null &&
                Text(panelView, "_status") == "> Система готова",
                "next ordinary save persists memory state and clears panel error", results);

            wallet.Spend(CurrencyTypes.Gold, 50);
            audio.Cues.Clear();
            popup = new ResetStatisticsPopupPresenter(popupView, progress, audio);
            popup.Initialize();
            Require(Button(popupView, "_confirm").interactable == false && Text(popupView, "_status").Contains("Не хватает 15"),
                "insufficient balance disables confirmation and explains missing gold", results);
            await Confirm(popup);
            Require(progress.Snapshot.Equals(new ProgressSnapshot(10, 1, 0)), "insufficient reset leaves all progress intact", results);
            RequireSingleCue(audio, AudioCue.Error, "insufficient reset cannot play victory feedback", results);
            popup.Dispose();

            wallet.Add(CurrencyTypes.Gold, 100);
            popup = new ResetStatisticsPopupPresenter(popupView, progress, audio);
            popup.Initialize();
            UniTaskCompletionSource teardownWrite = repository.DelayNextWrite();
            pending = Confirm(popup).AsTask();
            popup.Dispose();
            PrefabUtility.UnloadPrefabContents(popupRoot);
            popupRoot = null;
            popup = null;
            teardownWrite.TrySetResult();
            await pending;
            Require(progress.Snapshot.Gold == 85, "project-owned reset completes after popup disposal without touching released view", results);

            var gameplay = new GameplayScreenPresenter(null, null, null, null, audio, null);
            MethodInfo evaluated = typeof(GameplayScreenPresenter).GetMethod("OnInputEvaluated", PrivateInstanceFlags);
            audio.Cues.Clear();
            evaluated.Invoke(gameplay, new object[] { InputEvaluation.Correct });
            evaluated.Invoke(gameplay, new object[] { InputEvaluation.Incorrect });
            Require(audio.Cues.Count == 2 && audio.Cues[0] == AudioCue.Key && audio.Cues[1] == AudioCue.Error,
                "ordinary input and loss keep existing sound cues", results);
            audio.Cues.Clear();
            evaluated.Invoke(gameplay, new object[] { InputEvaluation.Completed });
            RequireSingleCue(audio, AudioCue.Success, "only winning input requests victory voice", results);
            return string.Join("\n", results);
        }
        finally
        {
            popup?.Dispose();
            panel?.Dispose();
            if (popupRoot != null) PrefabUtility.UnloadPrefabContents(popupRoot);
            if (menuRoot != null) PrefabUtility.UnloadPrefabContents(menuRoot);
            progress?.Dispose();
            if (Directory.Exists(scratchPath)) Directory.Delete(scratchPath, true);
        }
    }

    private static UniTask Confirm(ResetStatisticsPopupPresenter popup)
        => (UniTask)typeof(ResetStatisticsPopupPresenter).GetMethod("ConfirmAsync", PrivateInstanceFlags).Invoke(popup, null);

    private static Button Button(object view, string field)
        => (Button)view.GetType().GetField(field, PrivateInstanceFlags).GetValue(view);

    private static void RequireSingleCue(RecordingAudio audio, AudioCue expected, string label, List<string> results)
    {
        const int singleCueCount = 1;
        Require(audio.Cues.Count == singleCueCount && audio.Cues[0] == expected, label, results);
    }

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
