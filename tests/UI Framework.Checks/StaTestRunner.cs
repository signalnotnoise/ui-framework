using System;
using System.Runtime.ExceptionServices;
using System.Threading;

internal static class StaTestRunner
{
    public static void Run(Action test)
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                test();
            }
            catch (Exception error)
            {
                failure = error;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}