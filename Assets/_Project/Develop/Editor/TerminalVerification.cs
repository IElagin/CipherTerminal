using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Assets._Project.Develop.Runtime.Utilities.Input;

namespace Assets._Project.Develop.Editor
{
    public static class TerminalVerification
    {
        private const double InitialStepDelay = .35;
        private const double StepInterval = .18;
        private const int FirstEscapeCount = 1;
        private const int SecondEscapeCount = 2;
        private const int StandaloneViewGroup = 0;
        private const int FixedResolutionSizeType = 1;

        private static Keyboard _device;
        private static Keyboard _originalKeyboard;
        private static GameObject _probe;
        private static TerminalKeyboard _input;
        private static readonly Queue<Action> _steps = new Queue<Action>();
        private static readonly List<string> _results = new List<string>();
        private static string _received;
        private static int _escapeCount;
        private static double _nextStepTime;
        private static bool _originalRunInBackground;
        private static InputSettings _originalSettings;
        private static InputSettings _testSettings;

        public static string Status { get; private set; } = "not-run";

        public static void StartKeyboardChecks()
        {
            if (EditorApplication.isPlaying == false)
                throw new InvalidOperationException("Play Mode required");

            if (Status == "running")
                throw new InvalidOperationException("Already running");

            Status = "running";
            _results.Clear();
            _steps.Clear();
            _received = "";
            _escapeCount = 0;

            _originalRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            _originalSettings = InputSystem.settings;

            // InputManager destroys HideAndDontSave defaults when settings are replaced.
            // Keep a value-preserving clone to restore instead of a soon-destroyed reference.

            if (_originalSettings.hideFlags == HideFlags.HideAndDontSave)
                _originalSettings = UnityEngine.Object.Instantiate(_originalSettings);

            _testSettings = UnityEngine.Object.Instantiate(_originalSettings);
            _testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = _testSettings;

            _originalKeyboard = Keyboard.current;
            _device = InputSystem.AddDevice<Keyboard>();
            _probe = new GameObject("KeyboardRegressionProbe");
            _input = _probe.AddComponent<TerminalKeyboard>();
            _input.CharacterEntered += character => _received += character;
            _input.EscapePressed += () => _escapeCount++;
            _input.Activate();

            _steps.Enqueue(() => Press(null, Key.LeftShift));
            _steps.Enqueue(() => Press('A', Key.LeftShift, Key.A));
            _steps.Enqueue(() => Require(_received == "A", "Shift+letter is accepted"));
            _steps.Enqueue(() => Press('B', Key.LeftShift, Key.A, Key.B));
            _steps.Enqueue(() => Require(_received == "AB", "overlapping character key is accepted"));
            _steps.Enqueue(() => Press(null));
            _steps.Enqueue(() => Press(null, Key.LeftShift));
            _steps.Enqueue(() => Press(' ', Key.LeftShift, Key.Space));
            _steps.Enqueue(() => Require(_received == "AB ", "Shift+Space is delivered once as a character"));
            _steps.Enqueue(() => InputSystem.QueueTextEvent(_device, ' '));
            _steps.Enqueue(() => Require(_received == "AB ", "held Space text repeat is ignored"));
            _steps.Enqueue(() => Press(null));
            _steps.Enqueue(() => Press(null, Key.Space));
            _steps.Enqueue(() => Require(_received == "AB  ", "Space edge is delivered without a text callback"));
            _steps.Enqueue(() =>
            {
                InputSystem.QueueStateEvent(_device, new KeyboardState(Key.Space, Key.C));
                InputSystem.QueueTextEvent(_device, ' ');
                InputSystem.QueueTextEvent(_device, 'C');
            });
            _steps.Enqueue(() => Require(_received == "AB  C", "delayed Space repeat is discarded beside fresh text"));
            _steps.Enqueue(() => InputSystem.QueueTextEvent(_device, 'C'));
            _steps.Enqueue(() => Require(_received.EndsWith("C") && _received.EndsWith("CC") == false, "held-key text repeat is ignored"));
            _steps.Enqueue(() =>
            {
                _input.Deactivate();
                Press('A', Key.A);
            });
            _steps.Enqueue(() =>
            {
                _received = "";
                _input.Activate();
            });
            _steps.Enqueue(() => InputSystem.QueueTextEvent(_device, 'A'));
            _steps.Enqueue(() => Require(_received == "", "held entry key blocked until release"));
            _steps.Enqueue(() => Press(null));
            _steps.Enqueue(() => Press('A', Key.A));
            _steps.Enqueue(() => Require(_received == "A", "fresh key accepted after release"));
            _steps.Enqueue(() => Press(null));
            _steps.Enqueue(() => Press('4', Key.Escape, Key.Digit4));
            _steps.Enqueue(() => Require(_escapeCount == FirstEscapeCount && _received == "A", "Escape edge is separate from character input"));
            _steps.Enqueue(() => Press('4', Key.Escape, Key.Digit4));
            _steps.Enqueue(() => Require(_escapeCount == FirstEscapeCount && _received == "A", "held Escape and queued text are ignored"));
            _steps.Enqueue(() => Press(null));
            _steps.Enqueue(() => Press(null, Key.Escape));
            _steps.Enqueue(() => Require(_escapeCount == SecondEscapeCount && _received == "A", "fresh Escape is accepted after release"));
            _nextStepTime = EditorApplication.timeSinceStartup + InitialStepDelay;
            EditorApplication.update += Tick;
        }

