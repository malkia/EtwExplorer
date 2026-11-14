using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;

namespace EtwExplorer.Infrastructure {
	sealed class MessageBoxService : IMessageBoxService {
		Window? _owner;

		public void SetOwner(Window owner) => _owner = owner;

		public async Task ShowMessage(string text, string caption) {
			var owner = _owner ?? (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
				?? throw new InvalidOperationException("A desktop window is required to show a message.");

			var dialog = new Window {
				Title = caption,
				Width = 440,
				SizeToContent = SizeToContent.Height,
				MinHeight = 130,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				Content = new StackPanel {
					Margin = new Avalonia.Thickness(16),
					Spacing = 16,
					Children = {
						new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
						new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 80 }
					}
				}
			};
			((Button)((StackPanel)dialog.Content!).Children[1]).Click += (_, _) => dialog.Close();
			await dialog.ShowDialog(owner);
		}
	}
}
