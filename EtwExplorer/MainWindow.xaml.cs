using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EtwExplorer {
	public partial class MainWindow : Window {
		public MainWindow() {
			AvaloniaXamlLoader.Load(this);
			Title = Constants.WindowTitle;
		}
	}
}
