using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Assets._Project.Develop.Runtime.Gameplay;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataRepository;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.KeysStorage;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.Serializers;

namespace Assets._Project.Develop.Editor
{
    public static class ProgressChecks
    {
        public static async Task<string> RunAsync()
        {
            string rootPath = Path.Combine(Path.GetTempPath(),
                "cipher-progress-" + Guid.NewGuid().ToString("N"));
            var results = new List<string>();
            const int initialGold = 100;
            const int winReward = 10;
            const int lossPenalty = 5;
            const int resetCost = 25;
            var rules = new EconomyRules(initialGold, winReward, lossPenalty, resetCost);

            try
            {
                await CheckFirstStartAndReloadAsync(rootPath, rules, results);
                await CheckOutcomeAccountingAsync(rootPath, rules, results);
                await CheckGameplayTrackingAsync(rootPath, rules, results);
                await CheckPaidResetAsync(rootPath, results);
                await CheckInvalidSavesAsync(rootPath, rules, results);
                await CheckSaveRetryAsync(rootPath, rules, results);
                await CheckCancellationAsync(rootPath, rules, results);
                await CheckOverflowAsync(rootPath, rules, results);
                await CheckExceptionBoundariesAsync(rootPath, rules, results);
                await CheckInternalParticipantErrorsAsync(rootPath, rules, results);
                CheckSerialization(results);

                return string.Join("\n", results);
            }
            finally
            {
                if (Directory.Exists(rootPath))
                    Directory.Delete(rootPath, true);
            }
        }

        private static async Task CheckFirstStartAndReloadAsync(string rootPath, EconomyRules rules, List<string> results)
        {
            var expectedInitialProgress = new ProgressSnapshot(rules.InitialGold, 0, 0);
            string firstStartPath = Path.Combine(rootPath, "first-start");
            PlayerProgressService firstStart = CreateService(firstStartPath, rules);
            await firstStart.Initialize();
            Require(firstStart.IsReady && firstStart.Snapshot.Equals(expectedInitialProgress),
                "missing save initializes configured progress", results);
            Require(File.Exists(Path.Combine(firstStartPath, "player.json")),
                "first start persists player.json", results);
            firstStart.Dispose();

            PlayerProgressService reloaded = CreateService(firstStartPath, rules);
            await reloaded.Initialize();
            Require(reloaded.Snapshot.Equals(expectedInitialProgress),
                "valid save reloads all values", results);
            reloaded.Dispose();
        }

        private static async Task CheckOutcomeAccountingAsync(string rootPath, EconomyRules rules, List<string> results)
        {
            const int floorTestInitialGold = 3;
            const int expectedOutcomeChangeCount = 4;
            string outcomePath = Path.Combine(rootPath, "outcomes");
            PlayerProgressService outcomeProgress = CreateService(outcomePath, new EconomyRules(floorTestInitialGold, rules.WinReward, rules.LossPenalty, rules.StatisticsResetCost));
            await outcomeProgress.Initialize();
            int outcomeChangeCount = 0;
            outcomeProgress.Changed += _ => outcomeChangeCount++;
            await outcomeProgress.RecordResultAsync(SequenceState.Won);
            await outcomeProgress.RecordResultAsync(SequenceState.Lost);
            await outcomeProgress.RecordResultAsync(SequenceState.Lost);
            await outcomeProgress.RecordResultAsync(SequenceState.Lost);
            var expectedOutcomeProgress = new ProgressSnapshot(0, 1, 3);
            Require(outcomeProgress.Snapshot.Equals(expectedOutcomeProgress) && outcomeChangeCount == expectedOutcomeChangeCount,
                "reward, penalty, floor and one snapshot event per result stay coherent", results);
            outcomeProgress.Dispose();
        }

