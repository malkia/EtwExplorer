using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EtwExplorer.Views {
	/// <summary>Search label and text box bound to the view model's SearchText.</summary>
	public partial class SearchBox : UserControl {
		public static readonly StyledProperty<double> TextBoxWidthProperty =
			AvaloniaProperty.Register<SearchBox, double>(nameof(TextBoxWidth), 300);

		public double TextBoxWidth {
			get => GetValue(TextBoxWidthProperty);
			set => SetValue(TextBoxWidthProperty, value);
		}

		public SearchBox() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}
