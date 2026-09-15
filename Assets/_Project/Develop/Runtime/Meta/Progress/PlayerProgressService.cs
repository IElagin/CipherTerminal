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

        public bool IsReady { get; private set; }
        public bool IsBusy { get; private set; }
        public string Error { get; private set; }
        public int StatisticsResetCost => _rules.StatisticsResetCost;

        public ProgressSnapshot Snapshot
        {
            get
            {
                if (!IsReady)
                    throw new InvalidOperationException("Player progress is unavailable");

                return CurrentSnapshot();
            }
        }

        public PlayerProgressService(PlayerDataProvider provider, WalletService wallet,
            StatisticsService statistics, EconomyRules rules, CancellationToken projectToken)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
            _rules = rules;
            _projectToken = projectToken;

            _provider.RegisterReader(_wallet);
            _provider.RegisterReader(_statistics);
            _provider.RegisterWriter(_wallet);
            _provider.RegisterWriter(_statistics);
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            if (_initialized)
                throw new InvalidOperationException("Player progress is already initialized");

            _initialized = true;
            IsBusy = true;
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                IsReady = false;
                Error = LoadErrorMessage;
            }
            finally
            {
                IsBusy = false;
            }

            if (publish)
                Changed?.Invoke(CurrentSnapshot());
        }

        public async UniTask<ProgressOperationResult> RecordResultAsync(SequenceState state)
        {
            EnsureNotDisposed();

            if (state != SequenceState.Won && state != SequenceState.Lost)
                throw new ArgumentOutOfRangeException(nameof(state), state, "A terminal result is required");

            if (!IsReady)
                return new ProgressOperationResult(ProgressOperationStatus.Unavailable);
            if (IsBusy)
                return new ProgressOperationResult(ProgressOperationStatus.Busy);

            _projectToken.ThrowIfCancellationRequested();
            IsBusy = true;
            bool publish = false;
            ProgressOperationResult result;

            try
            {
                int gold;
                int wins;
                int losses;

                checked
                {
                    gold = state == SequenceState.Won
                        ? _wallet.Gold + _rules.WinReward
                        : Math.Max(0, _wallet.Gold - _rules.LossPenalty);
                    wins = _statistics.Wins + (state == SequenceState.Won ? 1 : 0);
                    losses = _statistics.Losses + (state == SequenceState.Lost ? 1 : 0);
                }

                int delta = gold - _wallet.Gold;
                _wallet.SetGold(gold);
                _statistics.SetCounts(wins, losses);
                publish = true;

                ProgressOperationStatus status = await SaveCurrentAsync(_projectToken);
                result = new ProgressOperationResult(status, delta);
            }
            catch (OverflowException)
            {
                Error = UpdateErrorMessage;
                publish = true;
                result = new ProgressOperationResult(ProgressOperationStatus.Failed);
            }
            finally
            {
                IsBusy = false;
            }

            if (publish)
                Changed?.Invoke(CurrentSnapshot());

            return result;
        }

        public async UniTask<ProgressOperationResult> ResetStatisticsAsync()
        {
            EnsureNotDisposed();

            if (!IsReady)
                return new ProgressOperationResult(ProgressOperationStatus.Unavailable);
            if (IsBusy)
                return new ProgressOperationResult(ProgressOperationStatus.Busy);

            _projectToken.ThrowIfCancellationRequested();
            IsBusy = true;
            bool publish = false;
            ProgressOperationResult result;

            try
            {
                if (_wallet.Gold < _rules.StatisticsResetCost)
                    return new ProgressOperationResult(ProgressOperationStatus.InsufficientGold);

                _wallet.SetGold(_wallet.Gold - _rules.StatisticsResetCost);
                _statistics.SetCounts(0, 0);
                publish = true;

                ProgressOperationStatus status = await SaveCurrentAsync(_projectToken);
                result = new ProgressOperationResult(status, -_rules.StatisticsResetCost);
            }
            finally
            {
                IsBusy = false;
            }

            if (publish)
                Changed?.Invoke(CurrentSnapshot());

            return result;
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                Error = SaveErrorMessage;
                return ProgressOperationStatus.SavedInMemoryOnly;
            }
        }

        private ProgressSnapshot CurrentSnapshot()
            => new ProgressSnapshot(_wallet.Gold, _statistics.Wins, _statistics.Losses);

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PlayerProgressService));
        }
    }
}
