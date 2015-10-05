using System.Collections.Generic;

namespace NPoco.FastJSON
{
	/// <summary>
	/// <para>An interface to intercept various aspects in JSON serialization and deserialization.</para>
	/// <para>It is recommended to inherit from <see cref="JsonInterceptor&lt;T&gt;"/> for easier implementation when possible.</para>
	/// </summary>
	/// <preliminary />
	public interface IJsonInterceptor
	{
		/// <summary>
		/// This method is called before values are written out during serialization. If the method returns false, the object will not be serialized.
		/// </summary>
		/// <param name="data">The object being serialized.</param>
		/// <returns>Whether the object should be serialized.</returns>
		bool OnSerializing (object data);

		/// <summary>
		/// This method is called before the serialization is finished. Extra values can be returned and written to the serialized result.
		/// </summary>
		/// <param name="data">The object being serialized.</param>
		/// <returns>Extra values to be serialized.</returns>
		IEnumerable<JsonItem> SerializeExtraValues (object data);

		/// <summary>
		/// This method is called after the object has been fully serialized.
		/// </summary>
		/// <param name="data">The object being serialized.</param>
		void OnSerialized (object data);

		/// <summary>
		/// This method is called before serializing a field or a property. If the method returns false, the member will not be serialized.
		/// </summary>
		/// <param name="data">The container object.</param>
		/// <param name="item">The item to be serialized.</param>
		/// <returns>Whether the member should be serialized.</returns>
		bool OnSerializing (object data, JsonItem item);

		/// <summary>
		/// This method is called between the object has been created and the values are filled during deserialization.
		/// This method provides an opportunity to initialize an object before deserialization.
		/// </summary>
		/// <param name="data">The object being deserialized.</param>
		void OnDeserializing (object data);

		/// <summary>
		/// This method is called after the object has been fully deserialized. Data validation could be done onto the serialized object.
		/// </summary>
		/// <param name="data">The object created from deserialization.</param>
		void OnDeserialized (object data);

		/// <summary>
		/// This method is called before deserializing a field or a property. If the method returns false, the member will not be deserialized.
		/// </summary>
		/// <param name="data">The container object.</param>
		/// <param name="item">The item to be deserialized.</param>
		/// <returns>Whether the member should be deserialized.</returns>
		bool OnDeserializing (object data, JsonItem item);
	}

	/// <summary>
	/// This is a default implementation of <see cref="IJsonInterceptor"/>, which restricts the type of the object being serialized or deserialized.
	/// The default implementation does nothing and returns true for all OnSerializing or OnDeserializing methods.
	/// </summary>
	/// <typeparam name="T">The type of the object being serialized or deserialized.</typeparam>
	/// <preliminary />
	public abstract class JsonInterceptor<T> : IJsonInterceptor
	{
		/// <summary>
		/// This method is called before values are written out during serialization. If the method returns false, the object will not be serialized.
		/// </summary>
		/// <param name="data">The object being serialized.</param>
		/// <returns>Whether the object should be serialized.</returns>
		public virtual bool OnSerializing (T data) { return true; }

		/// <summary>
		/// This method is called before the serialization is finished. Extra values can be returned and written to the serialized result.
		/// </summary>
		/// <param name="data">The object being serialized.</param>
		/// <returns>Extra values to be serialized.</returns>
		public virtual IEnumerable<JsonItem> SerializeExtraValues (T data) { return null; }

		/// <summary>
		/// This method is called after the object has been fully serialized.
		/// </summary>
		/// <param name="data">The object being serialized.</param>
		public virtual void OnSerialized (T data) { }

		/// <summary>
		/// This method is called between the object has been created and the values are filled during deserialization.
		/// This method provides an opportunity to initialize an object before deserialization.
		/// </summary>
		/// <param name="data">The object being deserialized.</param>
		public virtual void OnDeserializing (T data) { }

		/// <summary>
		/// This method is called after the object has been fully deserialized. Data validation could be done onto the serialized object.
		/// </summary>
		/// <param name="data">The object created from deserialization.</param>
		public virtual void OnDeserialized (T data) { }

		/// <summary>
		/// This method is called before serializing a field or a property. If the method returns false, the member will not be serialized.
		/// </summary>
		/// <param name="data">The container object.</param>
		/// <param name="item">The item being serialized.</param>
		/// <returns>Whether the member should be serialized.</returns>
		public virtual bool OnSerializing (T data, JsonItem item) {
			return true;
		}

		/// <summary>
		/// This method is called before deserializing a field or a property. If the method returns false, the member will not be deserialized.
		/// </summary>
		/// <param name="data">The container object.</param>
		/// <param name="item">The item to be deserialized.</param>
		/// <returns>Whether the member should be deserialized.</returns>
		public virtual bool OnDeserializing (T data, JsonItem item) {
			return true;
		}

		bool IJsonInterceptor.OnSerializing (object data) {
			return (data is T) && OnSerializing ((T)data);
		}

		IEnumerable<JsonItem> IJsonInterceptor.SerializeExtraValues (object data) {
			return (data is T) ? SerializeExtraValues ((T)data) : null;
		}

		void IJsonInterceptor.OnSerialized (object data) {
			if (data is T) {
				OnSerialized ((T)data);
			}
		}

		void IJsonInterceptor.OnDeserializing (object data) {
			if (data is T) {
				OnDeserializing ((T)data);
			}
		}

		void IJsonInterceptor.OnDeserialized (object data) {
			if (data is T) {
				OnDeserialized ((T)data);
			}
		}

		bool IJsonInterceptor.OnSerializing (object data, JsonItem item) {
			if (data is T) {
				return OnSerializing ((T)data, item);
			}
			return false;
		}

		bool IJsonInterceptor.OnDeserializing (object data, JsonItem item) {
			if (data is T) {
				return OnDeserializing ((T)data, item);
			}
			return false;
		}
	}

}