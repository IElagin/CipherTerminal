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
        private static Keyboard _device;
        private static Keyboard _original;
        private static GameObject _probe;
        private static TerminalKeyboard _input;
        private static readonly Queue<Action> Steps = new Queue<Action>();
        private static readonly List<string> Results = new List<string>();
        private static string _received;
        private static int _escapeCount;
        private static double _next;
        private static bool _background;
        private static InputSettings _originalSettings;
        private static InputSettings _testSettings;

        public static string Status { get; private set; } = "not-run";

        public static void StartKeyboardChecks()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Play Mode required");

            if (Status == "running")
                throw new InvalidOperationException("Already running");

            Status = "running";
            Results.Clear();
            Steps.Clear();
            _received = "";
            _escapeCount = 0;

            _background = Application.runInBackground;
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

            _original = Keyboard.current;
            _device = InputSystem.AddDevice<Keyboard>();
            _probe = new GameObject("KeyboardRegressionProbe");
            _input = _probe.AddComponent<TerminalKeyboard>();
            _input.Character += c => _received += c;
            _input.Escape += () => _escapeCount++;
            _input.Activate();

            Steps.Enqueue(() => Press(null, Key.LeftShift));
            Steps.Enqueue(() => Press('A', Key.LeftShift, Key.A));
            Steps.Enqueue(() => Require(_received == "A", "Shift+letter is accepted"));
            Steps.Enqueue(() => Press('B', Key.LeftShift, Key.A, Key.B));
            Steps.Enqueue(() => Require(_received == "AB", "overlapping character key is accepted"));
            Steps.Enqueue(() => Press(null));
            Steps.Enqueue(() => Press(null, Key.LeftShift));
            Steps.Enqueue(() => Press(' ', Key.LeftShift, Key.Space));
            Steps.Enqueue(() => Require(_received == "AB ", "Shift+Space is delivered once as a character"));
            Steps.Enqueue(() => InputSystem.QueueTextEvent(_device, ' '));
            Steps.Enqueue(() => Require(_received == "AB ", "held Space text repeat is ignored"));
            Steps.Enqueue(() => Press(null));
            Steps.Enqueue(() => Press(null, Key.Space));
            Steps.Enqueue(() => Require(_received == "AB  ", "Space edge is delivered without a text callback"));
            Steps.Enqueue(() =>
            {
                InputSystem.QueueStateEvent(_device, new KeyboardState(Key.Space, Key.C));
                InputSystem.QueueTextEvent(_device, ' ');
                InputSystem.QueueTextEvent(_device, 'C');
            });
            Steps.Enqueue(() => Require(_received == "AB  C", "delayed Space repeat is discarded beside fresh text"));
            Steps.Enqueue(() => InputSystem.QueueTextEvent(_device, 'C'));
            Steps.Enqueue(() => Require(_received.EndsWith("C") && !_received.EndsWith("CC"), "held-key text repeat is ignored"));
            Steps.Enqueue(() =>
            {
                _input.Deactivate();
                Press('A', Key.A);
            });
            Steps.Enqueue(() =>
            {
                _received = "";
                _input.Activate();
            });
            Steps.Enqueue(() => InputSystem.QueueTextEvent(_device, 'A'));
            Steps.Enqueue(() => Require(_received == "", "held entry key blocked until release"));
            Steps.Enqueue(() => Press(null));
            Steps.Enqueue(() => Press('A', Key.A));
            Steps.Enqueue(() => Require(_received == "A", "fresh key accepted after release"));
            Steps.Enqueue(() => Press(null));
            Steps.Enqueue(() => Press('4', Key.Escape, Key.Digit4));
            Steps.Enqueue(() => Require(_escapeCount == 1 && _received == "A", "Escape edge is separate from character input"));
            Steps.Enqueue(() => Press('4', Key.Escape, Key.Digit4));
            Steps.Enqueue(() => Require(_escapeCount == 1 && _received == "A", "held Escape and queued text are ignored"));
            Steps.Enqueue(() => Press(null));
            Steps.Enqueue(() => Press(null, Key.Escape));
            Steps.Enqueue(() => Require(_escapeCount == 2 && _received == "A", "fresh Escape is accepted after release"));
            _next = EditorApplication.timeSinceStartup + .35;
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
            if (!condition)
                throw new Exception(label + "; received=" + _received);

            Results.Add("PASS: " + label);
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next)
                return;

            _next = EditorApplication.timeSinceStartup + .18;

            try
            {
                if (!EditorApplication.isPlaying)
                    throw new Exception("Play Mode ended during verification");

                if (Steps.Count == 0)
                {
                    Finish("passed");
                    return;
                }

                Steps.Dequeue()();
            }
            catch (Exception error)
            {
                Results.Add("FAIL: " + error.Message);
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

            if (_original != null && _original.added)
                _original.MakeCurrent();

            InputSystem.settings = _originalSettings;

            if (_testSettings != null)
                UnityEngine.Object.Destroy(_testSettings);

            Application.runInBackground = _background;
            Status = status;

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/Checks"));
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, "keyboard-" + status + ".txt"), Results);
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
            object group = getGroup.Invoke(sizes, new[] { Enum.ToObject(groupEnum, 0) });
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
                object size = Activator.CreateInstance(sizeType, new object[] { Enum.ToObject(kind, 1), width, height, "Cipher " + width + "x" + height });
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

        public static string ActualResolution()
        {
            Type type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(type);
            return type.GetProperty("targetRenderSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view).ToString();
        }
    }
}
