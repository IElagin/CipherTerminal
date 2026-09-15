using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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
            var rules = new EconomyRules(100, 10, 5, 25);

            try
            {
                string firstStartPath = Path.Combine(rootPath, "first-start");
                PlayerProgressService firstStart = CreateService(firstStartPath, rules);
                await firstStart.InitializeAsync();
                Require(firstStart.IsReady && firstStart.Snapshot.Equals(new ProgressSnapshot(100, 0, 0)),
                    "missing save initializes configured progress", results);
                Require(File.Exists(Path.Combine(firstStartPath, "player.json")),
                    "first start persists player.json", results);
                firstStart.Dispose();

                PlayerProgressService reloaded = CreateService(firstStartPath, rules);
                await reloaded.InitializeAsync();
                Require(reloaded.Snapshot.Equals(new ProgressSnapshot(100, 0, 0)),
                    "valid save reloads all values", results);
                reloaded.Dispose();

                string outcomePath = Path.Combine(rootPath, "outcomes");
                PlayerProgressService outcomes = CreateService(outcomePath, new EconomyRules(3, 10, 5, 25));
                await outcomes.InitializeAsync();
                int outcomeChanges = 0;
                outcomes.Changed += _ => outcomeChanges++;
                await outcomes.RecordResultAsync(SequenceState.Won);
                await outcomes.RecordResultAsync(SequenceState.Lost);
                await outcomes.RecordResultAsync(SequenceState.Lost);
                await outcomes.RecordResultAsync(SequenceState.Lost);
                Require(outcomes.Snapshot.Equals(new ProgressSnapshot(0, 1, 3)) && outcomeChanges == 4,
                    "reward, penalty, floor and one snapshot event per result stay coherent", results);
                outcomes.Dispose();

                string resetPath = Path.Combine(rootPath, "reset");
                PlayerProgressService reset = CreateService(resetPath, new EconomyRules(25, 0, 1, 25));
                await reset.InitializeAsync();
                await reset.RecordResultAsync(SequenceState.Won);
                ProgressOperationResult exactReset = await reset.ResetStatisticsAsync();
                Require(exactReset.Status == ProgressOperationStatus.Completed &&
                        reset.Snapshot.Equals(new ProgressSnapshot(0, 0, 0)),
                    "statistics reset deducts exact configured price only", results);
                ProgressSnapshot beforeRejectedReset = reset.Snapshot;
                ProgressOperationResult rejectedReset = await reset.ResetStatisticsAsync();
                Require(rejectedReset.Status == ProgressOperationStatus.InsufficientGold &&
                        reset.Snapshot.Equals(beforeRejectedReset),
                    "insufficient reset leaves the coherent snapshot unchanged", results);
                reset.Dispose();

                string invalidPath = Path.Combine(rootPath, "invalid");
                Directory.CreateDirectory(invalidPath);
                string invalidFile = Path.Combine(invalidPath, "player.json");
                const string invalidJson = "{\"schemaVersion\":1,\"gold\":-1,\"wins\":2,\"losses\":3}";
                File.WriteAllText(invalidFile, invalidJson);
                PlayerProgressService invalid = CreateService(invalidPath, rules);
                await invalid.InitializeAsync();
                Require(!invalid.IsReady && File.ReadAllText(invalidFile) == invalidJson,
                    "invalid existing save is rejected without overwrite", results);
                invalid.Dispose();

                string unsupportedPath = Path.Combine(rootPath, "unsupported");
                Directory.CreateDirectory(unsupportedPath);
                File.WriteAllText(Path.Combine(unsupportedPath, "player.json"),
                    "{\"schemaVersion\":2,\"gold\":100,\"wins\":0,\"losses\":0}");
                PlayerProgressService unsupported = CreateService(unsupportedPath, rules);
                await unsupported.InitializeAsync();
                Require(!unsupported.IsReady, "unsupported save schema is rejected", results);
                unsupported.Dispose();

                string missingFieldPath = Path.Combine(rootPath, "missing-field");
                Directory.CreateDirectory(missingFieldPath);
                File.WriteAllText(Path.Combine(missingFieldPath, "player.json"),
                    "{\"schemaVersion\":1,\"gold\":100,\"wins\":0}");
                PlayerProgressService missingField = CreateService(missingFieldPath, rules);
                await missingField.InitializeAsync();
                Require(!missingField.IsReady, "missing typed save field is rejected", results);
                missingField.Dispose();

                string corruptPath = Path.Combine(rootPath, "corrupt");
                Directory.CreateDirectory(corruptPath);
                string corruptFile = Path.Combine(corruptPath, "player.json");
                const string corruptJson = "{not-json";
                File.WriteAllText(corruptFile, corruptJson);
                PlayerProgressService corrupt = CreateService(corruptPath, rules);
                await corrupt.InitializeAsync();
                string nullPath = Path.Combine(rootPath, "null");
                Directory.CreateDirectory(nullPath);
                string nullFile = Path.Combine(nullPath, "player.json");
                File.WriteAllText(nullFile, "null");
                PlayerProgressService nullSave = CreateService(nullPath, rules);
                await nullSave.InitializeAsync();
                Require(!corrupt.IsReady && File.ReadAllText(corruptFile) == corruptJson &&
                        !nullSave.IsReady && File.ReadAllText(nullFile) == "null",
                    "malformed and null saves are rejected without overwrite", results);
                corrupt.Dispose();
                nullSave.Dispose();

                string saveFailurePath = Path.Combine(rootPath, "save-failure");
                var realRepository = new LocalFileDataRepository(saveFailurePath, "json");
                PlayerProgressService seed = CreateService(realRepository, rules);
                await seed.InitializeAsync();
                seed.Dispose();
                var failOnce = new FailOnceWriteRepository(realRepository);
                PlayerProgressService saveFailure = CreateService(failOnce, rules);
                await saveFailure.InitializeAsync();
                ProgressOperationResult memoryOnly = await saveFailure.RecordResultAsync(SequenceState.Won);
                PlayerData oldDisk = new JsonSerializer().Deserialize<PlayerData>(
                    File.ReadAllText(Path.Combine(saveFailurePath, "player.json")));
                Require(memoryOnly.Status == ProgressOperationStatus.SavedInMemoryOnly &&
                        saveFailure.Snapshot.Equals(new ProgressSnapshot(110, 1, 0)) &&
                        oldDisk.Gold == 100 && oldDisk.Wins == 0 &&
                        saveFailure.Error == PlayerProgressService.SaveErrorMessage,
                    "save failure preserves memory, prior file and visible error", results);
                ProgressOperationResult retry = await saveFailure.RecordResultAsync(SequenceState.Lost);
                bool retryClearedError = string.IsNullOrEmpty(saveFailure.Error);
                saveFailure.Dispose();
                PlayerProgressService afterRetry = CreateService(realRepository, rules);
                await afterRetry.InitializeAsync();
                Require(retry.Status == ProgressOperationStatus.Completed && retryClearedError &&
                        afterRetry.Snapshot.Equals(new ProgressSnapshot(105, 1, 1)),
                    "next operation persists accumulated progress and clears save error", results);
                afterRetry.Dispose();

                string cancellationPath = Path.Combine(rootPath, "cancellation");
                var cancellationRepository = new LocalFileDataRepository(cancellationPath, "json");
                using var cancellation = new CancellationTokenSource();
                PlayerProgressService cancelled = CreateService(cancellationRepository, rules, cancellation.Token);
                await cancelled.InitializeAsync();
                ProgressSnapshot beforeCancellation = cancelled.Snapshot;
                string diskBeforeCancellation = File.ReadAllText(Path.Combine(cancellationPath, "player.json"));
                cancellation.Cancel();
                bool cancellationObserved = false;

                try
                {
                    await cancelled.RecordResultAsync(SequenceState.Won);
                }
                catch (OperationCanceledException)
                {
                    cancellationObserved = true;
                }

                Require(cancellationObserved && cancelled.Snapshot.Equals(beforeCancellation) &&
                        File.ReadAllText(Path.Combine(cancellationPath, "player.json")) == diskBeforeCancellation,
                    "pre-cancellation leaves memory and stored progress unchanged", results);
                cancelled.Dispose();

                string overflowPath = Path.Combine(rootPath, "overflow");
                Directory.CreateDirectory(overflowPath);
                File.WriteAllText(Path.Combine(overflowPath, "player.json"),
                    "{\"schemaVersion\":1,\"gold\":2147483647,\"wins\":2147483647,\"losses\":0}");
                PlayerProgressService overflow = CreateService(overflowPath, rules);
                await overflow.InitializeAsync();
                ProgressSnapshot beforeOverflow = overflow.Snapshot;
                ProgressOperationResult overflowResult = await overflow.RecordResultAsync(SequenceState.Won);
                Require(overflowResult.Status == ProgressOperationStatus.Failed &&
                        overflow.Snapshot.Equals(beforeOverflow),
                    "overflow cannot partially mutate progress", results);
                overflow.Dispose();

                var serializer = new JsonSerializer();
                string serialized = serializer.Serialize(new PlayerData(100, 2, 3));
                Require(!serialized.Contains("$type"), "JSON persistence emits no polymorphic type metadata", results);

                return string.Join("\n", results);
            }
            finally
            {
                if (Directory.Exists(rootPath))
                    Directory.Delete(rootPath, true);
            }
        }

        private static PlayerProgressService CreateService(string rootPath, EconomyRules rules)
            => CreateService(new LocalFileDataRepository(rootPath, "json"), rules);

        private static PlayerProgressService CreateService(IDataRepository repository, EconomyRules rules,
            CancellationToken cancellationToken = default)
        {
            var wallet = new WalletService();
            var statistics = new StatisticsService();
            var serializer = new JsonSerializer();
            var saveLoad = new SaveLoadService(serializer, new MapDataKeysStorage(), repository);
            var provider = new PlayerDataProvider(saveLoad, rules);
            return new PlayerProgressService(provider, wallet, statistics, rules, cancellationToken);
        }

        private static void Require(bool condition, string label, ICollection<string> results)
        {
            if (!condition)
                throw new InvalidOperationException(label);

            results.Add("PASS: " + label);
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
