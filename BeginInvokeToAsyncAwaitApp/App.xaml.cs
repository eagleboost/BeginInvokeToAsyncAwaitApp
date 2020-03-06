namespace BeginInvokeToAsyncAwaitApp
{
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Threading;

  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App
  {
    private DispatcherWaiter _waiter;
    private MainWindow _mainWindow;
    private Dispatcher _appDispatcher;
    private CancellationTokenSource _cts;
    
    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      _mainWindow = new MainWindow();
      _mainWindow.Show();

      _appDispatcher = Dispatcher;
      
      Print("Application Dispatcher");
      PrintLine();
      _waiter = new DispatcherWaiter(Dispatcher);
      StartAsync().ConfigureAwait(true);
    }

    private async Task StartAsync()
    {
      Print("Call DoSomethingAsync() on main GUI");
      await DoSomethingAsync();

      PrintLine();
      await DoSomethingFirstAsync();
      
      PrintLine();
      Print("DoSomething() callback in OnCompleted() on Main GUI");
      await DoSomethingCallback();
      
      PrintLine();
      await Task.Run(()=>
      {
        Print("DoSomething() callback in OnCompleted() on background");
        return DoSomethingCallback();
      });
      
      PrintLine();
      Print("DoSomethingMultipleTimesWithCancellation");
      await DoSomethingMultipleTimesWithCancellationAsync();
    }
    
    private void Print(string msg)
    {
      var d = Dispatcher.CurrentDispatcher;
      _appDispatcher.BeginInvoke(() => AddMsg(msg + ", threadId: " + d.Thread.ManagedThreadId.ToString()));
    }
    
    private void PrintLine()
    {
      _appDispatcher.BeginInvoke(() => AddMsg(""));
    }

    private void AddMsg(string msg)
    {
      _mainWindow.ListBox.Items.Add(msg);
    }
    
    private async Task DoSomethingAsync()
    {
      Print("Before WaitAsync");
      
      await _waiter.WaitAsync();

      Print("After WaitAsync");
      DoSomething();
    }

    private Task DoSomethingFirstAsync()
    {
      return Task.Run(()=>
      {
        Print("Call DoSomethingAsync() on background");
        return DoSomethingAsync();
      });
    }

    private Task DoSomethingCallback()
    {
      Print("Before OnCompleted");

      var tcs = new TaskCompletionSource<int>();
      _waiter.OnCompleted(()=>
      {
        Print("After OnCompleted");
        DoSomething();

        tcs.TrySetResult(1);
      });

      return tcs.Task;
    }

    private async Task DoSomethingMultipleTimesWithCancellationAsync()
    {
      Print("Before OnCompleted");

      var tasks = new List<Task>();
      for (var i = 0; i < 5; i++)
      {
        var task = CreateTask(i);
        tasks.Add(task);
      }

      await Task.WhenAll(tasks);
    }

    private Task CreateTask(int i)
    {
      return Task.Run(async () =>
      {
        Print("Create Task " + i.ToString());
        var ct = ResetCancellationToken();
        var status = await _waiter.WaitAsync(DispatcherPriority.SystemIdle, ct);
        Print("Task " + i.ToString() + ": " + status.ToString() + ", " + ct.IsCancellationRequested.ToString());
        if (status == TaskStatus.RanToCompletion)
        {
          if (!ct.IsCancellationRequested)
          {
            PrintLine();
            Print("Task " + i.ToString() + " completed");
          }
        }
      });
    }
    
    private CancellationToken ResetCancellationToken()
    {
      var newCts = new CancellationTokenSource();
      var cts = Interlocked.Exchange(ref _cts, newCts);
      if (cts != null)
      {
        cts.Cancel();
      }
      
      return newCts.Token;
    }
    
    private void DoSomething()
    {
    }
  }
}
