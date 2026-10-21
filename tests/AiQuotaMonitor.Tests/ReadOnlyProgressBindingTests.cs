using System.Xml.Linq;

namespace AiQuotaMonitor.Tests;

public sealed class ReadOnlyProgressBindingTests
{
    [Fact]
    public void EveryProgressValueBindingInWindowResourcesIsOneWay()
    {
        var xamlRoot = Path.Combine(AppContext.BaseDirectory, "AppXaml");
        var xamlFiles = Directory.GetFiles(xamlRoot, "*.xaml", SearchOption.AllDirectories);
        Assert.NotEmpty(xamlFiles);

        var bindings = xamlFiles
            .SelectMany(file => XDocument.Load(file).Descendants().Attributes())
            .Select(attribute => attribute.Value)
            .Where(value => value.Contains("{Binding ProgressValue", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(bindings);
        Assert.All(bindings, binding => Assert.True(
            binding.Contains("Mode=OneWay", StringComparison.Ordinal),
            $"Expected a read-only ProgressValue binding to be OneWay, received: {binding}"));
    }
}
