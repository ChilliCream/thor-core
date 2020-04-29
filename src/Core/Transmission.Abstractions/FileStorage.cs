using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thor.Core.Abstractions;

namespace Thor.Core.Transmission.Abstractions
{
    /// <summary>
    /// A transmission storage for <c>Files</c>.
    /// </summary>
    public abstract class FileStorage<TData>
        : ITransmissionStorage<TData>
        where TData : class
    {
        private static readonly int MaxBatchSize = 100;
        private readonly byte[] _emptyFile = new byte[0];
        private readonly TData[] _emptyBatch = new TData[0];
        private readonly string _storagePath;
        private readonly IEnumerable<FileInfo> _files;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStorage{TData}"/> class.
        /// </summary>
        /// <param name="storagePath">A storage path to save temporarily.</param>
        protected FileStorage(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                throw new ArgumentNullException(nameof(storagePath));
            }

            _storagePath = storagePath;

            _files = Directory
                .CreateDirectory(_storagePath)
                .EnumerateFiles("*.tmp", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Create a <see cref="TData"/> object from <see cref="byte"/> array.
        /// </summary>
        protected abstract TData Deserialize(byte[] payload, string fileName);

        /// <summary>
        /// Create a <see cref="byte"/> array from <see cref="TData"/>.
        /// </summary>
        protected abstract byte[] Serialize(TData data);

        /// <summary>
        /// Generates a file name.
        /// </summary>
        protected abstract string EncodeFileName(TData data);

        /// <inheritdoc/>
        public bool HasData => _files.Any();

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<TData>> DequeueAsync(
            CancellationToken cancellationToken)
        {
            if (!HasData)
            {
                return _emptyBatch;
            }

            var batch = new List<TData>();

            foreach(FileInfo file in _files.Take(MaxBatchSize))
            {
                byte[] bytes = await TryReadAllBytesAsync(file.FullName, cancellationToken);

                if (bytes.Length < 1)
                {
                    continue;
                }

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FullName);
                batch.Add(Deserialize(bytes, fileNameWithoutExtension));

                TryDelete(file.FullName);
            }

            return batch;
        }

        /// <inheritdoc/>
        public async Task EnqueueAsync(
            TData[] batch,
            CancellationToken cancellationToken)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            if (batch.Length == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(batch), ExceptionMessages.CollectionIsEmpty);
            }

            foreach (TData data in batch)
            {
                var fileName = Path.Combine(_storagePath, $"{EncodeFileName(data)}.tmp");

                await FileHelper.WriteAllBytesAsync(
                    fileName,
                    Serialize(data),
                    cancellationToken);
            }
        }

        private async Task<byte[]> TryReadAllBytesAsync(
            string fileFullName,
            CancellationToken cancellationToken)
        {
            try
            {
                return await FileHelper
                    .ReadAllBytesAsync(fileFullName, cancellationToken);
            }
            catch (IOException)
            {
                return _emptyFile;
            }
        }

        private void TryDelete(string fullName)
        {
            try
            {
                File.Delete(fullName);
            }
            catch (FileNotFoundException)
            {
                // Don't crash if file doesn't exists.
            }
        }
    }
}