        private static async Task CheckGameplayTrackingAsync(string rootPath, EconomyRules rules, List<string> results)
        {
            const int levelNumber = 1;
            const int oneResult = 1;
            const string target = "1";
            SequenceState[] outcomes = { SequenceState.Won, SequenceState.Lost };

            foreach (SequenceState outcome in outcomes)
            {
                string storePath = Path.Combine(rootPath, "tracked-" + outcome);
                using PlayerProgressService progress = CreateService(storePath, rules);
                await progress.Initialize();
                var session = new SequenceSession(new SequenceGenerator(new Random()));
                session.Initialize(target, target.Length);
                var loop = new GameplayLoop(session, new GameplayInputArgs(levelNumber, SequenceMode.Digits));
                using var tracker = new GameplayProgressTracker(loop, progress);
                tracker.Run();
                loop.Run();
                char character = outcome == SequenceState.Won ? '1' : '9';
                loop.Submit(character);
                loop.Submit(character);
                loop.Submit(' ');

                int expectedGold = outcome == SequenceState.Won
                    ? rules.InitialGold + rules.WinReward
                    : rules.InitialGold - rules.LossPenalty;
                int expectedWins = outcome == SequenceState.Won ? oneResult : 0;
                int expectedLosses = outcome == SequenceState.Lost ? oneResult : 0;
                var expected = new ProgressSnapshot(expectedGold, expectedWins, expectedLosses);

                // This file repository completes its UniTask synchronously.
                Require(progress.Snapshot.Equals(expected),
                    "gameplay tracker records " + outcome + " once despite later terminal input", results);

                using PlayerProgressService reloaded = CreateService(storePath, rules);
                await reloaded.Initialize();
                Require(reloaded.Snapshot.Equals(expected),
                    "tracked " + outcome + " is persisted once", results);
            }
        }

        private static async Task CheckPaidResetAsync(string rootPath, List<string> results)
        {
            const int exactResetGold = 25;
            const int resetScenarioPenalty = 1;
            string resetPath = Path.Combine(rootPath, "reset");
            PlayerProgressService resetProgress = CreateService(resetPath, new EconomyRules(exactResetGold, 0, resetScenarioPenalty, exactResetGold));
            await resetProgress.Initialize();
            await resetProgress.RecordResultAsync(SequenceState.Won);
            ProgressOperationResult exactReset = await resetProgress.ResetStatisticsAsync();
            Require(exactReset.Status == ProgressOperationStatus.Completed &&
                    resetProgress.Snapshot.Equals(new ProgressSnapshot(0, 0, 0)),
                "statistics reset deducts exact configured price only", results);
            ProgressSnapshot beforeRejectedReset = resetProgress.Snapshot;
            ProgressOperationResult rejectedReset = await resetProgress.ResetStatisticsAsync();
            Require(rejectedReset.Status == ProgressOperationStatus.InsufficientGold &&
                    resetProgress.Snapshot.Equals(beforeRejectedReset),
                "insufficient reset leaves the coherent snapshot unchanged", results);
            resetProgress.Dispose();
        }

