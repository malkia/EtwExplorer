using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EtwExplorer.Infrastructure;
using EtwExplorer.ViewModels;

namespace EtwExplorer {
	public partial class App : Application {
		public override void Initialize() => AvaloniaXamlLoader.Load(this);

		public override void OnFrameworkInitializationCompleted() {
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
				var ui = new UIServicesDefaults();
				var window = new MainWindow { DataContext = new MainViewModel(ui) };
				ui.MessageBoxService.SetOwner(window);
				desktop.MainWindow = window;
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}
