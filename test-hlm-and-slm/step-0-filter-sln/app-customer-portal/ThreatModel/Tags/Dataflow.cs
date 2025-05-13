namespace ThreatModel.Tags;

public static class Dataflow
{
    public static T Push<T>(string name, Func<T> func) => func();

    public static void Push(string name, Action action) => action();

    public static Task Push(string name, Func<Task> taskFunc) => taskFunc();

    public static Task<T> Push<T>(string name, Func<Task<T>> taskFunc) => taskFunc();

    public static T Pull<T>(string name, Func<T> func) => func();

    public static Task<T> Pull<T>(string name, Func<Task<T>> taskFunc) => taskFunc();

    public static T Sink<T>(string name, Func<T> func) => func();

    public static void Sink(string name, Action action) => action();

    public static Task Sink(string name, Func<Task> taskFunc) => taskFunc();

    public static Task<T> Sink<T>(string name, Func<Task<T>> taskFunc) => taskFunc();
}
