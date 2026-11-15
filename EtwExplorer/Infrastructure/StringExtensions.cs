namespace EtwExplorer.Infrastructure {
	static class StringExtensions {
		public static bool ContainsIgnoreCase(this string? value, string text) =>
			value?.Contains(text, StringComparison.OrdinalIgnoreCase) == true;
	}
}
