using DinkToPdf.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace RazorDinkToPdf.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRazorPdfGeneration_ServicesNull_ThrowsArgumentNullException()
    {
        var services = (IServiceCollection)null!;

        Assert.Throws<ArgumentNullException>(() => services.AddRazorPdfGeneration());
    }

    [Fact]
    public void AddRazorPdfGeneration_AddsExpectedServices()
    {
        var services = new ServiceCollection();

        services.AddRazorPdfGeneration();

        var provider = services.BuildServiceProvider();

        var converter = provider.GetService<IConverter>();

        Assert.NotNull(converter);
    }
}
