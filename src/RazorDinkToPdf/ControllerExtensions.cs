using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace RazorDinkToPdf;

public static class ControllerExtensions
{
    private static string PdfContentType => "application/pdf";

    /// <summary>
    /// Generates a FileContentResult with a PDF by rendering the specified view
    /// using data from the provided model object.
    /// </summary>
    /// <typeparam name="TModel">The type of the model object to use when rendering the Razor view.</typeparam>
    /// <param name="controller">The controller instance used to render the Razor view.</param>
    /// <param name="model">The data model instance used to render the Razor view.</param>
    /// <param name="viewName">The name of the Razor view. This is optional, the view matching the executed controller action will be used by default if not specified.</param>
    /// <param name="downloadFileName">If specified, the FileContentResult will be generated as an "attachment" which will cause the file to be downloaded by the browser, instead of opened in a tab for viewing.</param>
    /// <param name="globalSettings">
    ///     Optional WKHtmlToPdf options to use when rendering the view to a PDF. 
    ///     See: <see href="https://wkhtmltopdf.org/usage/wkhtmltopdf.txt"/>
    /// </param>
    /// <param name="headerSettings">
    ///     Optional WKHtmlToPdf options to use when rendering the view to a PDF. 
    ///     See: <see href="https://wkhtmltopdf.org/usage/wkhtmltopdf.txt"/>
    /// </param>
    /// <param name="footerSettings">
    ///     Optional WKHtmlToPdf options to use when rendering the view to a PDF. 
    ///     See: <see href="https://wkhtmltopdf.org/usage/wkhtmltopdf.txt"/>
    /// </param>
    /// <param name="isPartialView">Boolean indicating if the view to be rendered is a partial view, default: false.</param>
    /// <returns>A task representing the generated FileContentResult instance.</returns>
    public static async Task<FileContentResult> RazorPdf<TModel>(
        this Controller controller,
        TModel model,
        string? viewName = null,
        string? downloadFileName = null,
        GlobalSettings? globalSettings = null,
        HeaderSettings? headerSettings = null,
        FooterSettings? footerSettings = null,
        bool isPartialView = false,
        DateTimeOffset? lastModified = null)
    {
        if (controller is null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var bytes = await controller.GenerateRazorPdfAsync(
            model,
            viewName,
            globalSettings,
            headerSettings,
            footerSettings,
            isPartialView);

        EntityTagHeaderValue? entityTag = null;

        if (lastModified.HasValue)
        {
            var etagValue = $@"""{lastModified.Value.ToUnixTimeSeconds()}""";
            entityTag = new EntityTagHeaderValue(
                etagValue,
                isWeak: true);
        }

        if (!string.IsNullOrEmpty(downloadFileName))
        {
            return controller.File(
                bytes,
                PdfContentType,
                fileDownloadName: downloadFileName,
                lastModified: lastModified,
                entityTag: entityTag);
        }

        return controller.File(
            bytes,
            PdfContentType,
            lastModified: lastModified,
            entityTag: entityTag);
    }

    /// <summary>
    /// Generates a byte array with a PDF by rendering the specified view
    /// using data from the provided model object.
    /// </summary>
    /// <typeparam name="TModel">The type of the model object to use when rendering the Razor view.</typeparam>
    /// <param name="controller">The controller instance used to render the Razor view.</param>
    /// <param name="model">The data model instance used to render the Razor view.</param>
    /// <param name="viewName">The name of the Razor view. This is optional, the view matching the executed controller action will be used by default if not specified.</param>
    /// <param name="globalSettings">
    ///     Optional WKHtmlToPdf options to use when rendering the view to a PDF. 
    ///     See: <see href="https://wkhtmltopdf.org/usage/wkhtmltopdf.txt"/>
    /// </param>
    /// <param name="headerSettings">
    ///     Optional WKHtmlToPdf options to use when rendering the view to a PDF. 
    ///     See: <see href="https://wkhtmltopdf.org/usage/wkhtmltopdf.txt"/>
    /// </param>
    /// <param name="footerSettings">
    ///     Optional WKHtmlToPdf options to use when rendering the view to a PDF. 
    ///     See: <see href="https://wkhtmltopdf.org/usage/wkhtmltopdf.txt"/>
    /// </param>
    /// <param name="isPartialView">Boolean indicating if the view to be rendered is a partial view, default: false.</param>
    /// <returns>A task representing the generated byte array.</returns>
    public static async Task<byte[]> GenerateRazorPdfAsync<TModel>(
        this Controller controller,
        TModel model,
        string? viewName = null,
        GlobalSettings? globalSettings = null,
        HeaderSettings? headerSettings = null,
        FooterSettings? footerSettings = null,
        bool isPartialView = false)
    {
        if (controller is null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        globalSettings ??= new GlobalSettings();
        headerSettings ??= new HeaderSettings();
        footerSettings ??= new FooterSettings();

        var converter = controller.GetRazorPdfGenerator();
        var html = await controller.RenderViewAsync(model, viewName, isPartialView);
        var document = new HtmlToPdfDocument
        {
            GlobalSettings = globalSettings,
            Objects =
            {
                new ObjectSettings
                {
                    HtmlContent = html,
                    PagesCount = true,
                    WebSettings =
                    {
                        DefaultEncoding = "utf-8"
                    },
                    HeaderSettings = headerSettings,
                    FooterSettings = footerSettings
                }
            }
        };
        var bytes = converter.Convert(document);

        return bytes;
    }

    /// <summary>
    /// Gets the configured <see cref="IConverter"/> instance for use in generating PDFs.
    /// </summary>
    /// <param name="controller">The executing controller instance.</param>
    /// <returns>The configured <see cref="IConverter"/> instance.</returns>
    /// <exception cref="RazorPdfGenerationException">Thrown if PDF generation has not been configured in the Dependency Registration container.</exception>
    public static IConverter GetRazorPdfGenerator(
        this Controller controller)
    {
        if (controller is null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        var pdfConverter = controller
            .ControllerContext
            .HttpContext
            .RequestServices
            .GetService<IConverter>();

        if (pdfConverter is null)
        {
            throw new RazorPdfGenerationException(
                $"The Razor PDF Generation services have not been configured for this request. Please call {nameof(IServiceCollection)}.{nameof(ServiceCollectionExtensions.AddRazorPdfGeneration)}() in your application's dependency registration.");
        }

        return pdfConverter;
    }

    /// <summary>
    /// Renders the specified view to an HTML string, using th provided model data.
    /// </summary>
    /// <typeparam name="TModel">The type of the model object to use when rendering the Razor view.</typeparam>
    /// <param name="controller">The controller instance used to render the Razor view.</param>
    /// <param name="model">The data model instance used to render the Razor view.</param>
    /// <param name="viewName">The name of the Razor view. This is optional, the view matching the executed controller action will be used by default if not specified.</param>
    /// <param name="isPartialView">Boolean indicating if the view to be rendered is a partial view, default: false.</param>
    /// <returns>A task representing the rendered HTML.</returns>
    /// <exception cref="RazorPdfGenerationException">Thrown if the specified view could not be found.</exception>
    public static async Task<string> RenderViewAsync<TModel>(
        this Controller controller,
        TModel model,
        string? viewName = null,
        bool isPartialView = false)
    {
        if (controller is null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (string.IsNullOrEmpty(viewName))
        {
            viewName = controller.ControllerContext.ActionDescriptor.ActionName;
        }

        controller.ViewData.Model = model;

        var viewEngine = controller.GetViewEngine();
        var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, !isPartialView);

        if (!viewResult.Success)
        {
            throw new RazorPdfGenerationException(
                $@"A view with the name ""{viewName}"" could not be found.");
        }

        using var writer = new StringWriter();

        var viewContext = new ViewContext(
            controller.ControllerContext,
            viewResult.View,
            controller.ViewData,
            controller.TempData,
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);

        return writer.GetStringBuilder().ToString();
    }

    /// <summary>
    /// Gets the configured <see cref="ICompositeViewEngine"/> for rendering Razor views.
    /// </summary>
    /// <param name="controller">The executing controller instance.</param>
    /// <returns>The configured <see cref="ICompositeViewEngine"/> instance.</returns>
    /// <exception cref="RazorPdfGenerationException">Thrown if Razor view rendering has not been configured in the Dependency Registration container.</exception>
    public static ICompositeViewEngine GetViewEngine(
        this Controller controller)
    {
        if (controller is null)
        {
            throw new ArgumentNullException(nameof(controller));
        }
        var viewEngine = controller
            .HttpContext
            .RequestServices
            .GetService<ICompositeViewEngine>();

        if (viewEngine is null)
        {
            throw new RazorPdfGenerationException(
                $"View rendering services have not been configured for this request. Please call {nameof(IServiceCollection)}.AddControllersWithViews() in your application's dependency registration.");
        }

        return viewEngine;
    }
}