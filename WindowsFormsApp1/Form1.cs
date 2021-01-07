using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
  public partial class Form1 : Form
  {
    private DispatcherWaiter _waiter;

    public Form1()
    {
      InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      Print("Application Dispatcher");
      PrintLine();
      _waiter = new DispatcherWaiter(this);
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
      await Task.Run(() =>
      {
        Print("DoSomething() callback in OnCompleted() on background");
        return DoSomethingCallback();
      });

      PrintLine();
      Print("DoSomethingMultipleTimesWithCancellation");
      await DoSomethingMultipleTimesWithCancellationAsync();
    }

    public delegate void AddMsgDelegate(string msg);

    private void Print(string msg)
    {
      var param = msg + ", threadId: " + Thread.CurrentThread.ManagedThreadId.ToString();
      BeginInvoke(new AddMsgDelegate(AddMsg), param);
    }

    private void PrintLine()
    {
      BeginInvoke(new AddMsgDelegate(AddMsg), "");
    }

    private void AddMsg(string msg)
    {
      listBox1.Items.Add(msg);
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
      return Task.Run(() =>
      {
        Print("Call DoSomethingAsync() on background");
        return DoSomethingAsync();
      });
    }

    private Task DoSomethingCallback()
    {
      Print("Before OnCompleted");

      var tcs = new TaskCompletionSource<int>();
      _waiter.OnCompleted(() =>
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
        var status = await _waiter.WaitAsync();
        Print("Task " + i.ToString() + ": " + status.ToString());
        PrintLine();
        Print("Task " + i.ToString() + " completed");
      });
    }

    private void DoSomething()
    {
    }
  }
}
