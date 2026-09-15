using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.DataRepository
{
    public sealed class LocalFileDataRepository : IDataRepository
    {
        private readonly string _folderPath;
        private readonly string _extension;

        public LocalFileDataRepository(string folderPath, string extension)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("Save folder is required", nameof(folderPath));
            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("Save extension is required", nameof(extension));

            _folderPath = folderPath;
            _extension = extension.TrimStart('.');
        }

        public UniTask<string> ReadAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(File.ReadAllText(PathFor(key), Encoding.UTF8));
        }

        public UniTask WriteAsync(string key, string serializedData, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(_folderPath);

            string destination = PathFor(key);
            string temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N");

            try
            {
                File.WriteAllText(temporary, serializedData, new UTF8Encoding(false));
                cancellationToken.ThrowIfCancellationRequested();

                if (File.Exists(destination))
                    File.Replace(temporary, destination, null);
                else
                    File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }

            return UniTask.CompletedTask;
        }

        public UniTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(File.Exists(PathFor(key)));
        }

        private string PathFor(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                key.Contains(Path.DirectorySeparatorChar.ToString()) ||
                key.Contains(Path.AltDirectorySeparatorChar.ToString()))
            {
                throw new ArgumentException("Invalid save key", nameof(key));
            }

            return Path.Combine(_folderPath, key + "." + _extension);
        }
    }
}