        private static async Task CheckInvalidSavesAsync(string rootPath, EconomyRules rules, List<string> results)
        {
            string invalidPath = Path.Combine(rootPath, "invalid");
            Directory.CreateDirectory(invalidPath);
            string invalidFile = Path.Combine(invalidPath, "player.json");
            const string invalidJson = "{\"schemaVersion\":1,\"gold\":-1,\"wins\":2,\"losses\":3}";
            File.WriteAllText(invalidFile, invalidJson);
            PlayerProgressService invalidSaveProgress = CreateService(invalidPath, rules);
            await invalidSaveProgress.Initialize();
            Require(invalidSaveProgress.IsReady == false && File.ReadAllText(invalidFile) == invalidJson,
                "invalid existing save is rejected without overwrite", results);
            invalidSaveProgress.Dispose();

            string unsupportedPath = Path.Combine(rootPath, "unsupported");
            Directory.CreateDirectory(unsupportedPath);
            File.WriteAllText(Path.Combine(unsupportedPath, "player.json"),
                "{\"schemaVersion\":2,\"gold\":100,\"wins\":0,\"losses\":0}");
            PlayerProgressService unsupportedSchemaProgress = CreateService(unsupportedPath, rules);
            await unsupportedSchemaProgress.Initialize();
            Require(unsupportedSchemaProgress.IsReady == false, "unsupported save schema is rejected", results);
            unsupportedSchemaProgress.Dispose();

            string missingFieldPath = Path.Combine(rootPath, "missing-field");
            Directory.CreateDirectory(missingFieldPath);
            File.WriteAllText(Path.Combine(missingFieldPath, "player.json"),
                "{\"schemaVersion\":1,\"gold\":100,\"wins\":0}");
            PlayerProgressService missingField = CreateService(missingFieldPath, rules);
            await missingField.Initialize();
            Require(missingField.IsReady == false, "missing typed save field is rejected", results);
            missingField.Dispose();

            string corruptPath = Path.Combine(rootPath, "corrupt");
            Directory.CreateDirectory(corruptPath);
            string corruptFile = Path.Combine(corruptPath, "player.json");
            const string corruptJson = "{not-json";
            File.WriteAllText(corruptFile, corruptJson);
            PlayerProgressService corruptSaveProgress = CreateService(corruptPath, rules);
            await corruptSaveProgress.Initialize();
            string nullPath = Path.Combine(rootPath, "null");
            Directory.CreateDirectory(nullPath);
            string nullFile = Path.Combine(nullPath, "player.json");
            File.WriteAllText(nullFile, "null");
            PlayerProgressService nullSave = CreateService(nullPath, rules);
            await nullSave.Initialize();
            Require(corruptSaveProgress.IsReady == false && File.ReadAllText(corruptFile) == corruptJson &&
                    nullSave.IsReady == false && File.ReadAllText(nullFile) == "null",
                "malformed and null saves are rejected without overwrite", results);
            corruptSaveProgress.Dispose();
            nullSave.Dispose();
        }

        private static async Task CheckSaveRetryAsync(string rootPath, EconomyRules rules, List<string> results)
        {
            string saveFailurePath = Path.Combine(rootPath, "save-failure");
            var realRepository = new LocalFileDataRepository(saveFailurePath, "json");
            PlayerProgressService seedProgress = CreateService(realRepository, rules);
            await seedProgress.Initialize();
            seedProgress.Dispose();
            var failOnceRepository = new FailOnceWriteRepository(realRepository);
            PlayerProgressService saveFailureProgress = CreateService(failOnceRepository, rules);
            await saveFailureProgress.Initialize();
            ProgressOperationResult memoryOnly = await saveFailureProgress.RecordResultAsync(SequenceState.Won);
            PlayerData storedBeforeFailure = new JsonSerializer().Deserialize<PlayerData>(
                File.ReadAllText(Path.Combine(saveFailurePath, "player.json")));
            var expectedMemoryProgress = new ProgressSnapshot(110, 1, 0);
            Require(memoryOnly.Status == ProgressOperationStatus.SavedInMemoryOnly &&
                    saveFailureProgress.Snapshot.Equals(expectedMemoryProgress) &&
                    storedBeforeFailure.Gold == rules.InitialGold && storedBeforeFailure.Wins == 0 &&
                    saveFailureProgress.Error == PlayerProgressService.SaveErrorMessage,
                "save failure preserves memory, prior file and visible error", results);
            ProgressOperationResult retry = await saveFailureProgress.RecordResultAsync(SequenceState.Lost);
            bool retryClearedError = string.IsNullOrEmpty(saveFailureProgress.Error);
            saveFailureProgress.Dispose();
            PlayerProgressService afterRetry = CreateService(realRepository, rules);
            await afterRetry.Initialize();
            var expectedReloadedProgress = new ProgressSnapshot(105, 1, 1);
            Require(retry.Status == ProgressOperationStatus.Completed && retryClearedError &&
                    afterRetry.Snapshot.Equals(expectedReloadedProgress),
                "next operation persists accumulated progress and clears save error", results);
            afterRetry.Dispose();
        }

