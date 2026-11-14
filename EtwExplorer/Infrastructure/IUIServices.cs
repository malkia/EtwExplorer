using Avalonia.Controls;

namespace EtwExplorer.Infrastructure {
	public interface IFileDialogService {
		Task<string?> GetFileForOpen(string filter, string? title = null);
		Task<string?> GetFileForSave(string filter, string? title = null);
	}

	public interface IMessageBoxService {
		void SetOwner(Window owner);
		Task ShowMessage(string text, string caption);
	}

	public interface IDialogService {
		TViewModel CreateDialog<TViewModel, TWindow>()
			where TViewModel : DialogViewModelBase
			where TWindow : Window, new();
	}

	public interface IUIServices {
		IFileDialogService FileDialogService { get; }
		IMessageBoxService MessageBoxService { get; }
		IDialogService DialogService { get; }
	}

	public sealed class UIServicesDefaults : IUIServices {
		public IFileDialogService FileDialogService { get; } = new FileDialogService();
		public IMessageBoxService MessageBoxService { get; } = new MessageBoxService();
		public IDialogService DialogService { get; } = new DialogService();
	}
}
