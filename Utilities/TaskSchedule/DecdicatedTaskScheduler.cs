using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Utility.Log;
using Logger = Utility.Log.Log;

namespace Utility
{
    public class DecdicatedTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly CancellationTokenSource mCancellationToken;
        private readonly BlockingCollection<Task> mTasksQueue;

        SynchronizationContext mSyncContext;
        int mInLineThreadId;

        public DecdicatedTaskScheduler()
        {
            mCancellationToken = new CancellationTokenSource();
            mTasksQueue = new BlockingCollection<Task>();
            wathch.Start();
        }

        public override int MaximumConcurrencyLevel => 1;

        public void Dispose()
        {
            if (mCancellationToken.IsCancellationRequested)
                return;

            mTasksQueue.CompleteAdding();
            mCancellationToken.Cancel();
        }

        Stopwatch wathch = new Stopwatch();

        protected override void QueueTask(Task task)
        {
            //Logger.WriteLog(eLogLevel.LOG_DEBUG, "TaskSchedule", $" queue task [{task.Id}]; time: {wathch.Elapsed}");
            VerifyNotDisposed();
            if (mSyncContext == null)
                mSyncContext = SynchronizationContext.Current;
            mTasksQueue.Add(task, this.mCancellationToken.Token);
        }

        // protected override bool TryDequeue(Task task)
        // {
        //     return false;
        // }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            VerifyNotDisposed();
            if (mCancellationToken.Token.IsCancellationRequested)
                return false;

            if (mSyncContext != null && SynchronizationContext.Current != mSyncContext)
                return false;

            if (mInLineThreadId > 0 && Thread.CurrentThread.ManagedThreadId != mInLineThreadId)
                return false;

            //Logger.WriteLog(eLogLevel.LOG_DEBUG, "TaskSchedule", $" try to execute task [{task.Id}] inline, threadId: {Thread.CurrentThread.ManagedThreadId}");
            TryExecuteTask(task);
            //Logger.WriteLog(eLogLevel.LOG_DEBUG, "TaskSchedule",$" complete execute task [{task.Id}], inline, threadId: {Thread.CurrentThread.ManagedThreadId}; time: {wathch.Elapsed}");
            return true;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            VerifyNotDisposed();
            return mTasksQueue.ToArray();
        }

        public void SetInlineThreadId(int threadId)
        {
            mInLineThreadId = threadId;
        }

        public void MonoUpdate()
        {
            if (mInLineThreadId <= 0)
                mInLineThreadId = Thread.CurrentThread.ManagedThreadId;
            try
            {
                var token = mCancellationToken.Token;
                if (mTasksQueue.Count > 0)
                {
                    Task task;
                    if (mTasksQueue.TryTake(out task))
                    {
                        //Logger.WriteLog(eLogLevel.LOG_DEBUG, "TaskSchedule", $" try to execute task [{task.Id}], threadId: {Thread.CurrentThread.ManagedThreadId}, time: {wathch.Elapsed}");
                        TryExecuteTask(task);
                        //Logger.WriteLog(eLogLevel.LOG_DEBUG, "TaskSchedule",$" complete execute task [{task.Id}], threadId: {Thread.CurrentThread.ManagedThreadId}, time: {wathch.Elapsed}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(eLogLevel.LOG_DEBUG, "TaskSchedule", $" catche exception executing task, ex: {ex.Message}");
            }
        }

        private void VerifyNotDisposed()
        {
            if (mCancellationToken.IsCancellationRequested)
                throw new ObjectDisposedException(typeof(DecdicatedTaskScheduler).Name);
        }
    }


    public class DecdicatedTaskFactory : TaskFactory
    {
        public DecdicatedTaskFactory(TaskScheduler scheduler)
            : base(scheduler)
        { }

        public DecdicatedTaskFactory(CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
            : base(cancellationToken, creationOptions, continuationOptions, scheduler)
        { }

        public Task Run(Func<Task> func)
        {
            return StartNew(func).Unwrap();
        }

        public Task<T> Run<T>(Func<Task<T>> func)
        {
            return StartNew(func).Unwrap();
        }

        public Task Run(Action func)
        {
            return StartNew(func);
        }

        public Task<T> Run<T>(Func<T> func)
        {
            return StartNew(func);
        }
    }
}
