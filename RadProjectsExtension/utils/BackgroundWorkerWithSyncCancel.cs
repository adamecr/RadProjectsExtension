using System.ComponentModel;
using System.Threading;

namespace net.adamec.dev.vs.extension.radprojects.utils
{
    /// <inheritdoc />
    /// <summary>
    /// Executes an operation on a separate thread with possibility of sync cancel.
    /// </summary>
    public class BackgroundWorkerWithSyncCancel : BackgroundWorker
    {
        private readonly AutoResetEvent resetEvent = new AutoResetEvent(false);
        private readonly object lockObject = new object();

        private bool isRunningInternal;
        private bool isCancelling;
        public bool IsCancelling
        {
            get
            {
                lock (lockObject)
                {
                    return isCancelling;
                }
            }
        }

        /// <summary>
        /// Requests cancellation of a pending background operation and wait for cancel to complete
        /// </summary>
        public void Cancel()
        {
            var doCancel = false;
            lock (lockObject)
            {
                if (isRunningInternal && !isCancelling)
                {
                    isCancelling = true;
                    doCancel = true;
                }
            }
            if (!doCancel) return;

            CancelAsync();
            resetEvent.WaitOne(); //wait for DoWork to finish (sets the resetEvent)
            lock (lockObject)
            {
                isCancelling = false;
                isRunningInternal = false;
            }
        }

        /// <inheritdoc />
        /// <summary>Raises the <see cref="E:System.ComponentModel.BackgroundWorker.DoWork" /> event. </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnDoWork(DoWorkEventArgs e)
        {
            lock (lockObject)
            {
                isCancelling = false;
                isRunningInternal = true;
                resetEvent.Reset();
            }
            try
            {
                base.OnDoWork(e);
            }
            finally
            {
                lock (lockObject)
                {
                    isRunningInternal = false;
                    resetEvent.Set(); //ensure the set is called whatever happens in DoWork handler
                }
            }
        }
    }
}
