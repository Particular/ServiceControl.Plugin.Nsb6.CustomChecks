namespace ServiceControl.Plugin.CustomChecks.Internal
{
    using System.Threading.Tasks;

    public interface ICheck
    {
        string Category { get; }
        string Id { get; }
    }
}