        private static async Task CheckCancellationAsync(string rootPath, EconomyRules rules, List<string> results)
        {
            string cancellationPath = Path.Combine(rootPath, "cancellation");
            var cancellationRepository = new LocalFileDataRepository(cancellationPath, "json");
            using var cancellation = new CancellationTokenSource();
            PlayerProgressService cancelledProgress = CreateService(cancellationRepository, rules, cancellation.Token);
            await cancelledProgress.Initialize();
            ProgressSnapshot beforeCancellation = cancelledProgress.Snapshot;
            string diskBeforeCancellation = File.ReadAllText(Path.Combine(cancellationPath, "player.json"));
            cancellation.Cancel();
            bool cancellationObserved = false;

            try
            {
                await cancelledProgress.RecordResultAsync(SequenceState.Won);
            }
            catch (OperationCanceledException)
            {
                cancellationObserved = true;
            }

            Require(cancellationObserved && cancelledProgress.Snapshot.Equals(beforeCancellation) &&
                    File.ReadAllText(Path.Combine(cancellationPath, "player.json")) == diskBeforeCancellation,
                "pre-cancellation leaves memory and stored progress unchanged", results);
            cancelledProgress.Dispose();
        }

        private static async Task CheckOverflowAsync(string rootPath, EconomyRules rules, List<string> results)
        {
            string overflowPath = Path.Combine(rootPath, "overflow");
            Directory.CreateDirectory(overflowPath);
            const int overflowValue = int.MaxValue;
            string overflowJson = "{\"schemaVersion\":1,\"gold\":" + overflowValue +
                                  ",\"wins\":" + overflowValue + ",\"losses\":0}";
            File.WriteAllText(Path.Combine(overflowPath, "player.json"), overflowJson);
            PlayerProgressService overflowProgress = CreateService(overflowPath, rules);
            await overflowProgress.Initialize();
            ProgressSnapshot beforeOverflow = overflowProgress.Snapshot;
            ProgressOperationResult overflowResult = await overflowProgress.RecordResultAsync(SequenceState.Won);
            Require(overflowResult.Status == ProgressOperationStatus.Failed &&
                    overflowProgress.Snapshot.Equals(beforeOverflow),
                "overflow cannot partially mutate progress", results);
            overflowProgress.Dispose();
        }

