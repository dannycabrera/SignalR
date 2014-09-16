// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

#if NETFX_CORE
using Windows.System.Threading;
#endif

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    internal sealed class TaskQueueMonitor : ITaskMonitor, IDisposable
    {
#if !NETFX_CORE
        private Timer _timer;
#else
        private ThreadPoolTimer _timer;
#endif

        private readonly IConnection _connection;
        private readonly TimeSpan _deadlockErrorTimeout;
 
        private readonly object _lockObj = new object();
        private bool _isRunning;
        private uint _taskId;
        private uint _lastTaskId;

        public TaskQueueMonitor(IConnection connection, TimeSpan deadlockErrorTimeout)
        {
            _connection = connection;
            _deadlockErrorTimeout = deadlockErrorTimeout;

#if !NETFX_CORE
            _timer = new Timer(_ => Beat(), state: null, dueTime: deadlockErrorTimeout, period: deadlockErrorTimeout);
#else
            _timer = ThreadPoolTimer.CreatePeriodicTimer(_ => Beat(), period: deadlockErrorTimeout);
#endif
        }

        public void TaskStarted()
        {
            lock (_lockObj)
            {
                Debug.Assert(!_isRunning);

                _isRunning = true;
                _taskId++;
            }
        }

        public void TaskCompleted()
        {
            lock (_lockObj)
            {
                Debug.Assert(!_isRunning);

                _isRunning = false;
            }
        }

        // This is only able to detect deadlocks because Connection enqueues callbacks using
        // Task.Factory.StartNew. Otherwise _queue.ExecutingTask would stay null during the deadlock.
        internal void Beat()
        {
            lock (_lockObj)
            {
                if (_isRunning && _taskId == _lastTaskId)
                {
                    var errorMessage = String.Format(Resources.Error_PossibleDeadlockDetected,
                                                     _deadlockErrorTimeout.TotalSeconds);
                    _connection.OnError(new SlowCallbackException(errorMessage));
                }

                _lastTaskId = _taskId;
            }
        }

        /// <summary>
        /// Dispose off the timer
        /// </summary>
        public void Dispose()
        {
                if (_timer != null)
                {
#if !NETFX_CORE
                    _timer.Dispose();
                    _timer = null;
#else
                    _timer.Cancel();
                    _timer = null;
#endif
                }
        }
    }
}