        private static void Press(char? text, params Key[] keys)
        {
            InputSystem.QueueStateEvent(_device, new KeyboardState(keys));

            if (text.HasValue)
                InputSystem.QueueTextEvent(_device, text.Value);
        }

        private static void Require(bool condition, string label)
        {
            if (condition == false)
                throw new Exception(label + "; received=" + _received);

            _results.Add("PASS: " + label);
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _nextStepTime)
                return;

            _nextStepTime = EditorApplication.timeSinceStartup + StepInterval;

            try
            {
                if (EditorApplication.isPlaying == false)
                    throw new Exception("Play Mode ended during verification");

                if (_steps.Count == 0)
                {
                    Finish("passed");
                    return;
                }

                _steps.Dequeue()();
            }
            catch (Exception error)
            {
                _results.Add("FAIL: " + error.Message);
                Finish("failed");
            }
        }

        private static void Finish(string status)
        {
            EditorApplication.update -= Tick;

            if (_probe != null)
                UnityEngine.Object.Destroy(_probe);

            if (_device != null && _device.added)
                InputSystem.RemoveDevice(_device);

            if (_originalKeyboard != null && _originalKeyboard.added)
                _originalKeyboard.MakeCurrent();

            InputSystem.settings = _originalSettings;

            if (_testSettings != null)
                UnityEngine.Object.Destroy(_testSettings);

            Application.runInBackground = _originalRunInBackground;
            Status = status;

            string resultsDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/Checks"));
            Directory.CreateDirectory(resultsDirectory);
            File.WriteAllLines(Path.Combine(resultsDirectory, "keyboard-" + status + ".txt"), _results);
            Debug.Log("Keyboard regression checks: " + status);
        }

        public static string SetResolution(int width, int height)
        {
            Assembly assembly = typeof(UnityEditor.Editor).Assembly;
            Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance").GetValue(null);
            MethodInfo getGroup = sizesType.GetMethod("GetGroup");
            Type groupEnum = getGroup.GetParameters()[0].ParameterType;
            object group = getGroup.Invoke(sizes, new[] { Enum.ToObject(groupEnum, StandaloneViewGroup) });
            Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
            Type kind = assembly.GetType("UnityEditor.GameViewSizeType");
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            int selected = -1;

            for (int i = 0; i < count; i++)
            {
                object existing = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });

                if ((int)sizeType.GetProperty("width").GetValue(existing) == width && (int)sizeType.GetProperty("height").GetValue(existing) == height)
                {
                    selected = i;
                    break;
                }
            }

            if (selected < 0)
            {
                object size = Activator.CreateInstance(sizeType, new object[] { Enum.ToObject(kind, FixedResolutionSizeType), width, height, "Cipher " + width + "x" + height });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                selected = count;
            }

            Type viewType = assembly.GetType("UnityEditor.GameView");
            EditorWindow view = EditorWindow.GetWindow(viewType);
            viewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, selected);
            view.Focus();
            view.Repaint();

            return "Selected " + width + "x" + height;
        }

        public static string GetActualResolution()
        {
            Type type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(type);
            return type.GetProperty("targetRenderSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view).ToString();
        }
    }
}