        private static async Task CheckExceptionBoundariesAsync(string rootPath, EconomyRules rules,
            List<string> results)
        {
            string boundaryPath = Path.Combine(rootPath, "exception-boundary");
            var realRepository = new LocalFileDataRepository(boundaryPath, "json");

            using (PlayerProgressService seed = CreateService(realRepository, rules))
                await seed.Initialize();

            Exception[] programmingErrors =
            {
                new InvalidOperationException("Injected programming error"),
                new NullReferenceException("Injected missing internal dependency"),
                new OperationCanceledException("Injected operation cancellation")
            };

            foreach (Exception expected in programmingErrors)
            {
                string exceptionName = expected.GetType().Name;
                var loadRepository = new ErrorRepository(realRepository) { ReadError = expected };
                using (PlayerProgressService loadProgress = CreateService(loadRepository, rules))
                {
                    Exception observed = await ObserveExceptionAsync(() => loadProgress.Initialize());
                    Require(MatchesExpectedException(observed, expected) && loadProgress.Error == null,
                        "load propagates " + exceptionName + " without a save-error message", results);
                }

                var writeRepository = new ErrorRepository(realRepository) { WriteError = expected };
                using (PlayerProgressService writeProgress = CreateService(writeRepository, rules))
                {
                    await writeProgress.Initialize();
                    Exception observed = await ObserveExceptionAsync(async () =>
                    {
                        await writeProgress.RecordResultAsync(SequenceState.Won);
                    });
                    Require(MatchesExpectedException(observed, expected) && writeProgress.Error == null,
                        "save propagates " + exceptionName + " without a save-error result", results);
                }
            }

            Exception[] storageErrors =
            {
                new IOException("Injected disk failure"),
                new InvalidDataException("Injected invalid saved document"),
                new UnauthorizedAccessException("Injected denied file access"),
                new Newtonsoft.Json.JsonSerializationException("Injected serialization failure")
            };

            foreach (Exception expected in storageErrors)
            {
                string exceptionName = expected.GetType().Name;
                var loadRepository = new ErrorRepository(realRepository) { ReadError = expected };
                using (PlayerProgressService loadProgress = CreateService(loadRepository, rules))
                {
                    await loadProgress.Initialize();
                    Require(loadProgress.IsReady == false && loadProgress.Error == PlayerProgressService.LoadErrorMessage,
                        "load reports expected " + exceptionName, results);
                }

                var writeRepository = new ErrorRepository(realRepository) { WriteError = expected };
                using (PlayerProgressService writeProgress = CreateService(writeRepository, rules))
                {
                    await writeProgress.Initialize();
                    ProgressOperationResult result = await writeProgress.RecordResultAsync(SequenceState.Won);
                    Require(result.Status == ProgressOperationStatus.SavedInMemoryOnly &&
                            writeProgress.Snapshot.Gold == rules.InitialGold + rules.WinReward &&
                            writeProgress.Error == PlayerProgressService.SaveErrorMessage,
                        "save retains memory after expected " + exceptionName, results);
                }
            }
        }

        private static async Task CheckInternalParticipantErrorsAsync(string rootPath, EconomyRules rules,
            List<string> results)
        {
            var repository = new LocalFileDataRepository(Path.Combine(rootPath, "internal-participants"), "json");

            using (PlayerProgressService seed = CreateService(repository, rules))
                await seed.Initialize();

            Exception[] failures =
            {
                new Newtonsoft.Json.JsonSerializationException("Internal participant failed"),
                new InvalidDataException("Internal participant failed")
            };

            foreach (Exception expected in failures)
            {
                string exceptionName = expected.GetType().Name;
                var participant = new FailingParticipant(expected);

                using (PlayerProgressService progress = CreateService(repository, rules,
                    prepareProvider: provider => provider.RegisterReader(participant)))
                {
                    Exception observed = await ObserveExceptionAsync(() => progress.Initialize());
                    Require(ReferenceEquals(observed, expected) && progress.Error == null,
                        "internal reader " + exceptionName + " is not a load failure", results);
                }

                using (PlayerProgressService progress = CreateService(repository, rules,
                    prepareProvider: provider => provider.RegisterWriter(participant)))
                {
                    await progress.Initialize();
                    Exception observed = await ObserveExceptionAsync(async () =>
                    {
                        await progress.RecordResultAsync(SequenceState.Won);
                    });
                    Require(ReferenceEquals(observed, expected) && progress.Error == null,
                        "internal writer " + exceptionName + " is not a save failure", results);
                }

                var newRepository = new LocalFileDataRepository(
                    Path.Combine(rootPath, "internal-first-save-" + exceptionName), "json");

                using (PlayerProgressService progress = CreateService(newRepository, rules,
                    prepareProvider: provider => provider.RegisterWriter(participant)))
                {
                    Exception observed = await ObserveExceptionAsync(() => progress.Initialize());
                    Require(ReferenceEquals(observed, expected) && progress.Error == null,
                        "first-save internal " + exceptionName + " propagates through Initialize", results);
                }
            }

            using (PlayerProgressService progress = CreateService(repository, rules,
                prepareProvider: provider => provider.RegisterWriter(new InvalidStateWriter())))
            {
                await progress.Initialize();
                Exception observed = await ObserveExceptionAsync(async () =>
                {
                    await progress.RecordResultAsync(SequenceState.Won);
                });
                Require(observed is Newtonsoft.Json.JsonSerializationException && progress.Error == null,
                    "invalid in-memory state is not mislabeled as an external save failure", results);
            }
        }

