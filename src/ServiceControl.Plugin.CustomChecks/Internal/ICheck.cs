namespace ServiceControl.Plugin.CustomChecks.Internal
{
    public interface ICheck
    {
        string Category { get; }
        string Id { get; }
    }
}