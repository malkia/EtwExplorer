using System.Xml.Linq;

namespace EtwManifestParsing {
	public sealed class EtwOpcode {
		public required string Name { get; init; }
		public string? Message { get; init; }
		public int Value { get; init; }
	}

	public sealed class EtwTask {
		public int Value { get; }
		public string Name { get; }

		public EtwOpcode[] Opcodes { get; }

		internal EtwTask(XElement element, EtwManifest manifest) {
			// name/value are required by the manifest schema; a missing one throws and is
			// surfaced by ManifestParser.Parse as "Failed to parse manifest XML".
			Name = element.Attribute("name")!.Value;
			Value = (int)element.Attribute("value")!;

			Opcodes = [.. element.Descendants("opcode").Select(node => new EtwOpcode {
				Name = node.Attribute("name")!.Value,
				Message = manifest.ResolveMessage((string?)node.Attribute("message")),
				Value = (int)node.Attribute("value")!
			})];
		}

		public override string ToString() => $"{Name} {Value} {Opcodes.Length} opcodes";
	}
}
