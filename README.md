# razor-dink-to-pdf

Wrapper for DinkToPDF that enables creating PDFs via Razor views.

## Usage

Register the RazorPdfGeneration in your dependency registration at app start:

```csharp
public void ConfigureServices(IServiceCollection services)
{
	// other service registrations

	services.AddRazorPdfGeneration();
}
```

Return a PDF from an ASP.NET Controller Action just like how you would normally return a view result:

```csharp
public sealed class UserController : Controller
{
  public async Task<FileContentResult> PrintReport(int userId)
  {
	var model = await this.userService.GetByIdAsync(userId);
	return this.RazorPdf(
		model,
		downloadFileName: "user_report.pdf",
		lastModified: DateTimeOffset.UtcNow);
  }
}
```
