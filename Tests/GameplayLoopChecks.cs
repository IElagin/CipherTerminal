using System;
using Assets._Project.Develop.Runtime.Gameplay;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

public static class GameplayLoopChecks
{
    private const int DefaultLevelNumber = 1;
    private const int SingleEventCount = 1;
    private const int TwoEventCount = 2;
    private const int SingleCharacterProgress = 1;

    private static int _passedCount;

    public static int Main()
    {
        EnteredRetainsOnlyAcceptedNormalizedInput();
        SessionInitializationIsExplicitAndRunsOnce();
        UninitializedSessionCannotAcceptInputOrRun();
        InvalidInitializationCanBeCorrected();
        RunDoesNotRepeatInitialUpdateOrResetProgress();
        InputSpaceLosesWithoutNavigatingUntilLaterSpace();
        LossRequestsSameModeRetryOnlyOnce();
        NavigationRemainsLatched();
        WinRequestsMenuOnlyOnce();
        StopIsTerminalAndBlocksFurtherInput();
        EvaluatedInputEmitsOneSemanticEventBeforeVisualAndResultEvents();
        IgnoredAndTerminalInputStaySilent();

        Console.WriteLine("PASS " + _passedCount + " GameplayLoop checks");
        return 0;
    }

    private static void EnteredRetainsOnlyAcceptedNormalizedInput()
    {
        var loop = CreateLoop("ABCD", SequenceMode.Letters);
        loop.Submit('a');
        AssertEqual("", loop.Session.Entered, "before Run has no entered text");
        loop.Run();
        loop.Submit('\n');
        AssertEqual("", loop.Session.Entered, "ignored control has no entered text");
        loop.Submit('a');
        loop.Submit('b');
        AssertEqual("AB", loop.Session.Entered, "correct prefix is normalized");
        loop.Submit('x');
        AssertEqual("ABX", loop.Session.Entered, "first incorrect input is retained");
        AssertEqual(2, loop.Session.Progress, "incorrect symbol does not advance verified count");
        loop.Submit('c');
        loop.Submit(' ');
        AssertEqual("ABX", loop.Session.Entered, "terminal input and navigation cannot alter entered text");
        var space = CreateLoop("12", SequenceMode.Digits);
        space.Run();
        space.Submit(' ');
        AssertEqual(" ", space.Session.Entered, "incorrect space is retained as data");
        space.Submit(' ');
        AssertEqual(" ", space.Session.Entered, "retry space is not appended");
        var win = CreateLoop("a", SequenceMode.Letters);
        win.Run();
        win.Submit('a');
        win.Submit(' ');
        AssertEqual("A", win.Session.Entered, "winning entry survives navigation unchanged");
    }

    private static void RunDoesNotRepeatInitialUpdateOrResetProgress()
    {
        var loop = CreateLoop("12", SequenceMode.Digits);
        int updateCount = 0;
        loop.Updated += () => updateCount++;

        loop.Run();
        loop.Submit('1');
        loop.Run();

        AssertEqual(TwoEventCount, updateCount, "Run emits one initial update and one accepted-input update");
        AssertEqual(SingleCharacterProgress, loop.Session.Progress, "repeated Run preserves progress");
    }

    private static void InputSpaceLosesWithoutNavigatingUntilLaterSpace()
    {
        var loop = CreateLoop("12", SequenceMode.Digits);
        int resultCount = 0;
        int navigationRequestCount = 0;
        loop.Finished += state =>
        {
            AssertEqual(SequenceState.Lost, state, "Space during input reports loss");
            resultCount++;
        };
        loop.NavigationRequested += request => navigationRequestCount++;

        loop.Run();
        loop.Submit(' ');

        AssertEqual(SequenceState.Lost, loop.Session.State, "Space is submitted as game input");
        AssertEqual(SingleEventCount, resultCount, "Space reports one result");
        AssertEqual(0, navigationRequestCount, "the losing Space does not also retry");

        loop.Submit(' ');
        AssertEqual(SingleEventCount, navigationRequestCount, "a later Space requests retry");
    }

