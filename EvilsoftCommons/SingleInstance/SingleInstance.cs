using log4net;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace EvilsoftCommons.SingleInstance {
    /// <summary>
    /// Ensures there is only one application instance and securely notifies the running instance of later launches.
    /// </summary>
    public sealed class SingleInstance : IDisposable {
        private static readonly ILog _logger = LogManager.GetLogger("SingleInstance");
        private readonly Mutex _mutex;
        private readonly bool _ownsMutex;
        private readonly string _pipeName;
        private CancellationTokenSource? _listenerCancellation;
        private Task? _listenerTask;
        private bool _disposed;

        /// <summary>
        /// Enforces a single instance for an application.
        /// </summary>
        /// <param name="identifier">An identifier unique to this application.</param>
        public SingleInstance(Guid identifier) {
            _pipeName = identifier.ToString();
            _mutex = new Mutex(true, _pipeName, out _ownsMutex);
        }

        /// <summary>
        /// Indicates whether this is the first application instance.
        /// </summary>
        public bool IsFirstInstance => _ownsMutex;

        /// <summary>
        /// Notifies the first running instance that another launch was requested.
        /// </summary>
        public bool NotifyFirstInstance() {
            if (IsFirstInstance) {
                throw new InvalidOperationException("This is the first instance.");
            }

            try {
                using var client = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.Out,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                client.Connect(1000);
                using var writer = new StreamWriter(client);
                writer.WriteLine("show");
                return true;
            }
            catch (TimeoutException) {
                return false;
            }
            catch (IOException) {
                return false;
            }
            catch (Exception ex) {
                _logger.Warn("Unexpected error notifying the running instance", ex);
                return false;
            }
        }

        /// <summary>
        /// Listens for launch requests from successive instances of the application.
        /// </summary>
        /// <param name="onLaunchRequested">The action to run when another instance starts.</param>
        public void ListenForSuccessiveInstances(Action onLaunchRequested) {
            if (!IsFirstInstance) {
                throw new InvalidOperationException("This is not the first instance.");
            }

            var listenerCancellation = new CancellationTokenSource();
            _listenerCancellation = listenerCancellation;
            var cancellationToken = listenerCancellation.Token;
            _listenerTask = Task.Run(() => ListenAsync(onLaunchRequested, cancellationToken));
        }

        private async Task ListenAsync(Action onLaunchRequested, CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    using var server = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                    using var reader = new StreamReader(server);
                    using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    readCancellation.CancelAfter(TimeSpan.FromSeconds(1));
                    var command = await reader.ReadLineAsync(readCancellation.Token).ConfigureAwait(false);
                    if (command == "show") {
                        onLaunchRequested();
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    return;
                }
                catch (OperationCanceledException) {
                    _logger.Warn("The single-instance notification timed out");
                }
                catch (IOException ex) {
                    _logger.Warn("The single-instance notification pipe failed", ex);
                }
                catch (Exception ex) {
                    _logger.Warn("Unexpected error receiving a launch notification", ex);
                    return;
                }
            }
        }

        private void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            if (disposing) {
                _listenerCancellation?.Cancel();
                try {
                    _listenerTask?.Wait(TimeSpan.FromSeconds(1));
                }
                catch (AggregateException ex) {
                    _logger.Warn("The single-instance listener did not stop cleanly", ex.Flatten());
                }
                _listenerCancellation?.Dispose();
            }

            if (_ownsMutex) {
                try {
                    _mutex.ReleaseMutex();
                }
                catch (ApplicationException) {
                    // The process is shutting down, so there is nothing left to release.
                }
                catch (Exception ex) {
                    _logger.Warn(ex.Message);
                }
            }

            if (disposing) {
                _mutex.Dispose();
            }

            _disposed = true;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
