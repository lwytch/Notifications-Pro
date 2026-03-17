using System.Runtime.ExceptionServices;

namespace NotificationsPro.Tests;

internal static class StaThreadTestHelper
{
    public static void Run(Action action)
    {
        Run(() =>
        {
            action();
            return true;
        });
    }

    public static T Run<T>(Func<T> action)
    {
        T? result = default;
        Exception? captured = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured != null)
            ExceptionDispatchInfo.Capture(captured).Throw();

        return result!;
    }
}
