namespace WindowsFormsApp1
{
  using System;
  using System.Threading.Tasks;
  using System.Windows.Forms;

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
    IDispatcherWaiter WaitAsync();
  }
  
  public class DispatcherWaiter : IDispatcherWaiter
  {
    private readonly Control _ctrl;
    private bool _isCompleted;

    public DispatcherWaiter(Control ctrl)
    {
      _ctrl = ctrl;
    }

    public IDispatcherWaiter GetAwaiter()
    {
      return this;
    }
    
    public TaskStatus GetResult()
    {
      return TaskStatus.RanToCompletion;
    }

    public bool IsCompleted
    {
      get { return _isCompleted; }
    }

    public void OnCompleted(Action continuation)
    {
      try
      {
        _ctrl.BeginInvoke(continuation);
      }
      catch (Exception)
      {
        continuation();
      }
    }

    public bool CheckAccess()
    {
      return !_ctrl.InvokeRequired;
    }

    public void VerifyAccess()
    {
      if (_ctrl.InvokeRequired)
      {
        throw new InvalidOperationException();
      }
    }

    public IDispatcherWaiter CheckedWaitAsync()
    {
      return new DispatcherWaiter(_ctrl) {_isCompleted = CheckAccess()};
    }

    public IDispatcherWaiter WaitAsync()
    {
      return new DispatcherWaiter(_ctrl);
    }
  }
}