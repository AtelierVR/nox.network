namespace Nox.Network.Assets {
	/// <summary>
	/// Free-form file attribute (<c>{ "key": "platform", "value": "windows" }</c>).
	/// Attributes are not Nox tags: they describe how a file must be matched by the client
	/// (recommended keys: <c>platform</c>, <c>arch</c>, <c>engine</c>).
	/// </summary>
	public interface IAssetAttribute {
		string Key { get; }

		string Value { get; }
	}
}
