namespace BeginInvokeToAsyncAwaitApp
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows.Threading;

  /// <summary>
  /// IDispatcherWaiter
  /// </summary>
  public interface IDispatcherWaiter : IAwaitable<IDispatcherWaiter, TaskStatus>
  {
    /// <summary>
    /// Returns Dispatcher.CheckAccess
    /// </summary>
    /// <returns></returns>
    bool CheckAccess();
    
    /// <summary>
    /// Calls Dispatcher.VerifyAccess
    /// </summary>
    /// <returns></returns>
    void VerifyAccess();

    /// <summary>
    /// Returns immediately if called on the dispatcher thread, otherwise dispatch to the dispatcher thread 
    /// </summary>
    /// <returns></returns>
    IDispatcherWaiter CheckedWaitAsync();

    /// <summary>
    /// Dispatch to the dispatcher thread with specified priority
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    IDispatcherWaiter WaitAsync(DispatcherPriority priority = DispatcherPriority.Normal, CancellationToken ct = default(CancellationToken));
  }
  
  public class DispatcherWaiter : IDispatcherWaiter
  {
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherPriority _priority = DispatcherPriority.Normal;
    private readonly CancellationToken _ct;
    private bool _isCompleted;

    public DispatcherWaiter(Dispatcher d)
    {
      _dispatcher = d;
    }

    private DispatcherWaiter(Dispatcher d, DispatcherPriority priority, CancellationToken ct)
    {
      if (priority == DispatcherPriority.Send)
      {
        throw new InvalidOperationException("Send priority is not allowed");
      }

      if (priority <= DispatcherPriority.Inactive)
      {
        throw new InvalidOperationException(priority.ToString() + " priority is not allowed");
      }

      _dispatcher = d;
      _priority = priority;
      _ct = ct;
    }

    public IDispatcherWaiter GetAwaiter()
    {
      return this;
    }
    
    public TaskStatus GetResult()
    {
      return _ct.IsCancellationRequested ? TaskStatus.Canceled : TaskStatus.RanToCompletion;
    }

    public bool IsCompleted
    {
      get { return _isCompleted; }
    }

    public void OnCompleted(Action continuation)
    {
      if (_ct.IsCancellationRequested)
      {
        continuation();
      }
      else
      {
        try
        {
          var op = _dispatcher.InvokeAsync(() => { }, _priority, _ct);
          op.Completed += (s, e) => continuation();
          op.Aborted += (s, e) => continuation();
        }
        catch (Exception)
        {
          continuation();
        }
      }
    }

    public bool CheckAccess()
    {
      return _dispatcher.CheckAccess();
    }

    public void VerifyAccess()
    {
      _dispatcher.VerifyAccess();
    }

    public IDispatcherWaiter CheckedWaitAsync()
    {
      return new DispatcherWaiter(_dispatcher) {_isCompleted = _dispatcher.CheckAccess()};
    }

    public IDispatcherWaiter WaitAsync(DispatcherPriority priority = DispatcherPriority.Normal, CancellationToken ct = default(CancellationToken))
    {
      return new DispatcherWaiter(_dispatcher, priority, ct);
    }
  }

  public static class DispatcherWaiterExt
  {
    public static IDispatcherWaiter WaitForAppIdleAsync(this IDispatcherWaiter waiter, CancellationToken ct = default(CancellationToken))
    {
      return waiter.WaitAsync(DispatcherPriority.ApplicationIdle, ct);
    }
  }
}