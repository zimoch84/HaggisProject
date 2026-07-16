using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Haggis.Infrastructure.Tests;

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Test";
    public string ApplicationName { get; set; } = "Haggis.Infrastructure.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

    public static IHostEnvironment Create(string? contentRootPath = null)
    {
        return new TestHostEnvironment
        {
            ContentRootPath = string.IsNullOrWhiteSpace(contentRootPath)
                ? AppContext.BaseDirectory
                : contentRootPath
        };
    }
}