    private static void LossRequestsSameModeRetryOnlyOnce()
    {
        const int levelNumber = 7;
        var loop = new GameplayLoop(
            CreateSession("AB"),
            new GameplayInputArgs(levelNumber, SequenceMode.Letters));
        GameplayNavigationRequest observedRequest = null;
        int navigationRequestCount = 0;
        loop.NavigationRequested += request =>
        {
            observedRequest = request;
            navigationRequestCount++;
        };

        loop.Run();
        loop.Submit('x');
        loop.Submit('x');
        loop.Submit(' ');
        loop.Submit(' ');

        AssertEqual(SingleEventCount, navigationRequestCount, "loss navigation is emitted once");
        AssertEqual(GameplayNavigationDestination.Retry, observedRequest.Destination, "loss retries gameplay");
        AssertEqual(levelNumber, observedRequest.LevelNumber, "retry preserves level");
        AssertEqual(SequenceMode.Letters, observedRequest.Mode, "retry preserves letter mode");
    }

    private static void WinRequestsMenuOnlyOnce()
    {
        var loop = CreateLoop("a", SequenceMode.Letters);
        GameplayNavigationRequest observedRequest = null;
        int resultCount = 0;
        int navigationRequestCount = 0;
        loop.Finished += state =>
        {
            AssertEqual(SequenceState.Won, state, "case-insensitive final input reports win");
            resultCount++;
        };
        loop.NavigationRequested += request =>
        {
            observedRequest = request;
            navigationRequestCount++;
        };

        loop.Run();
        loop.Submit('A');
        loop.Submit(' ');
        loop.Submit(' ');

        AssertEqual(SingleEventCount, resultCount, "win result is emitted once");
        AssertEqual(SingleEventCount, navigationRequestCount, "win navigation is emitted once");
        AssertEqual(GameplayNavigationDestination.MainMenu, observedRequest.Destination, "win returns to menu");
    }

    private static void NavigationRemainsLatched()
    {
        var loop = CreateLoop("1", SequenceMode.Digits);
        int navigationRequestCount = 0;
        loop.NavigationRequested += request => navigationRequestCount++;

        loop.Run();
        loop.Submit('9');
        loop.Submit(' ');
        loop.Submit(' ');
        AssertEqual(SingleEventCount, navigationRequestCount, "a pending navigation request remains latched");

        loop.Submit(' ');
        AssertEqual(SingleEventCount, navigationRequestCount, "navigation stays latched until the scene is replaced");
    }

    private static void StopIsTerminalAndBlocksFurtherInput()
    {
        var loop = CreateLoop("12", SequenceMode.Digits);
        int updateCount = 0;
        int resultCount = 0;
        int navigationRequestCount = 0;
        loop.Updated += () => updateCount++;
        loop.Finished += state => resultCount++;
        loop.NavigationRequested += request => navigationRequestCount++;

        loop.Run();
        loop.Stop();
        loop.Submit('1');
        loop.Run();
        loop.Submit(' ');

        AssertEqual(SingleEventCount, updateCount, "Stop blocks later updates and repeated Run");
        AssertEqual(0, loop.Session.Progress, "Stop blocks progress");
        AssertEqual(0, resultCount, "Stop blocks results");
        AssertEqual(0, navigationRequestCount, "Stop blocks navigation");
    }

    private static void EvaluatedInputEmitsOneSemanticEventBeforeVisualAndResultEvents()
    {
        var loop = CreateLoop("12", SequenceMode.Digits);
        string eventTrace = "";
        loop.InputEvaluated += evaluation => eventTrace += "evaluation:" + evaluation + ",";
        loop.Updated += () => eventTrace += "updated,";
        loop.Finished += state => eventTrace += "result:" + state + ",";

        loop.Run();
        eventTrace = "";
        loop.Submit('1');
        AssertEqual("evaluation:Correct,updated,", eventTrace, "non-final match emits Correct before its visual update");

        eventTrace = "";
        loop.Submit('2');
        AssertEqual("evaluation:Completed,updated,result:Won,", eventTrace, "final match emits only Completed before update and result");

        var losingLoop = CreateLoop("12", SequenceMode.Digits);
        eventTrace = "";
        losingLoop.InputEvaluated += evaluation => eventTrace += "evaluation:" + evaluation + ",";
        losingLoop.Updated += () => eventTrace += "updated,";
        losingLoop.Finished += state => eventTrace += "result:" + state + ",";
        losingLoop.Run();
        eventTrace = "";
        losingLoop.Submit('9');

        AssertEqual("evaluation:Incorrect,updated,result:Lost,", eventTrace, "incorrect input emits only Incorrect before update and result");
    }

