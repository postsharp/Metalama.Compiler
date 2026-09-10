// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Metalama>

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// Identifies this compiler server to the telemetry host. This file has no upstream counterpart.
    /// Metalama Compiler is a fork of Roslyn that runs a transformer pipeline over every compilation, so its
    /// timings, its diagnostics and its cache behaviour are not those of the compiler server shipped in the
    /// .NET SDK. An event that does not name the compiler that produced it would be read as data about a
    /// compiler that did not run.
    /// </summary>
    internal static class MetalamaCompilerTelemetry
    {
        /// <summary>
        /// The telemetry event name. The host prefixes it, as it does every event reported through
        /// <c>IBuildEngine5.LogTelemetry</c>: Visual Studio prefixes <c>vs/</c> and the dotnet command-line
        /// interface prefixes <c>dotnet/cli/msbuild/</c>.
        /// </summary>
        internal const string EventName = "roslyn/compilerfork";

        /// <summary>
        /// The name of the fork, reported by every event this compiler server produces.
        /// </summary>
        internal const string ForkName = "Metalama.Compiler";

        private const string UnknownVersion = "unknown";

        /// <summary>
        /// The version of Metalama Compiler, read from the <c>PackageVersion</c> assembly metadata that
        /// Microsoft.CodeAnalysis.csproj stamps on the compiler assembly.
        /// </summary>
        internal static string ForkVersion { get; } = GetAssemblyMetadata("PackageVersion");

        /// <summary>
        /// The version of the Roslyn that this fork is built from, read from the <c>RoslynVersion</c>
        /// assembly metadata stamped on the same assembly. The assembly version cannot be used for this,
        /// because this fork stamps its own product version on every assembly it builds.
        /// </summary>
        internal static string RoslynVersion { get; } = GetAssemblyMetadata("RoslynVersion");

        /// <summary>
        /// Returns the event that identifies this compiler as Metalama Compiler. It is reported for every
        /// request, including a request that produces no other telemetry, because a build that reports
        /// nothing else is still a build that this compiler performed.
        /// </summary>
        internal static BuildTelemetryEvent CreateForkEvent()
            => new BuildTelemetryEvent(
                EventName,
                new Dictionary<string, string>(3)
                {
                    ["fork"] = ForkName,
                    ["forkversion"] = ForkVersion,
                    ["roslynversion"] = RoslynVersion,
                });

        /// <summary>
        /// Reads one assembly metadata value from the compiler assembly. A key can be stamped more than
        /// once, because the Metalama item group of Microsoft.CodeAnalysis.csproj adds
        /// <c>PackageVersion</c> that the build system also adds, and one of the two is empty in a build
        /// that does not set the product version. Take the first value that has one.
        /// </summary>
        private static string GetAssemblyMetadata(string key)
            => typeof(Compilation).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                   .Where(a => a.Key == key)
                   .Select(a => a.Value)
                   .FirstOrDefault(value => !string.IsNullOrEmpty(value))
               ?? UnknownVersion;
    }
}
