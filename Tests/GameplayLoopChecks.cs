using System;
using Assets._Project.Develop.Runtime.Gameplay;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

public static class GameplayLoopChecks
{
    private static int _passed;

    public static int Main()
    {
        RunDoesNotRepeatInitialUpdateOrResetProgress();
        InputSpaceLosesWithoutNavigatingUntilLaterSpace();
        LossRequestsSameModeRetryOnlyOnce();
        FailedNavigationCanBeRetried();
        WinRequestsMenuOnlyOnce();
        StopIsTerminalAndBlocksFurtherInput();
        InitialUpdateCallbacksCannotSubmitInput();
        ResultCallbacksCannotReenterSubmission();
        StopFromUpdateSuppressesResultDelivery();
        EvaluatedInputEmitsOneSemanticEventBeforeVisualAndResultEvents();
        IgnoredAndTerminalInputStaySilent();
        InputEvaluatedCallbacksCannotReenterSubmission();
        StopFromInputEvaluatedSuppressesLaterEvents();

        Console.WriteLine("PASS " + _passed + " GameplayLoop checks");
        return 0;
    }

    private static void RunDoesNotRepeatInitialUpdateOrResetProgress()
    {
        var loop = Create("12", SequenceMode.Digits);
        int updates = 0;
        loop.Updated += () => updates++;

        loop.Run();
        loop.Submit('1');
        loop.Run();

        Equal(2, updates, "Run emits one initial update and one accepted-input update");
        Equal(1, loop.Session.Progress, "repeated Run preserves progress");
    }

    private static void InputSpaceLosesWithoutNavigatingUntilLaterSpace()
    {
        var loop = Create("12", SequenceMode.Digits);
        int results = 0;
        int navigation = 0;
        loop.Result += state =>
        {
            Equal(SequenceState.Lost, state, "Space during input reports loss");
            results++;
        };
        loop.NavigationRequested += request => navigation++;

        loop.Run();
        loop.Submit(' ');

        Equal(SequenceState.Lost, loop.Session.State, "Space is submitted as game input");
        Equal(1, results, "Space reports one result");
        Equal(0, navigation, "the losing Space does not also retry");

        loop.Submit(' ');
        Equal(1, navigation, "a later Space requests retry");
    }

    private static void LossRequestsSameModeRetryOnlyOnce()
    {
        var loop = new GameplayLoop(
            new SequenceSession("AB"),
            new GameplayInputArgs(7, SequenceMode.Letters));
        GameplayNavigationRequest observed = null;
        int navigation = 0;
        loop.NavigationRequested += request =>
        {
            observed = request;
            navigation++;
        };

        loop.Run();
        loop.Submit('x');
        loop.Submit('x');
        loop.Submit(' ');
        loop.Submit(' ');

        Equal(1, navigation, "loss navigation is emitted once");
        Equal(GameplayNavigationDestination.Retry, observed.Destination, "loss retries gameplay");
        Equal(7, observed.LevelNumber, "retry preserves level");
        Equal(SequenceMode.Letters, observed.Mode, "retry preserves letter mode");
    }

    private static void WinRequestsMenuOnlyOnce()
    {
        var loop = Create("a", SequenceMode.Letters);
        GameplayNavigationRequest observed = null;
        int results = 0;
        int navigation = 0;
        loop.Result += state =>
        {
            Equal(SequenceState.Won, state, "case-insensitive final input reports win");
            results++;
        };
        loop.NavigationRequested += request =>
        {
            observed = request;
            navigation++;
        };

        loop.Run();
        loop.Submit('A');
        loop.Submit(' ');
        loop.Submit(' ');

        Equal(1, results, "win result is emitted once");
        Equal(1, navigation, "win navigation is emitted once");
        Equal(GameplayNavigationDestination.MainMenu, observed.Destination, "win returns to menu");
    }

    private static void FailedNavigationCanBeRetried()
    {
        var loop = Create("1", SequenceMode.Digits);
        int navigation = 0;
        loop.NavigationRequested += request => navigation++;

        loop.Run();
        loop.Submit('9');
        loop.Submit(' ');
        loop.Submit(' ');
        Equal(1, navigation, "a pending navigation request remains latched");

        loop.AllowNavigationRetry();
        loop.Submit(' ');
        Equal(2, navigation, "a failed transition can be requested again explicitly");
    }

