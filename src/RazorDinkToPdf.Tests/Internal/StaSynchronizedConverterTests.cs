using System.Reflection;
using System.Runtime.InteropServices;
using RazorDinkToPdf.Internal;

namespace RazorDinkToPdf.Tests.Internal;

public sealed class StaSynchronizedConverterTests
{
    [Fact]
    public void StartThread_StartsInExpectedApartmentState()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.True(true);
            return;
        }

        var converter = StaSynchronizedConverter.Instance;

        var startMethod = converter.GetType().GetMethod("StartThread", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(startMethod);
        startMethod.Invoke(converter, Array.Empty<object>());

        var threadProperty = converter.GetType().GetField("conversionThread", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(threadProperty);
        var conversionThread = threadProperty.GetValue(converter) as Thread;

        Assert.NotNull(conversionThread);

        var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ApartmentState.STA
            : ApartmentState.Unknown;

        var actual = conversionThread.GetApartmentState();

        Assert.Equal(expected, actual);
    }
}
