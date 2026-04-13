using JetBrains.Annotations;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using Microsoft.Extensions.Logging;
using ReSharperPlugin.DotNetDisassembler.JitDisasm;

namespace ReSharperPlugin.DotNetDisassembler.JitDisasmAdapters;

public static class JitDisasmTargetFrameworkFactory
{
    private static readonly ILogger Logger = JitDisasmLoggerFactory.Create<JitDisasmTargetFramework>();

    public static JitDisasmTargetFramework Create(TargetFrameworkId tfmId)
    {
        var tfmString = GetTfmString(tfmId);

        // .NET Core/5+: netcoreapp*, net5-* (exclude net1-4 = old .NET Framework, netstandard)
        var isNetCore = tfmString.StartsWith("netcoreapp")
                        || (tfmString.StartsWith("net") && tfmId.Version.Major >= 5);

        return new JitDisasmTargetFramework(tfmString, tfmId.Version, isNetCore);
    }

    public static JitDisasmTargetFramework Create([NotNull] string tfmString)
    {
        var tfmId = TargetFrameworkId.Create(tfmString);
        return Create(tfmId);
    }

    // For non-platform TFMs (e.g. "net9.0") returns UniqueString as is.
    // For platform-specific TFMs, UniqueString is normalized (e.g. "net9.0-ios" → "net9.0-ios26.2")
    // which doesn't match the csproj TargetFramework value and can cause build errors (e.g. NETSDK1005),
    // so we retrieve the original value via internal OriginalTargetFrameworkText.
    private static string GetTfmString(TargetFrameworkId tfmId)
    {
        if (!tfmId.NuGetFramework.HasPlatform)
            return tfmId.UniqueString;

        var original = tfmId.GetType().GetProperty("OriginalTargetFrameworkText")?.GetValue(tfmId) as string;
        if (original != null)
            return original;

        Logger.LogWarning("Could not resolve original TFM alias for '{0}'. " +
                          "Internal API may have changed. Using normalized form which may cause build errors (e.g. NETSDK1005)", tfmId.UniqueString);
        return tfmId.UniqueString;
    }
}