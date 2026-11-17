using ATuimStudio.Common;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace ATuimStudio.Extensions.Core
{
	sealed class PhysicalDiskSolutionDataStorage : ISolutionDataStorage, IDisposable
	{
		readonly MSBuildWorkspace _workspace;

		static PhysicalDiskSolutionDataStorage()
		{
			if (!MSBuildLocator.IsRegistered)
				if (MSBuildLocator.RegisterDefaults().Version.Major < 10)
					throw new InvalidOperationException("No MsBuild version 10+ found.");
		}

		public PhysicalDiskSolutionDataStorage()
		{
			_workspace = MSBuildWorkspace.Create();
		}

		public async Task<ISolutionData> GetSolutionDataAsync(string path, CancellationToken cancellationToken)
		{
			Task<Solution> solutionOpenTask = _workspace.OpenSolutionAsync(path, cancellationToken: cancellationToken);
			Task<SolutionModel>? serializerTask = SolutionSerializers.GetSerializerByMoniker(path)?.OpenAsync(path, cancellationToken);

			Solution sln = await solutionOpenTask;
			SolutionModel? solutionModel = serializerTask is null ? null : await serializerTask;

			ProjectData[] mappedProjects = [.. sln.Projects.Select(proj =>
						new ProjectData(proj.Name, Path.GetDirectoryName(proj.FilePath)!, proj.FilePath!, BuildProjectDocumentsStructure(proj.Documents, Path.GetDirectoryName(proj.FilePath)!))
						).OrderBy(static x => x.Name, PathHelper.PathComparer)];

			string solutionDir = Path.GetDirectoryName(sln.FilePath)!;
			Dictionary<SolutionFolderModel, ProjectData[]> projectsByParentFolder = solutionModel == null
				? []
				: mappedProjects
					.Join(solutionModel.SolutionProjects,
						static p => p.FilePath,
						sp => Path.Combine(solutionDir, sp.FilePath),
						static (p, sp) => (p, sp),
						PathHelper.PathEqualityComparer)
					.Where(x => x.sp.Parent is not null)
					.GroupBy(x => x.sp.Parent!)
					.ToDictionary(static g => g.Key, static g => g.Select(static x => x.p).ToArray());
			IReadOnlyCollection<ISolutionFolderData> MapSolutionFolder(IReadOnlyCollection<TreeMaker.INode<SolutionFolderModel>> nodes)
				=> new MappingReadOnlyCollection<TreeMaker.INode<SolutionFolderModel>, ISolutionFolderData>(nodes,
					node => new SolutionFolderData(node.Name, node.Data.Path, MapSolutionFolder(node.Children),
						projectsByParentFolder.TryGetValue(node.Data, out var folderProjects) ? folderProjects : [],
						node.Data.Files == null ? null : new MappingReadOnlyCollection<string, IProjectFileData>(node.Data.Files, f => new ProjectFileData(f, Path.Combine(solutionDir, f)))));

			SolutionData slnInfo = new SolutionData(sln, solutionDir, Path.GetFileNameWithoutExtension(sln.FilePath)!,
					mappedProjects,
					solutionModel == null
						? []
						: MapSolutionFolder(TreeMaker.Make(solutionModel.SolutionFolders.Select(sf => (sf.Path.Split('/', StringSplitOptions.RemoveEmptyEntries), sf)))));
			return slnInfo;
		}

		#region BuildProjectDocumentsStructure
		static IReadOnlyCollection<IProjectFilesystemItemData> BuildProjectDocumentsStructure(IEnumerable<Document> documents, string projDir)
		{
			IEnumerable<TreeMaker.INode<Document>> tree = TreeMaker.Make(
				documents
					.Where(static x => x.Folders.Count == 0 || !x.Folders[0].EqualsOrdinal("obj"))
					.Select<Document, (string[], Document)>(static document => ([.. document.Folders, document.Name], document))
					
				);

			static IReadOnlyCollection<IProjectFilesystemItemData> ConvertNodes(IEnumerable<TreeMaker.INode<Document>> nodes, string parentPath)
				=> [.. nodes.OrderBy(static x => x.Name, PathHelper.PathComparer).Select(x => Convert(x, parentPath))];
			static IProjectFilesystemItemData Convert(TreeMaker.INode<Document> node, string parentPath)
			{
				string name = node.Name;
				string path = Path.Combine(parentPath, name);
				return node.Children.Count != 0
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

		public void Dispose()
		{
			_workspace.Dispose();
		}

		#region data implementations
		internal sealed class SolutionData : ISolutionData
		{
			public SolutionData(Solution solution, string path, string name, IReadOnlyCollection<IProjectData> projects, IReadOnlyCollection<ISolutionFolderData> folders)
			{
				RawData = solution;
				Path = path;
				Name = name;
				Projects = projects;
				Folders = folders;
			}

			public string Name { get; }
			public string Path { get; }
			public object? RawData { get; set; }
			public IReadOnlyCollection<IProjectData> Projects { get; }
			public IReadOnlyCollection<ISolutionFolderData> Folders { get; }
		}

		sealed record ProjectData(
			string Name,
			string Path,
			string FilePath,
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

		sealed record SolutionFolderData(
			string Name,
			string Path,
			IReadOnlyCollection<ISolutionFolderData> Folders,
			IReadOnlyCollection<IProjectData> Projects,
			IReadOnlyCollection<IProjectFileData>? Files
			) : ISolutionFolderData;
		#endregion data implementations
	}
}
