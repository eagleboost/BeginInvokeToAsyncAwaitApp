namespace WindowsFormsApp1
{
  using System.Runtime.CompilerServices;

  /// <summary>
  /// IAwaitable
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <typeparam name="TResult"></typeparam>
  public interface IAwaitable<out T, out TResult> : INotifyCompletion
    where T : class
  {
    T GetAwaiter();

    TResult GetResult();

    bool IsCompleted { get; }
  }
}