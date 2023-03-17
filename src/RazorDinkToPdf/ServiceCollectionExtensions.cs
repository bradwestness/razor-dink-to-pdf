using Microsoft.Extensions.DependencyInjection;
using RazorDinkToPdf.Internal;

namespace RazorDinkToPdf;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IConverter" instance for use in generating PDFs./>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to configure.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance configured for PDF generation.</returns>
    public static IServiceCollection AddRazorPdfGeneration(this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton(provider => StaSynchronizedConverter.Instance);

        return services;
    }
}