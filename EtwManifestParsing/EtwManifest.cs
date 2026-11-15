namespace EtwManifestParsing {
	public class EtwManifest {
		internal EtwManifest(string xml) {
			Xml = xml;
		}

		public string Xml { get; }
		public EtwEvent[] Events { get; internal set; } = [];
		public EtwKeyword[] Keywords { get; internal set; } = [];
		public EtwTemplate[] Templates { get; internal set; } = [];

		/// <summary>Null for legacy (MOF) providers, which declare no tasks.</summary>
		public EtwTask[]? Tasks { get; internal set; }

		public IDictionary<string, string>? StringTable { get; internal set; }
		public string ProviderName { get; internal set; } = string.Empty;
		public Guid ProviderGuid { get; internal set; }
		public string? ProviderSymbol { get; internal set; }

		public string? GetString(string messageId) {
			return StringTable != null && StringTable.TryGetValue(messageId, out var value) ? value : null;
		}

		/// <summary>Resolves a "$(string.Foo)" message reference through the string table; any other text is returned as is.</summary>
		internal string? ResolveMessage(string? message) {
			return message != null && message.StartsWith('$') ? GetString(message[9..^1]) : message;
		}
	}
}
