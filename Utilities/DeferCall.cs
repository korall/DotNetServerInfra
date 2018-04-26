using System;
using System.Threading;

public class CancelableDefer
{
    public int mRequestCount = 0;
    Timer mTimer;

    Action<CancelableDefer> mDeferredProc;

    void OnTimer(object state)
    {
        Relief();
    }

    public CancelableDefer(Action<CancelableDefer> deferredProc, int deferToMillSec)
    {
        mDeferredProc = deferredProc;
        mTimer = new Timer(OnTimer, this, deferToMillSec, 1000000);
    }

    public void Cancel()
    {
        mTimer.Dispose();
    }

    public void Relief()
    {
        mTimer.Dispose();
        if (mDeferredProc != null)
            mDeferredProc(this);
    }
}

public class ThresholdDefer
{
    Action mDeferProc;
    CancelableDefer mCancelableDefer;

    int mThresholdCount = 20;
    int mDeferTime = 2 * 1000;  // milliseconds

    public ThresholdDefer(Action deferProc, int thresholdCount, int deferTime)
    {
        mThresholdCount = thresholdCount;
        mDeferTime = deferTime;
        if (mDeferTime < 20)
            mDeferTime = 20;

        mDeferProc = deferProc;
    }

    //////////////////////
    //// On deferred call
    //////////////////////
    void DeferProc(CancelableDefer currentDefer)
    {
        // remove the current defer
        Interlocked.CompareExchange(ref mCancelableDefer, null, currentDefer);

        // call proc function
        if (mDeferProc != null)
            mDeferProc();
    }

    public void RequestDeferCall()
    {
        var defer = mCancelableDefer;
        if (defer == null)
        {
            defer = new CancelableDefer(DeferProc, mDeferTime);
            var last = Interlocked.CompareExchange(ref mCancelableDefer, defer, null);
            if (null != last)
            {
                defer.Cancel();
                defer = last;
            }
        }

        var queryCount = Interlocked.Increment(ref defer.mRequestCount);
        if (queryCount >= mThresholdCount)
        {
            // remove defer and call it to execute defer proc immediately
            if (Interlocked.CompareExchange(ref mCancelableDefer, null, defer) == defer)
                defer.Relief();
        }
    }
}