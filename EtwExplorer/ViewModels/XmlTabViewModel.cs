namespace EtwExplorer.ViewModels {
	sealed class XmlTabViewModel : TabViewModelBase {
		public override string Header => "XML";

		public string Xml { get; }
		public XmlTabViewModel(string xml) {
			Xml = xml;
		}


	}
}
