using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.Reactive;

public static class WalletStatisticsChecks
{
    public static string Run()
    {
        var results = new List<string>();
        const int initialGold = 25;
        const int reward = 10;
        const int resetCost = 25;
        const int expectedRemainingGold = 10;
        const int expectedNotificationCount = 2;
        var gold = new ReactiveVariable<int>(initialGold);
        var currencies = new Dictionary<CurrencyTypes, ReactiveVariable<int>>
        {
            [CurrencyTypes.Gold] = gold
        };
        var wallet = new WalletService(currencies);
        currencies.Clear();
        wallet.AvailableCurrencies.Clear();
        IReadOnlyVariable<int> balance = wallet.GetCurrency(CurrencyTypes.Gold);
        Require(ReferenceEquals(balance, gold) && wallet.AvailableCurrencies.Contains(CurrencyTypes.Gold),
            "course constructor copies dictionary structure and exposes a read-only reactive balance", results);
        Require(wallet.HasEnough(CurrencyTypes.Gold, resetCost), "exact funds are sufficient", results);

        var changes = new List<(int Previous, int Current)>();
        IDisposable subscription = balance.Subscribe((previous, current) => changes.Add((previous, current)));
        wallet.Add(CurrencyTypes.Gold, reward);
        wallet.Spend(CurrencyTypes.Gold, resetCost);
        wallet.Add(CurrencyTypes.Gold, 0);
        wallet.Spend(CurrencyTypes.Gold, 0);
        int afterReward = initialGold + reward;
        Require(balance.Value == expectedRemainingGold && changes.Count == expectedNotificationCount &&
                changes[0] == (initialGold, afterReward) && changes[1] == (afterReward, expectedRemainingGold),
            "Add and Spend publish old/new values once; zero amounts do not notify", results);
        Require(wallet.HasEnough(CurrencyTypes.Gold, resetCost) == false &&
                Throws<InvalidOperationException>(() => wallet.Spend(CurrencyTypes.Gold, resetCost)) &&
                balance.Value == expectedRemainingGold && changes.Count == expectedNotificationCount,
            "insufficient spending preserves balance and notifications", results);

        const int negativeAmount = -1;
        Require(Throws<ArgumentOutOfRangeException>(() => wallet.Add(CurrencyTypes.Gold, negativeAmount)) &&
                Throws<ArgumentOutOfRangeException>(() => wallet.Spend(CurrencyTypes.Gold, negativeAmount)) &&
                Throws<ArgumentOutOfRangeException>(() => wallet.HasEnough(CurrencyTypes.Gold, negativeAmount)),
            "course wallet operations reject negative amounts", results);
        subscription.Dispose();
        wallet.Add(CurrencyTypes.Gold, reward);
        Require(changes.Count == expectedNotificationCount, "disposed wallet subscription stops notifications", results);

        const int savedGold = 73;
        const int savedWins = 2;
        const int savedLosses = 3;
        var saved = new PlayerData(savedGold, savedWins, savedLosses);
        int loadNotifications = 0;
        using (balance.Subscribe((previous, current) => loadNotifications++))
            wallet.ReadFrom(saved);

        const int singleLoadNotification = 1;
        var written = new PlayerData(0, savedWins, savedLosses);
        wallet.WriteTo(written);
        Require(ReferenceEquals(balance, wallet.GetCurrency(CurrencyTypes.Gold)) &&
                balance.Value == savedGold && loadNotifications == singleLoadNotification &&
                written.Gold == savedGold && written.Wins == savedWins && written.Losses == savedLosses,
            "legacy Gold mapping retains reactive identity and leaves statistics untouched", results);

        var statistics = new StatisticsService();
        statistics.ReadFrom(saved);
        statistics.RecordWin();
        statistics.RecordLoss();
        const int expectedWins = 3;
        const int expectedLosses = 4;
        statistics.WriteTo(written);
        Require(statistics.Wins == expectedWins && statistics.Losses == expectedLosses &&
                written.Wins == expectedWins && written.Losses == expectedLosses && written.Gold == savedGold,
            "statistics owns win/loss recording and its save fields", results);
        statistics.Reset();
        statistics.WriteTo(written);
        Require(statistics.Wins == 0 && statistics.Losses == 0 && written.Wins == 0 &&
                written.Losses == 0 && written.Gold == savedGold,
            "statistics Reset clears both counters without touching gold", results);
        return string.Join("\n", results);
    }

    private static bool Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
            return false;
        }
        catch (TException)
        {
            return true;
        }
    }

    private static void Require(bool condition, string label, ICollection<string> results)
    {
        if (condition == false)
            throw new InvalidOperationException(label);

        results.Add("PASS: " + label);
    }
}
