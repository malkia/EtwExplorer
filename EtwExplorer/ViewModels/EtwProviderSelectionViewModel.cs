using Avalonia.Collections;
using Avalonia.Controls;
using EtwExplorer.Infrastructure;
using Microsoft.Diagnostics.Tracing.Session;

namespace EtwExplorer.ViewModels {
	sealed record Provider(string Name, Guid Guid) {
		public bool IsLikelyName => !Name.Contains(' ');
	}

	sealed class EtwProviderSelectionViewModel : DialogViewModelBase {
		static readonly Provider[] _providers = [.. TraceEventProviders.GetPublishedProviders()
			.Select(g => new Provider(TraceEventProviders.GetProviderName(g), g))];
		readonly DataGridCollectionView _providerView;

		public EtwProviderSelectionViewModel(Window dialog) : base(dialog) {
			_providerView = new DataGridCollectionView(_providers) { Filter = FilterProvider };
			IsLikely = true;
		}

		public DataGridCollectionView ProviderView => _providerView;

		Provider? _selectedProvider;
		public Provider? SelectedProvider {
			get => _selectedProvider;
			set => SetProperty(ref _selectedProvider, value);
		}

		string? _searchText;
		public string? SearchText {
			get => _searchText;
			set {
				if (SetProperty(ref _searchText, value))
					_providerView.Refresh();
			}
		}

		bool _closeCurrentManifest = true;
		public bool CloseCurrentManifest {
			get => _closeCurrentManifest;
			set => SetProperty(ref _closeCurrentManifest, value);
		}

		bool _isLikely;
		public bool IsLikely {
			get => _isLikely;
			set {
				if (SetProperty(ref _isLikely, value))
					_providerView.Refresh();
			}
		}

		bool FilterProvider(object item) {
			var provider = (Provider)item;
			if (string.IsNullOrWhiteSpace(SearchText))
				return !IsLikely || provider.IsLikelyName;
			return provider.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
				|| provider.Guid.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase);
		}

		protected override void OnOK() {
			if (SelectedProvider != null)
				base.OnOK();
		}
	}
}
