namespace RazorDinkToPdf;

public sealed class RazorPdfGenerationException : Exception
{
    public RazorPdfGenerationException(string message)
        : base(message)
    {
    }
}