    private static void StopIsTerminalAndBlocksFurtherInput()
    {
        var loop = Create("12", SequenceMode.Digits);
        int updates = 0;
        int results = 0;
        int navigation = 0;
        loop.Updated += () => updates++;
        loop.Result += state => results++;
        loop.NavigationRequested += request => navigation++;

        loop.Run();
        loop.Stop();
        loop.Submit('1');
        loop.Run();
        loop.Submit(' ');

        Equal(1, updates, "Stop blocks later updates and repeated Run");
        Equal(0, loop.Session.Progress, "Stop blocks progress");
        Equal(0, results, "Stop blocks results");
        Equal(0, navigation, "Stop blocks navigation");
    }

    private static void ResultCallbacksCannotReenterSubmission()
    {
        var loop = Create("1", SequenceMode.Digits);
        int navigation = 0;
        loop.NavigationRequested += request => navigation++;
        loop.Result += state => loop.Submit(' ');

        loop.Run();
        loop.Submit('1');

        Equal(0, navigation, "result callbacks cannot reuse the completing input cycle");
        loop.Submit(' ');
        Equal(1, navigation, "later input can navigate after result delivery");
    }

    private static void InitialUpdateCallbacksCannotSubmitInput()
    {
        var loop = Create("1", SequenceMode.Digits);
        loop.Updated += () => loop.Submit('1');

        loop.Run();

        Equal(0, loop.Session.Progress, "initial update callbacks cannot submit input");
    }

    private static void StopFromUpdateSuppressesResultDelivery()
    {
        var loop = Create("1", SequenceMode.Digits);
        int results = 0;
        loop.Result += state => results++;
        loop.Updated += () =>
        {
            if (loop.Session.State == SequenceState.Won)
                loop.Stop();
        };

        loop.Run();
        loop.Submit('1');

        Equal(0, results, "Stop from an update suppresses later result delivery");
    }

    private static void EvaluatedInputEmitsOneSemanticEventBeforeVisualAndResultEvents()
    {
        var loop = Create("12", SequenceMode.Digits);
        string events = "";
        loop.InputEvaluated += evaluation => events += "evaluation:" + evaluation + ",";
        loop.Updated += () => events += "updated,";
        loop.Result += state => events += "result:" + state + ",";

        loop.Run();
        events = "";
        loop.Submit('1');
        Equal("evaluation:Correct,updated,", events, "non-final match emits Correct before its visual update");

        events = "";
        loop.Submit('2');
        Equal("evaluation:Completed,updated,result:Won,", events, "final match emits only Completed before update and result");

        var losingLoop = Create("12", SequenceMode.Digits);
        events = "";
        losingLoop.InputEvaluated += evaluation => events += "evaluation:" + evaluation + ",";
        losingLoop.Updated += () => events += "updated,";
        losingLoop.Result += state => events += "result:" + state + ",";
        losingLoop.Run();
        events = "";
        losingLoop.Submit('9');

        Equal("evaluation:Incorrect,updated,result:Lost,", events, "incorrect input emits only Incorrect before update and result");
    }

    private static void IgnoredAndTerminalInputStaySilent()
    {
        var loop = Create("1", SequenceMode.Digits);
        int evaluations = 0;
        loop.InputEvaluated += evaluation => evaluations++;

        loop.Submit('1');
        loop.Run();
        loop.Submit('\n');
        Equal(0, evaluations, "input ignored before Run and control input emit no evaluation");

        loop.Submit('1');
        loop.Submit('x');
        loop.Submit(' ');

        Equal(1, evaluations, "completed input and terminal navigation produce no duplicate feedback");
    }

    private static void InputEvaluatedCallbacksCannotReenterSubmission()
    {
        var loop = Create("12", SequenceMode.Digits);
        int evaluations = 0;
        loop.InputEvaluated += evaluation =>
        {
            evaluations++;
            loop.Submit('2');
        };

        loop.Run();
        loop.Submit('1');

        Equal(1, loop.Session.Progress, "input-evaluated callbacks cannot advance the sequence again");
        Equal(1, evaluations, "reentrant submission cannot emit another evaluation");
    }

    private static void StopFromInputEvaluatedSuppressesLaterEvents()
    {
        var loop = Create("1", SequenceMode.Digits);
        int updates = 0;
        int results = 0;
        loop.Updated += () => updates++;
        loop.Result += state => results++;
        loop.InputEvaluated += evaluation => loop.Stop();

        loop.Run();
        loop.Submit('1');

        Equal(1, updates, "Stop from input feedback suppresses the trailing visual update");
        Equal(0, results, "Stop from input feedback suppresses the trailing result");
    }

    private static GameplayLoop Create(string target, SequenceMode mode)
    {
        return new GameplayLoop(new SequenceSession(target), new GameplayInputArgs(1, mode));
    }

    private static void Equal<T>(T expected, T actual, string label)
    {
        if (!Equals(expected, actual))
            throw new Exception(label + ": expected " + expected + ", got " + actual);

        _passed++;
    }
}
