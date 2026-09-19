using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;

namespace Assets._Project.Develop.Runtime.Meta.Progress
{
    public sealed class PlayerProgressService : IDisposable
    {
        public const string LoadErrorMessage =
            "Не удалось загрузить прогресс. Сохранённые данные не изменены.";

        public const string SaveErrorMessage =
            "Не удалось сохранить прогресс. Изменения пока в памяти.";

        public const string UpdateErrorMessage = "Не удалось обновить прогресс.";

        private readonly PlayerDataProvider _provider;
        private readonly WalletService _wallet;
        private readonly StatisticsService _statistics;
        private readonly EconomyRules _rules;
        private readonly CancellationToken _projectToken;

        private bool _initialized;
        private bool _disposed;

        public event Action<ProgressSnapshot> Changed;

        public PlayerProgressService(PlayerDataProvider provider, WalletService wallet,
            StatisticsService statistics, EconomyRules rules, CancellationToken projectToken)
        {
            _provider = provider;
            _wallet = wallet;
            _statistics = statistics;
            _rules = rules;
            _projectToken = projectToken;

            _provider.RegisterReader(_wallet);
            _provider.RegisterReader(_statistics);
            _provider.RegisterWriter(_wallet);
            _provider.RegisterWriter(_statistics);
        }

        public bool IsReady { get; private set; }
        public string Error { get; private set; }
        public int StatisticsResetCost => _rules.StatisticsResetCost;

        public ProgressSnapshot Snapshot
        {
            get
            {
                if (IsReady == false)
                    throw new InvalidOperationException("Player progress is unavailable");

                return CreateSnapshot();
            }
        }

        public async UniTask Initialize(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            if (_initialized)
                throw new InvalidOperationException("Player progress is already initialized");

            _initialized = true;
            bool publish = false;

            using var linkedLifetime =
                CancellationTokenSource.CreateLinkedTokenSource(_projectToken, cancellationToken);
            CancellationToken token = linkedLifetime.Token;

            try
            {
                if (await _provider.ExistsAsync(token))
                {
                    await _provider.LoadAsync(token);
                    Error = null;
                    IsReady = true;
                }
                else
                {
                    _provider.Reset();
                    IsReady = true;
                    await SaveCurrentAsync(token);
                }

                publish = true;
            }
            catch (SaveDataException)
            {
                IsReady = false;
                Error = LoadErrorMessage;
            }

            if (publish)
                Changed?.Invoke(CreateSnapshot());
        }

        public async UniTask<ProgressOperationResult> RecordResultAsync(SequenceState state)
        {
            EnsureNotDisposed();

            if (state != SequenceState.Won && state != SequenceState.Lost)
                throw new ArgumentOutOfRangeException(nameof(state), state, "A terminal result is required");

            if (IsReady == false)
                return new ProgressOperationResult(ProgressOperationStatus.Unavailable);

            _projectToken.ThrowIfCancellationRequested();
            int gold = _wallet.GetCurrency(CurrencyTypes.Gold).Value;
            bool wouldOverflow = state == SequenceState.Won
                ? gold > int.MaxValue - _rules.WinReward || _statistics.Wins == int.MaxValue
                : _statistics.Losses == int.MaxValue;

            // Validate the complete result before either service mutates loaded progress.
            if (wouldOverflow)
            {
                Error = UpdateErrorMessage;
                Changed?.Invoke(CreateSnapshot());
                return new ProgressOperationResult(ProgressOperationStatus.Failed);
            }

            int delta;

            if (state == SequenceState.Won)
            {
                _wallet.Add(CurrencyTypes.Gold, _rules.WinReward);
                _statistics.RecordWin();
                delta = _rules.WinReward;
            }
            else
            {
                int penalty = Math.Min(gold, _rules.LossPenalty);
                _wallet.Spend(CurrencyTypes.Gold, penalty);
                _statistics.RecordLoss();
                delta = -penalty;
            }

            ProgressOperationStatus status = await SaveCurrentAsync(_projectToken);
            Changed?.Invoke(CreateSnapshot());
            return new ProgressOperationResult(status, delta);
        }

        public async UniTask<ProgressOperationResult> ResetStatisticsAsync()
        {
            EnsureNotDisposed();

            if (IsReady == false)
                return new ProgressOperationResult(ProgressOperationStatus.Unavailable);

            _projectToken.ThrowIfCancellationRequested();

            if (_wallet.HasEnough(CurrencyTypes.Gold, _rules.StatisticsResetCost) == false)
                return new ProgressOperationResult(ProgressOperationStatus.InsufficientGold);

            _wallet.Spend(CurrencyTypes.Gold, _rules.StatisticsResetCost);
            _statistics.Reset();

            ProgressOperationStatus status = await SaveCurrentAsync(_projectToken);
            Changed?.Invoke(CreateSnapshot());
            return new ProgressOperationResult(status, -_rules.StatisticsResetCost);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Changed = null;
            _provider.UnregisterReader(_wallet);
            _provider.UnregisterReader(_statistics);
            _provider.UnregisterWriter(_wallet);
            _provider.UnregisterWriter(_statistics);
        }

        private async UniTask<ProgressOperationStatus> SaveCurrentAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _provider.SaveAsync(cancellationToken);
                Error = null;
                return ProgressOperationStatus.Completed;
            }
            catch (SaveDataException)
            {
                Error = SaveErrorMessage;
                return ProgressOperationStatus.SavedInMemoryOnly;
            }
        }

        private ProgressSnapshot CreateSnapshot()
            => new ProgressSnapshot(_wallet.GetCurrency(CurrencyTypes.Gold).Value, _statistics.Wins, _statistics.Losses);

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PlayerProgressService));
        }
    }
}
