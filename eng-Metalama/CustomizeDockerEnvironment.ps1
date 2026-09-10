# Per-repo customization of the container environment, dot-sourced by DockerBuild.ps1 after the
# standard environment-variable set is assembled and before eng-Metalama/.g/Init.g.ps1 is written.
# Mutate $ContainerEnvironmentVariables in place (add / change / remove keys).
# See "Customizing the container environment (product repos)" in PostSharp.Engineering's
# doc/dockerbuild.md.

param(
    [hashtable] $ContainerEnvironmentVariables,  # full normal var set; mutate in place
    [string]    $DockerfileName,                 # leaf Dockerfile, e.g. 'build.Dockerfile' / 'claude.Dockerfile'
    [switch]    $Claude                          # true when running the Claude leaf
)

# Work around https://github.com/dotnet/arcade/issues/15970: building via the Arcade toolset
# (microsoft.dotnet.arcade.sdk/<ver>/tools/Build.proj) fails to resolve packages from the GLOBAL
# NuGet cache -- e.g. "NETSDK1064: Package Microsoft.CodeAnalysis.Analyzers ... was not found"
# (also BannedApiAnalyzers, MessagePackAnalyzer) -- even though the packages are physically on disk.
# Restore into a repo-local '.packages' folder instead, exactly as azure-pipelines.yml and
# eng/make-bootstrap.ps1 do. This OVERRIDES DockerBuild.ps1's default of $USERPROFILE\.nuget\packages
# (which, on the machine-account TeamCity agent, is the global C:\WINDOWS\system32\config\systemprofile
# cache that triggers the bug).
#
# Applies to every DockerBuild leaf (build and Claude). The container bind-mounts the repo at the
# same path as the host, so an absolute host path resolves identically inside the container.
$repoRoot = Split-Path -Parent $PSScriptRoot
$ContainerEnvironmentVariables['NUGET_PACKAGES'] = Join-Path $repoRoot '.packages'
$ContainerEnvironmentVariables['RESTORENOCACHE'] = 'true'

# From .NET 11 on, dotnet-install.ps1 downloads a .tar.gz on Windows instead of a .zip and extracts it by
# invoking tar as an external process. Every generated Windows image puts C:\git\usr\bin ahead of System32 in
# PATH, so that call resolves to the GNU tar of Git for Windows, which reads the leading 'C:' of the archive
# path as the name of a remote host and fails with "Cannot connect to C: resolve failed".
#
# PostSharp.Engineering already sets DOTNET_INSTALL_SKIP_TAR on the RUN instruction that installs the SDK into
# the image, so building the image works. It does not cover the second installation, which Arcade performs
# inside the container: this repository's global.json declares tools.runtimes, and eng/common/tools.ps1 then
# ignores the SDK of the image and installs its own copy under <repo>\.dotnet, without the variable. That is
# what failed build 336139 of the Roslyn 5.11 merge, in the step that runs eng/build.ps1.
#
# Setting the variable for the whole container covers that installation and any later one. It is inert for a
# .NET 10 or earlier SDK, which dotnet-install.ps1 downloads as a .zip in the first place.
$ContainerEnvironmentVariables['DOTNET_INSTALL_SKIP_TAR'] = '1'
