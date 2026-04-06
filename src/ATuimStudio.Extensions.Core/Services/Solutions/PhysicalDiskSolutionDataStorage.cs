using ATuimStudio.Common;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace ATuimStudio.Extensions.Core
{
	sealed class PhysicalDiskSolutionDataStorage : ISolutionDataStorage
	{
		public async Task<ISolutionData> GetSolutionDataAsync(string path, CancellationToken cancellationToken)
		{
			if (!MSBuildLocator.IsRegistered)
				MSBuildLocator.RegisterDefaults();

			SolutionData slnInfo;
			MSBuildWorkspace ws = MSBuildWorkspace.Create();
			try
			{
				Solution sln = await ws.OpenSolutionAsync(path, cancellationToken: cancellationToken);

				//string solutionDir = Path.GetDirectoryName(sln.FilePath)!;
				slnInfo = new SolutionData(sln, Path.GetDirectoryName(sln.FilePath)!, Path.GetFileNameWithoutExtension(sln.FilePath)!, [.. sln.Projects.Select(proj=>
					new ProjectData(proj.Name, Path.GetDirectoryName(proj.FilePath)!, BuildProjectDocumentsStructure(proj.Documents, Path.GetDirectoryName(proj.FilePath)!))
					).OrderBy(static x=>x.Name, PathHelper.PathComparer)]);
				return slnInfo;
			}
			catch
			{
				ws.Dispose();
				throw;
			}
		}

		#region BuildProjectDocumentsStructure
		readonly static char[] _pathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
		static IReadOnlyCollection<IProjectFilesystemItemData> BuildProjectDocumentsStructure(IEnumerable<Document> documents, string projDir)
		{
			IEnumerable<TreeMaker.INode<Document>> tree = TreeMaker.Make(
				documents.Select(document => (Segments: Path.GetRelativePath(projDir, document.FilePath!).Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries), document))
					.Where(static x => x.Segments.Length > 0 && !x.Segments[0].EqualsOrdinal("obj"))
				);

			static IReadOnlyCollection<IProjectFilesystemItemData> ConvertNodes(IEnumerable<TreeMaker.INode<Document>> nodes, string parentPath)
				=> [.. nodes.OrderBy(static x => x.Name, PathHelper.PathComparer).Select(x => Convert(x, parentPath))];
			static IProjectFilesystemItemData Convert(TreeMaker.INode<Document> node, string parentPath)
			{
				string name = node.Name;
				string path = Path.Combine(parentPath, name);
				return node.Children.Any()
					? new ProjectDirectoryData(name, path, ConvertNodes(node.Children, path))
					: new ProjectFileData(name, path);
			}
			return ConvertNodes(tree, projDir);
		}
		#endregion BuildProjectDocumentsStructure

		public Task SaveDocumentAsync(string path, string content, CancellationToken cancellationToken)
			=> File.WriteAllTextAsync(path, content, cancellationToken);

		public void DeleteFile(string path)
			=> File.Delete(path);

		#region data implementations
		internal sealed class SolutionData : ISolutionData, IDisposable
		{
			public SolutionData(Solution solution, string path, string name, IReadOnlyCollection<IProjectData> projects)
			{
				RawData = solution;
				Path = path;
				Name = name;
				Projects = projects;
			}

			public string Name { get; }
			public string Path { get; }
			public object? RawData { get; set; }
			public IReadOnlyCollection<IProjectData> Projects { get; }

			public void Dispose()
			{
				((Solution)RawData!).Workspace.Dispose();
			}
		}

		sealed record ProjectData(
			string Name,
			string Path,
			IReadOnlyCollection<IProjectItemData> Items
			) : IProjectData;

		internal record ProjectFileData(
			string Name,
			string Path
			) : IProjectFileData;

		sealed record ProjectDirectoryData(
			string Name,
			string Path,
			IReadOnlyCollection<IProjectFilesystemItemData> Items
			) : IProjectDirectoryData;
		#endregion data implementations
	}
}
