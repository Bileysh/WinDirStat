namespace Volumetric_App.Services;

internal static class LargeStack
{
    private const int StackSize = 128 * 1024 * 1024;

    public static Task<T> RunAsync<T>(Func<T> work)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.SetResult(work());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }, StackSize)
        {
            IsBackground = true,
            Name = "Volumetric deep tree worker"
        };

        thread.Start();
        return completion.Task;
    }

    public static T Run<T>(Func<T> work) => RunAsync(work).GetAwaiter().GetResult();
}
