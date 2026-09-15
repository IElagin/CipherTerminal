using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Utilities.SceneManagement
{
    public static class AsyncErrors
    {
        public static void Report(Exception exception)
        {
            if (exception is OperationCanceledException)
                return;

            Debug.LogException(exception);
        }
    }
}
