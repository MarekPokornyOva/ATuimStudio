namespace ATuimStudio.Extensions.Core
{
	public interface INamedData
	{
		string Name { get; }
	}

	public interface ISolutionData: INamedData
	{
		string Path { get; }
		object? RawData { get; set; }
		IReadOnlyCollection<IProjectData> Projects { get; }
		IReadOnlyCollection<ISolutionFolderData> Folders { get; }
	}

	public interface IProjectData : INamedData
	{
		string Path { get; }
		IReadOnlyCollection<IProjectItemData> Items { get; }
	}

	public interface IProjectItemData : INamedData
	{
	}

	public interface IProjectFilesystemItemData : IProjectItemData
	{
		string Path { get; }
	}

	public interface IProjectFileData : IProjectFilesystemItemData
	{
	}

	public interface IProjectDirectoryData : IProjectFilesystemItemData
	{
		IReadOnlyCollection<IProjectFilesystemItemData> Items { get; }
	}

	public interface ISolutionFolderData : INamedData
	{
		string Path { get; }
		IReadOnlyCollection<ISolutionFolderData> Folders { get; }
		IReadOnlyCollection<IProjectData> Projects { get; }
		IReadOnlyCollection<IProjectFileData>? Files { get; }
	}
}
