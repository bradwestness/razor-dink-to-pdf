using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorDinkToPdf.Tests.Extensions;

public sealed class ControllerExtensionsTests
{
    [Fact]
    public async Task TmsPdf_ControllerNull_ThrowsArgumentNullException()
    {
        var controller = (Controller)null!;
        var model = new UserModel(123, "Foo", "Bar");

        await Assert.ThrowsAsync<ArgumentNullException>(() => controller.RazorPdf(model));
    }

    [Fact]
    public async Task TmsPdf_ModelNull_ThrowsArgumentNullException()
    {
        var controller = new UserController(GetTestActionContext());
        var model = (UserModel)null!;

        await Assert.ThrowsAsync<ArgumentNullException>(() => controller.RazorPdf(model));
    }

    [Fact]
    public async Task TmsPdf_NoDownload_ReturnsExpected()
    {
        var actionContext = GetTestActionContext();
        var controller = new UserController(actionContext);
        var result = await controller.Get(123);

        await result.ExecuteResultAsync(actionContext);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.True(result.FileContents.Length > 0);
        Assert.Equal(string.Empty, result.FileDownloadName);
    }

    [Fact]
    public async Task TmsPdf_LastModified_ReturnsExpected()
    {
        var actionContext = GetTestActionContext();
        var controller = new UserController(actionContext);
        var result = await controller.Get(123, lastModified: DateTimeOffset.UtcNow);

        await result.ExecuteResultAsync(actionContext);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.True(result.FileContents.Length > 0);
        Assert.Equal(string.Empty, result.FileDownloadName);
    }

    [Fact]
    public async Task TmsPdf_Download_ReturnsExpected()
    {
        var actionContext = GetTestActionContext();
        var controller = new UserController(actionContext);
        var result = await controller.Get(123, "user_details.pdf");

        await result.ExecuteResultAsync(actionContext);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.True(result.FileContents.Length > 0);
        Assert.Equal("user_details.pdf", result.FileDownloadName);

        TryOpenFileIfDebugging(result);
    }
    private static void TryOpenFileIfDebugging(FileContentResult result)
    {
        if (!Debugger.IsAttached)
        {
            return;
        }

        try
        {
            ArgumentNullException.ThrowIfNull(result);

            var outputFilePath = Path.Combine(Environment.CurrentDirectory, result.FileDownloadName);
            File.WriteAllBytes(outputFilePath, result.FileContents);

            var processStartInfo = new ProcessStartInfo("explorer", $@"""{outputFilePath}""");
            Process.Start(processStartInfo);
        }
        catch { }
    }

    private ActionContext GetTestActionContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRazorPdfGeneration();
        services.AddControllersWithViews();
        services.AddSingleton<ICompositeViewEngine>(new FakeViewEngine());

        var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = provider
        };

        var actionContext = new ActionContext(
           httpContext,
           new Microsoft.AspNetCore.Routing.RouteData(),
           new ControllerActionDescriptor());

        return actionContext;
    }

    private class UserController : Controller
    {
        public UserController(ActionContext actionContext)
        {
            ControllerContext = new ControllerContext(actionContext);
        }

        public Task<FileContentResult> Get(int id, string? downloadFileName = null, DateTimeOffset? lastModified = null)
        {
            ControllerContext.ActionDescriptor = new ControllerActionDescriptor
            {
                ActionName = nameof(Get)
            };

            return this.RazorPdf(
                model: new UserModel(id, "Foo", "Bar"),
                downloadFileName: downloadFileName,
                lastModified: lastModified);
        }
    }

    private class FakeViewEngine : ICompositeViewEngine
    {
        public IReadOnlyList<IViewEngine> ViewEngines => throw new NotImplementedException();

        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
        {
            return viewName switch
            {
                "Get" => ViewEngineResult.Found(viewName, new FakeView()),

                _ => ViewEngineResult.NotFound(viewName, Array.Empty<string>())
            };
        }

        public ViewEngineResult GetView(string? executingFilePath, string viewPath, bool isMainPage)
        {
            throw new NotImplementedException();
        }
    }

    private class FakeView : IView
    {
        public string Path => throw new NotImplementedException();

        public async Task RenderAsync(ViewContext context)
        {
            var model = (UserModel)context.ViewData.Model!;
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>User {model.Id} Details: {model.FirstName} {model.LastName}</title>
</head>
<body>
    <p>
        <strong>User ID:</strong> {model.Id}
    </p>
    <p>
        <strong>First Name:</strong> {model.FirstName}
    </p>
    <p>
        <strong>First Name:</strong> {model.LastName}
    </p>
</body>
</html>";
            await context.Writer.WriteAsync(html);
        }
    }

    private record UserModel(int Id, string FirstName, string LastName);
}