        private static bool MatchesExpectedException(Exception observed, Exception expected)
        {
            if (expected is OperationCanceledException expectedCancellation)
                return observed is OperationCanceledException observedCancellation &&
                       observedCancellation.CancellationToken == expectedCancellation.CancellationToken;

            return ReferenceEquals(observed, expected);
        }

        private static async Task<Exception> ObserveExceptionAsync(Func<UniTask> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static void CheckSerialization(List<string> results)
        {
            var serializer = new JsonSerializer();
            var sampleData = new PlayerData(100, 2, 3);
            string serialized = serializer.Serialize(sampleData);
            Require(serialized.Contains("$type") == false, "JSON persistence emits no polymorphic type metadata", results);
        }

        private static PlayerProgressService CreateService(string rootPath, EconomyRules rules)
            => CreateService(new LocalFileDataRepository(rootPath, "json"), rules);

        private static PlayerProgressService CreateService(IDataRepository repository, EconomyRules rules,
            CancellationToken cancellationToken = default, Action<PlayerDataProvider> prepareProvider = null)
        {
            var wallet = new WalletService();
            var statistics = new StatisticsService();
            var serializer = new JsonSerializer();
            var saveLoad = new SaveLoadService(serializer, new MapDataKeysStorage(), repository);
            var provider = new PlayerDataProvider(saveLoad, rules);
            var progress = new PlayerProgressService(provider, wallet, statistics, rules, cancellationToken);
            prepareProvider?.Invoke(provider);

            return progress;
        }

        private static void Require(bool condition, string label, ICollection<string> results)
        {
            if (condition == false)
                throw new InvalidOperationException(label);

            results.Add("PASS: " + label);
        }

        private sealed class FailingParticipant : IDataReader<PlayerData>, IDataWriter<PlayerData>
        {
            private readonly Exception _failure;

            public FailingParticipant(Exception failure) => _failure = failure;

            public void ReadFrom(PlayerData data) => throw _failure;

            public void WriteTo(PlayerData data) => throw _failure;
        }

        private sealed class InvalidStateWriter : IDataWriter<PlayerData>
        {
            private const int InvalidGold = -1;

            public void WriteTo(PlayerData data) => data.Gold = InvalidGold;
        }

        private sealed class ErrorRepository : IDataRepository
        {
            private readonly IDataRepository _inner;

            public Exception ReadError;
            public Exception WriteError;

            public ErrorRepository(IDataRepository inner) => _inner = inner;

            public UniTask<string> ReadAsync(string key, CancellationToken cancellationToken = default)
            {
                if (ReadError != null)
                    throw ReadError;

                return _inner.ReadAsync(key, cancellationToken);
            }

            public UniTask WriteAsync(string key, string serializedData, CancellationToken cancellationToken = default)
            {
                if (WriteError != null)
                    throw WriteError;

                return _inner.WriteAsync(key, serializedData, cancellationToken);
            }

            public UniTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
                => _inner.ExistsAsync(key, cancellationToken);
        }

        private sealed class FailOnceWriteRepository : IDataRepository
        {
            private readonly IDataRepository _inner;
            private bool _failNextWrite = true;

            public FailOnceWriteRepository(IDataRepository inner)
            {
                _inner = inner;
            }

            public UniTask<string> ReadAsync(string key, CancellationToken cancellationToken = default)
                => _inner.ReadAsync(key, cancellationToken);

            public UniTask WriteAsync(string key, string serializedData,
                CancellationToken cancellationToken = default)
            {
                if (_failNextWrite)
                {
                    _failNextWrite = false;
                    throw new IOException("Expected isolated write failure");
                }

                return _inner.WriteAsync(key, serializedData, cancellationToken);
            }

            public UniTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
                => _inner.ExistsAsync(key, cancellationToken);
        }
    }
}
