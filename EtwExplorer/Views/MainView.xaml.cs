using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EtwExplorer.Views {
	public partial class MainView : UserControl {
		public MainView() => AvaloniaXamlLoader.Load(this);
	}
}