    private static void IgnoredAndTerminalInputStaySilent()
    {
        var loop = CreateLoop("1", SequenceMode.Digits);
        int evaluationCount = 0;
        loop.InputEvaluated += evaluation => evaluationCount++;

        loop.Submit('1');
        loop.Run();
        loop.Submit('\n');
        AssertEqual(0, evaluationCount, "input ignored before Run and control input emit no evaluation");

        loop.Submit('1');
        loop.Submit('x');
        loop.Submit(' ');

        AssertEqual(SingleEventCount, evaluationCount, "completed input and terminal navigation produce no duplicate feedback");
    }

    private static GameplayLoop CreateLoop(string target, SequenceMode mode)
    {
        return new GameplayLoop(CreateSession(target), new GameplayInputArgs(DefaultLevelNumber, mode));
    }

    private static SequenceSession CreateSession(string target)
    {
        var session = new SequenceSession(new SequenceGenerator(new OrderedRandom()));
        session.Initialize(target, target.Length);
        return session;
    }

    private static void SessionInitializationIsExplicitAndRunsOnce()
    {
        var random = new OrderedRandom();
        var session = new SequenceSession(new SequenceGenerator(random));
        AssertEqual(0, random.CallCount, "construction does not generate a target");
        AssertEqual(false, session.IsInitialized, "new session waits for initialization");

        const string initialSymbols = "ab";
        const string replacementSymbols = "xyz";
        int sequenceLength = initialSymbols.Length;
        session.Initialize(initialSymbols, sequenceLength);
        AssertEqual("AB", session.Target, "initialization generates and normalizes target");
        AssertEqual(sequenceLength, random.CallCount, "one generation draws exactly the requested characters");
        session.Submit('a');

        AssertThrows<InvalidOperationException>(() => session.Initialize(replacementSymbols, replacementSymbols.Length),
            "repeated initialization must not replace an active round");
        AssertEqual("AB", session.Target, "repeated initialization preserves target");
        AssertEqual(SingleCharacterProgress, session.Progress, "repeated initialization preserves progress");
        AssertEqual(sequenceLength, random.CallCount, "repeated initialization does not consume randomness");
    }

    private static void UninitializedSessionCannotAcceptInputOrRun()
    {
        var session = new SequenceSession(new SequenceGenerator(new OrderedRandom()));
        var loop = new GameplayLoop(session, new GameplayInputArgs(DefaultLevelNumber, SequenceMode.Digits));
        int updateCount = 0;
        loop.Updated += () => updateCount++;

        AssertThrows<InvalidOperationException>(() => session.Submit('1'), "input requires a prepared session");
        AssertThrows<InvalidOperationException>(() => loop.Run(), "Run requires a prepared session");
        AssertEqual(0, updateCount, "failed startup does not publish an unprepared session");

        const string singleSymbol = "1";
        session.Initialize(singleSymbol, singleSymbol.Length);
        loop.Run();
        loop.Submit('1');
        AssertEqual(SequenceState.Won, session.State, "failed premature Run does not prevent correct startup");
    }

    private static void InvalidInitializationCanBeCorrected()
    {
        var session = new SequenceSession(new SequenceGenerator(new OrderedRandom()));
        AssertThrows<ArgumentException>(() => session.Initialize("", SequenceGenerator.MinimumLength), "empty symbols are rejected");
        AssertThrows<ArgumentException>(() => session.Initialize("1", 0), "invalid length is rejected");
        AssertEqual(false, session.IsInitialized, "failed initialization leaves the session uninitialized");

        const string validSymbols = "12";
        session.Initialize(validSymbols, validSymbols.Length);
        AssertEqual("12", session.Target, "valid initialization succeeds after validation failure");
    }

    private static void AssertThrows<TException>(Action action, string label) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            _passedCount++;
            return;
        }

        throw new Exception(label);
    }

    private static void AssertEqual<T>(T expected, T actual, string label)
    {
        if (Equals(expected, actual) == false)
            throw new Exception(label + ": expected " + expected + ", got " + actual);

        _passedCount++;
    }

    private sealed class OrderedRandom : Random
    {
        public int CallCount { get; private set; }

        public override int Next(int maxValue) => CallCount++ % maxValue;
    }
}
