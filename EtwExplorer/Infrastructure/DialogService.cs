using Avalonia.Controls;

namespace EtwExplorer.Infrastructure {
	sealed class DialogService : IDialogService {
		public TViewModel CreateDialog<TViewModel, TWindow>()
			where TViewModel : DialogViewModelBase
			where TWindow : Window, new() {
			var window = new TWindow();
			var viewModel = (TViewModel?)Activator.CreateInstance(typeof(TViewModel), window)
				?? throw new InvalidOperationException($"Could not create {typeof(TViewModel).Name}");
			window.DataContext = viewModel;
			return viewModel;
		}
	}
}
