# razor-dink-to-pdf

Wrapper for [DinkToPDF](https://github.com/rdvojmoc/DinkToPdf) that enables creating PDFs via Razor views.

## Usage

Register the RazorPdfGeneration services in your dependency registration at app start:

```csharp
public void ConfigureServices(IServiceCollection services)
{
	// other service registrations

	services.AddRazorPdfGeneration();
}
```

Return a PDF from an ASP.NET Controller Action just like you would normally return a view result:

```csharp
public sealed class UserController : Controller
{
	[HttpGet]
	public async Task<FileContentResult> PrintReport(
		int userId
		[FromServices] IUserService userService)
	{
		var model = await userService.GetReportDataAsync(userId);

		// Renders the Razor view at ~/Views/User/PrintReport.cshtml (by default),
		// then generates a PDF from the HTML, and returns the PDF as a FileContentResult
		return this.RazorPdf(
			model,
			downloadFileName: "user_report.pdf",
			lastModified: DateTimeOffset.UtcNow);
	}
}
```
