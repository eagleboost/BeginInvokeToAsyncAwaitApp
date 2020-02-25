namespace BeginInvokeToAsyncAwaitApp
{
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
    
    private void DoSomething()
    {
    }
  }
}
