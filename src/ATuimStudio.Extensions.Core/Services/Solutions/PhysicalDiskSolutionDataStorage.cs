using ATuimStudio.Common;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.SolutionPersistence;
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
			Task<Solution> solutionLoader = _workspace.OpenSolutionAsync(path, cancellationToken: cancellationToken);

			SolutionModel? solutionModel = null;
			ISolutionSerializer? serializer = SolutionSerializers.GetSerializerByMoniker(path);
			if (serializer != null)
			{
				//string basePath = Path.GetDirectoryName(path)!;
				solutionModel = await serializer.OpenAsync(path, cancellationToken).ConfigureAwait(false);
				//solutionModel.SolutionFolders
				//solutionModel.SolutionProjects.Select(p => p.Parent == null ? null : p.Parent.Path))];
			}
			Solution sln = await solutionLoader;
#error dodelat solutionFolders

			//string solutionDir = Path.GetDirectoryName(sln.FilePath)!;
			SolutionData slnInfo = new SolutionData(sln, Path.GetDirectoryName(sln.FilePath)!, Path.GetFileNameWithoutExtension(sln.FilePath)!, [.. sln.Projects.Select(proj=>
					new ProjectData(proj.Name, Path.GetDirectoryName(proj.FilePath)!, BuildProjectDocumentsStructure(proj.Documents, Path.GetDirectoryName(proj.FilePath)!))
					).OrderBy(static x=>x.Name, PathHelper.PathComparer)]);
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

		public void Dispose()
		{
			_workspace.Dispose();
		}

		#region data implementations
		internal sealed class SolutionData : ISolutionData
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
