using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Windows.Input;

namespace EtwExplorer.Infrastructure {
	public abstract class DialogViewModelBase : BindableBase {
		protected DialogViewModelBase(Window dialog) {
			Dialog = dialog;
			OKCommand = new DelegateCommand(OnOK);
			CancelCommand = new DelegateCommand(OnCancel);
		}

		protected Window Dialog { get; }

		public ICommand OKCommand { get; }
		public ICommand CancelCommand { get; }

		public Task<bool?> ShowDialogAsync() {
			var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
				?? throw new InvalidOperationException("A desktop window is required to show the provider dialog.");
			return Dialog.ShowDialog<bool?>(owner);
		}

		protected virtual void OnOK() => Dialog.Close(true);

		protected virtual void OnCancel() => Dialog.Close(false);
	}
}
