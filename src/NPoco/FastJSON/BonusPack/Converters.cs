using System;

namespace NPoco.FastJSON.BonusPack
{
	/// <summary>
	/// Contains extra <see cref="IJsonConverter"/>s to help serializing extra types. Those converters will not automatically get effective unless they are applied to corresponding types via the <see cref="SerializationManager.Override{T}(TypeOverride)"/> method or members via the <see cref="SerializationManager.OverrideMemberConverter(Type, string, IJsonConverter)"/> method.
	/// </summary>
	/// <preliminary />
	public static class Converters
	{
		/// <summary>
		/// Gets an <see cref="IJsonConverter"/> to convert <see cref="System.Net.IPAddress"/> instances.
		/// </summary>
		public static IJsonConverter IPAddress { get; private set; }
		/// <summary>
		/// Gets an <see cref="IJsonConverter"/> to convert <see cref="System.Text.RegularExpressions.Regex"/> instances.
		/// </summary>
		public static IJsonConverter Regex { get; private set; }
		/// <summary>
		/// Gets an <see cref="IJsonConverter"/> to convert <see cref="bool"/> instances to numeric 1 or 0 rather than the default "true", "false" values.
		/// </summary>
		public static IJsonConverter ZeroOneBoolean { get; private set; }
		/// <summary>
		/// Gets an <see cref="IJsonConverter"/> to convert <see cref="bool"/> instances to literal "1" or "0" rather than the default "true", "false" values.
		/// </summary>
		public static IJsonConverter TextualZeroOneBoolean { get; private set; }
		/// <summary>
		/// Gets an <see cref="IJsonConverter"/> to convert <see cref="System.Uri"/> instances.
		/// </summary>
		public static IJsonConverter Uri { get; private set; }
		/// <summary>
		/// Gets an <see cref="IJsonConverter"/> to convert <see cref="System.Version"/> instances.
		/// </summary>
		public static IJsonConverter Version { get; private set; }
		/// <summary>
		/// Gets an <see cref="IJsonConverter"/> to serialize <see cref="System.Xml.XmlDocument"/> or <see cref="System.Xml.XmlElement"/> instances. NOTICE: Deserialization is not supported at this moment.
		/// </summary>
		/// <remarks>
		/// <para>Elements will be serialized as a dictionary which contains the following three items:</para>
		/// <list type="table">
		/// <listheader><term>XML Node</term><description>Serialization Result</description></listheader>
		/// <item><term>Element name</term><description>A dictionary entry with a name as the element qualified name surrounded with &lt; and &gt; and the value is its namespace URL.</description></item>
		/// <item><term>Attributes</term><description>A dictionary entry with a name as the attribute name prefixed with an "@" character, and the value of the attribute.</description></item>
		/// <item><term>Nodes</term><description>An array contains the child nodes of the element. The array can contain the following node types and child elements.</description></item>
		/// <item><term>Text, CDATA, Entity reference</term><description>A text in the nodes array.</description></item>
		/// <item><term>Processing Instruction, XML declaration</term><description>A dictionary containing one name-value pair in the nodes array.</description></item>
		/// <item><term>Comment</term><description>A dictionary in the nodes array with a name "!" and a value as the content of the comment.</description></item>
		/// </list>
		/// </remarks>
		public static IJsonConverter XmlNode { get; private set; }

		static Converters () {
			IPAddress = new IPAddressConverter ();
			Regex = new RegexConverter ();
			ZeroOneBoolean = new ZeroOneBooleanConverter ();
			TextualZeroOneBoolean = new ZeroOneBooleanConverter (true);
			Uri = new UriConverter ();
			Version = new VersionConverter ();
			XmlNode = new XmlNodeConverter ();
		}
	}
}
