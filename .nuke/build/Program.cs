using System;
using System.IO;
using System.Linq;
using NuGet.Versioning;
using Nuke.Common;
using Nuke.Common.ChangeLog;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable UnusedMember.Local

[GitHubActions(
	"continuous",
	GitHubActionsImage.WindowsLatest,
	On = new[] { GitHubActionsTrigger.Push },
	InvokedTargets = new[] { nameof(Release) })]
class Program: NukeBuild
{
	public static int Main() => Execute<Program>(x => x.Build);

	[Parameter("Use debug configuration")] readonly bool Debug;

	Configuration Configuration =>
		!IsLocalBuild ? Configuration.Release :
		Debug ? Configuration.Debug :
		Configuration.Release;
	
	bool IsReleasing => 
		ScheduledTargets.Contains(Release) ||
		RunningTargets.Contains(Release) ||
		FinishedTargets.Contains(Release);

	[Solution] readonly Solution Solution;
	
	[GitRepository] readonly GitRepository GitRepository;
	[GitVersion] readonly GitVersion GitVersion;
	
	static readonly AbsolutePath NukeDirectory = RootDirectory / ".nuke";
	static readonly AbsolutePath OutputDirectory = RootDirectory / ".output";

	AbsolutePath ArtifactsPattern => OutputDirectory / $"*.{PackageVersion}.nupkg";

	readonly ReleaseNotes[] ReleaseNotes = ChangelogTasks
		.ReadReleaseNotes(RootDirectory / "CHANGES.md")
		.ToArray();

	NuGetVersion PackageVersion =>
		ReleaseNotes.FirstOrDefault()?.Version ??
		throw new ArgumentException("No release notes found");

    static void RestoreSecretFile(string secretFile, string exampleFile)
	{
		if (File.Exists(RootDirectory / secretFile))
			return;

		Log.Warning(
			"Secret file '{SecretFile}' not found, copying example file '{ExampleFile}' instead",
			secretFile, exampleFile);
		CopyFile(RootDirectory / exampleFile, RootDirectory / secretFile);
	}
    
    static string GetNugetApiKey() =>
	    EnvironmentInfo.GetVariable<string>("NUGET_API_KEY").NullIfEmpty() ??
	    throw new Exception("NUGET_API_KEY is not set");

    static string GetGitHubApiKey() =>
	    EnvironmentInfo.GetVariable<string>("GITHUB_API_KEY").NullIfEmpty() ??
	    throw new Exception("GITHUB_API_KEY is not set");

	Target Clean => _ => _
		.Before(Restore)
		.Executes(() =>
		{
			RootDirectory
				.GlobDirectories("**/bin", "**/obj", "packages")
				.Where(p => !NukeDirectory.Contains(p))
				.ForEach(DeleteDirectory);
			EnsureCleanDirectory(OutputDirectory);
		});

	Target Restore => _ => _
		.After(Clean)
		.Executes(() =>
		{
            RestoreSecretFile(".secrets.cfg", "res/.secrets.example.cfg");
            RestoreSecretFile(".signing.snk", "res/.signing.example.snk");

			DotNetToolRestore();
			DotNet("paket restore");
			DotNetRestore(s => s.SetProjectFile(Solution));
		});

	Target Build => _ => _
		.DependsOn(Restore)
		.Executes(() =>
		{
			DotNetBuild(s => s
				.SetProjectFile(Solution)
				.SetConfiguration(Configuration)
				.SetProperty("IsReleasing", IsReleasing)
				.SetVersion(PackageVersion.ToString())
				.EnableNoRestore());
		});

	Target Rebuild => _ => _
		.DependsOn(Build).DependsOn(Clean)
		.Executes(() => { });

	Target Release => _ => _
		.DependsOn(Rebuild)
		.Produces(OutputDirectory / "*.nupkg")
		.Executes(() =>
		{
			DotNetPack(s => s
				.SetProject(Solution)
				.SetConfiguration(Configuration)
				.SetVersion(PackageVersion.ToString())
				.SetOutputDirectory(OutputDirectory)
				.EnableNoRestore()
				.EnableNoBuild());
		});

	Target PublishToNuget => _ => _
		.After(Release).After(PublishToGitHub)
		.Requires(() => ArtifactsPattern.GlobFiles().Any())
		.Executes(() =>
		{
			var token = GetNugetApiKey();

			DotNetNuGetPush(s => s
				.SetTargetPath(ArtifactsPattern)
				.SetSource("https://api.nuget.org/v3/index.json")
				.SetApiKey(token));
		});

	Target PublishToGitHub => _ => _
		.After(Release)
		.Requires(() => GitRepository.IsGitHubRepository())
		.Requires(() => ArtifactsPattern.GlobFiles().Any())
		.Executes(async () =>
		{
			var token = GetGitHubApiKey();
			var artifacts = ArtifactsPattern.GlobFiles().ToArray();
			await GitHubReleaser.Release(
				token,
				PackageVersion,
				GitRepository,
				GitVersion,
				ReleaseNotes.First(),
				artifacts);
		});

	Target Test => _ => _
		.After(Build)
		.Executes(() =>
		{
			DotNetTest(s => s
				.SetProjectFile(Solution)
				.SetConfiguration(Configuration));
		});
}
