using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace EtwExplorer.Infrastructure {
	sealed class FileDialogService : IFileDialogService {
		public async Task<string?> GetFileForOpen(string filter, string? title = null) {
			var owner = GetOwner() ?? throw new InvalidOperationException("A desktop window is required to open a file picker.");

			var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
				Title = title,
				AllowMultiple = false,
				FileTypeFilter = ParseFilter(filter)
			});
			return files.FirstOrDefault()?.Path.LocalPath;
		}

		public async Task<string?> GetFileForSave(string filter, string? title = null) {
			var owner = GetOwner() ?? throw new InvalidOperationException("A desktop window is required to open a file picker.");

			var types = ParseFilter(filter);
			var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
				Title = title,
				FileTypeChoices = types,
				DefaultExtension = types.FirstOrDefault()?.Patterns?.FirstOrDefault()?.TrimStart('*', '.')
			});
			return file?.Path.LocalPath;
		}

		static Window? GetOwner() =>
			(Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

		static IReadOnlyList<FilePickerFileType> ParseFilter(string filter) {
			var parts = filter.Split('|');
			var types = new List<FilePickerFileType>();
			for (var index = 0; index + 1 < parts.Length; index += 2) {
				var patterns = parts[index + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				types.Add(new FilePickerFileType(parts[index]) { Patterns = patterns });
			}
			return types;
		}
	}
}
