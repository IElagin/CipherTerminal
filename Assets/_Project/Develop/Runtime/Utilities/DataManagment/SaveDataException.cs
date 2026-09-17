using System;
using System.IO;
using Newtonsoft.Json;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment
{
    public sealed class SaveDataException : Exception
    {
        public SaveDataException(Exception innerException)
            : base("The save storage or saved document is invalid or unavailable", innerException)
        {
        }

        public static bool IsExpectedFailure(Exception exception)
            => exception is IOException or InvalidDataException or UnauthorizedAccessException or JsonException;
    }
}
