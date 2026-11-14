using Avalonia.Controls.ApplicationLifetimes;
using EtwExplorer.Infrastructure;
using EtwExplorer.Views;
using EtwManifestParsing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace EtwExplorer.ViewModels {
	sealed class MainViewModel : BindableBase {
		public readonly IUIServices UI;
		readonly ObservableCollection<TabViewModelBase> _tabs = new();
		public IList<TabViewModelBase> Tabs => _tabs;

		public EtwManifest? Manifest { get; private set; }
		public EtwKeyword[]? Keywords { get; private set; }

		string? _filename;
		public string? FileName {
			get => _filename;
			set => SetProperty(ref _filename, value);
		}

		string? _providerName;
		public string? ProviderName {
			get => _providerName;
			set => SetProperty(ref _providerName, value);
		}

		public MainViewModel(IUIServices ui, EtwManifest? manifest = null) {
			UI = ui;
			Manifest = manifest;
			if (manifest != null) {
				AddTabs();
				ProviderName = manifest.ProviderName;
			}
		}

		public MainViewModel(IUIServices ui, EtwKeyword[] keywords, string providerName) {
			UI = ui;
			ProviderName = providerName;
			Keywords = keywords;
			AddTabs();
		}

		void AddTabs() {
			if (Manifest is { } manifest) {
				Tabs.Add(new SummaryTabViewModel(manifest));
				Tabs.Add(new EventsTabViewModel(manifest));
				Tabs.Add(new KeywordsTabViewModel(manifest));
				Tabs.Add(new StringsTabViewModel(manifest));
				Tabs.Add(new XmlTabViewModel(manifest.Xml));
			}
			else if (Keywords is { } keywords) {
				Tabs.Add(new KeywordsTabViewModel(keywords));
			}
			SelectedTab = Tabs.Count > 0 ? Tabs[0] : null;
		}

		public ICommand ExitCommand => new DelegateCommand(() => {
			if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
				desktop.Shutdown();
		});

		public ICommand OpenXmlCommand => new DelegateCommand(async () => await OpenXmlAsync());

		async Task OpenXmlAsync() {
			try {
				var filename = await UI.FileDialogService.GetFileForOpen("Xml Files|*.xml", "Open XML Manifest");
				if (filename != null)
					DoOpenFile(filename);
			}
			catch (Exception ex) {
				await UI.MessageBoxService.ShowMessage($"Error: {ex.Message}", Constants.AppTitle);
			}
		}

		public ICommand CloseCommand => new DelegateCommand(DoClose);

		void DoClose() {
			Manifest = null;
			Keywords = null;
			Tabs.Clear();
			FileName = null;
			RaisePropertyChanged(nameof(Manifest));
		}

		public ICommand OpenRegisteredCommand => new DelegateCommand(async () => await OpenRegisteredAsync());

		async Task OpenRegisteredAsync() {
			var vm = UI.DialogService.CreateDialog<EtwProviderSelectionViewModel, EtwProviderSelectionDialog>();
			if (await vm.ShowDialogAsync() != true || vm.SelectedProvider is not { } provider)
				return;

			if (vm.CloseCurrentManifest)
				DoClose();
			try {
				var xml = string.Empty;
				try {
					xml = RegisteredTraceEventParser.GetManifestForRegisteredProvider(provider.Guid);
				}
				catch (ApplicationException ae) {
					try {
						DoOpenWmiEventTrace(provider.Guid);
						await UI.MessageBoxService.ShowMessage($"{ae.Message}\n\nShowing WMI EventTrace class details instead.", Constants.AppTitle);
					}
					catch {
						throw ae;
					}
				}

				if (xml.Length > 0)
					DoOpenXml(xml);

				Keywords = null;
			}
			catch (Exception e) {
				var keywords = TraceEventProviders.GetProviderKeywords(provider.Guid).Select(info => new EtwKeyword {
					Name = info.Name,
					Mask = info.Value,
					Message = info.Description
				}).ToArray();
				await UI.MessageBoxService.ShowMessage($"{e.Message}\nShowing keywords only.", Constants.AppTitle);

				if (vm.CloseCurrentManifest) {
					DoClose();
					Keywords = keywords;
					AddTabs();
				}
				else {
					var winvm = new MainViewModel(UI, keywords, provider.Name);
					new MainWindow { DataContext = winvm }.Show();
				}
			}
		}

		void DoOpenXml(string xml) => DoOpen(ManifestParser.Parse(xml));

		void DoOpenWmiEventTrace(Guid provider) => DoOpen(ManifestParser.ParseWmiEventTraceClass(provider));

		void DoOpen(EtwManifest manifest) {
			if (Manifest == null) {
				Manifest = manifest;
				RaisePropertyChanged(nameof(Manifest));
				ProviderName = Manifest.ProviderName;
				AddTabs();
			}
			else {
				new MainWindow { DataContext = new MainViewModel(UI, manifest) }.Show();
			}
		}

		TabViewModelBase? _selectedTab;
		public TabViewModelBase? SelectedTab {
			get => _selectedTab;
			set => SetProperty(ref _selectedTab, value);
		}

		void DoOpenFile(string filename) {
			var manifest = ManifestParser.Parse(File.ReadAllText(filename));
			if (Manifest == null) {
				Manifest = manifest;
				FileName = filename;
				ProviderName = Manifest.ProviderName;
				RaisePropertyChanged(nameof(Manifest));
				AddTabs();
			}
			else {
				var vm = new MainViewModel(UI, manifest) { FileName = filename };
				new MainWindow { DataContext = vm }.Show();
			}
		}

		public DelegateCommand SaveXmlCommand => new DelegateCommand(async () => await SaveXmlAsync(), () => Manifest != null).ObservesProperty(() => Manifest);

		async Task SaveXmlAsync() {
			try {
				var filename = await UI.FileDialogService.GetFileForSave("XML files|*.xml|All Files|*.*");
				if (filename != null && Manifest is { } manifest)
					File.WriteAllText(filename, manifest.Xml);
			}
			catch (Exception ex) {
				await UI.MessageBoxService.ShowMessage(ex.Message, Constants.AppTitle);
			}
		}
	}
}
