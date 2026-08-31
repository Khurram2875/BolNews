using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    

    public class WordPressMigrationState
    {
        private readonly object _lock = new();

        private CancellationTokenSource? _cancellationTokenSource;

        public bool IsRunning { get; private set; }

        public string? Operation { get; private set; }

        public DateTime? StartTime { get; private set; }

        public DateTime? FinishTime { get; private set; }

        public string Status { get; private set; } = "Idle";

        public int Total { get; private set; }

        public int Repaired { get; private set; }

        public int Imported { get; private set; }

        public int Skipped { get; private set; }

        public int Failed { get; private set; }

        public void Start(string operation)
        {
            lock (_lock)
            {
                if (IsRunning)
                {
                    throw new InvalidOperationException(
                        "Another WordPress migration operation is already running.");
                }

                _cancellationTokenSource =
                    new CancellationTokenSource();

                IsRunning = true;
                Operation = operation;

                StartTime = DateTime.Now;
                FinishTime = null;

                Status = "Running";

                Total = 0;
                Imported = 0;
                Repaired = 0;
                Skipped = 0;
                Failed = 0;
            }
        }

        public CancellationToken Token
        {
            get
            {
                lock (_lock)
                {
                    return _cancellationTokenSource?.Token
                        ?? CancellationToken.None;
                }
            }
        }

        public void RequestAbort()
        {
            lock (_lock)
            {
                if (!IsRunning)
                    return;

                Status = "Abort Requested";

                _cancellationTokenSource?.Cancel();
            }
        }

        public void UpdateImportProgress(
            int total,
            int imported,
            int skipped,
            int failed)
        {
            lock (_lock)
            {
                Total = total;
                Imported = imported;
                Skipped = skipped;
                Failed = failed;
            }
        }

        public void UpdateRepairProgress(
            int total,
            int repaired,
            int skipped,
            int failed)
        {
            lock (_lock)
            {
                Total = total;
                Repaired = repaired;
                Skipped = skipped;
                Failed = failed;
            }
        }

        public void Complete(
            int total,
            int imported,
            int repaired,
            int skipped,
            int failed)
        {
            lock (_lock)
            {
                Total = total;
                Imported = imported;
                Repaired = repaired;
                Skipped = skipped;
                Failed = failed;

                IsRunning = false;
                FinishTime = DateTime.Now;
                Status = "Completed";

                DisposeToken();
            }
        }

        public void MarkAborted(
            int total,
            int imported,
            int repaired,
            int skipped,
            int failed)
        {
            lock (_lock)
            {
                Total = total;
                Imported = imported;
                Repaired = repaired;
                Skipped = skipped;
                Failed = failed;

                IsRunning = false;
                FinishTime = DateTime.Now;
                Status = "Aborted";

                DisposeToken();
            }
        }

        public void MarkFailed(
            int total,
            int imported,
            int repaired,
            int skipped,
            int failed)
        {
            lock (_lock)
            {
                Total = total;
                Imported = imported;
                Repaired = repaired;
                Skipped = skipped;
                Failed = failed;

                IsRunning = false;
                FinishTime = DateTime.Now;
                Status = "Failed";

                DisposeToken();
            }
        }

        private void DisposeToken()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
