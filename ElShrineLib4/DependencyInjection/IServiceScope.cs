namespace ElShrine.DependencyInjection;

public interface IServiceScope : IDisposable
{
    IServiceProvider ServiceProvider { get; }
}
