using Avalonia.Collections;
using EtwExplorer.Infrastructure;
using EtwManifestParsing;

namespace EtwExplorer.ViewModels {
	sealed class StringsTabViewModel : TabViewModelBase {
		public sealed record Item(string Name, string Value);

		public override string Header => "Strings";

		readonly DataGridCollectionView _stringView;
		public StringsTabViewModel(EtwManifest manifest) {
			var strings = manifest.StringTable is { } table
				? table.Select(pair => new Item(pair.Key, pair.Value)).ToArray()
				: [];
			_stringView = new DataGridCollectionView(strings) { Filter = FilterString };
		}

		public DataGridCollectionView StringView => _stringView;

		string? _searchText;
		public string? SearchText {
			get => _searchText;
			set {
				if (SetProperty(ref _searchText, value))
					_stringView.Refresh();
			}
		}

		bool FilterString(object value) {
			if (string.IsNullOrWhiteSpace(SearchText))
				return true;
			var item = (Item)value;
			return item.Name.ContainsIgnoreCase(SearchText) || item.Value.ContainsIgnoreCase(SearchText);
		}
	}
}
