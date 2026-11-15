using Avalonia.Collections;
using EtwExplorer.Infrastructure;
using EtwManifestParsing;

namespace EtwExplorer.ViewModels {
	sealed class EventsTabViewModel : TabViewModelBase {
		public override string Header => "Events";

		readonly DataGridCollectionView _eventView;
		public EventsTabViewModel(EtwManifest manifest) {
			var events = manifest.Events.Select(evt => new EtwEventViewModel(evt, manifest)).ToArray();
			_eventView = new DataGridCollectionView(events) { Filter = FilterEvent };
		}

		public DataGridCollectionView EventView => _eventView;

		string? _searchText;
		public string? SearchText {
			get => _searchText;
			set {
				if (SetProperty(ref _searchText, value))
					_eventView.Refresh();
			}
		}

		bool FilterEvent(object item) {
			if (string.IsNullOrWhiteSpace(SearchText))
				return true;
			var evt = ((EtwEventViewModel)item).Event;
			return evt.Symbol.ContainsIgnoreCase(SearchText) || evt.Opcode.ContainsIgnoreCase(SearchText) || evt.Task.ContainsIgnoreCase(SearchText);
		}
	}
}
