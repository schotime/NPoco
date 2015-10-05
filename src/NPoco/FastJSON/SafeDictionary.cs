using System.Collections.Generic;

namespace NPoco.FastJSON
{
	/// <summary>
	/// A thread-safe <see cref="IDictionary{TKey, TValue}"/>.
	/// </summary>
	/// <typeparam name="TKey">The type of the dictionary key.</typeparam>
	/// <typeparam name="TValue">The type of the dictionary value.</typeparam>
	public sealed class SafeDictionary<TKey, TValue>
	{
		readonly object _Padlock = new object ();
		readonly Dictionary<TKey, TValue> _Dictionary;

		/// <summary>
		/// Initializes a new instance of the <see cref="SafeDictionary{TKey, TValue}"/> class that is empty, has the specified initial capacity, and uses the default equality comparer for the key type.
		/// </summary>
		/// <param name="capacity"></param>
		public SafeDictionary (int capacity) {
			_Dictionary = new Dictionary<TKey, TValue> (capacity);
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="SafeDictionary{TKey, TValue}"/> class that is empty, has the default initial capacity, and uses the default equality comparer for the key type.
		/// </summary>
		public SafeDictionary () {
			_Dictionary = new Dictionary<TKey, TValue> ();
		}
		internal SafeDictionary (Dictionary<TKey, TValue> dict) {
			_Dictionary = dict;
		}

		/// <summary>
		/// Clears all items.
		/// </summary>
		public void Clear () {
			lock (_Padlock)
				_Dictionary.Clear ();
		}
		/// <summary>
		/// Gets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized. </param>
		/// <returns>Returns true if the dictionary contains an element with the specified key; otherwise, false.</returns>
		public bool TryGetValue (TKey key, out TValue value) {
			lock (_Padlock)
				return _Dictionary.TryGetValue (key, out value);
		}

		/// <summary>
		/// Gets the number of key/value pairs contained in the dictionary.
		/// </summary>
		public int Count { get { lock (_Padlock) return _Dictionary.Count; } }

		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to get or set.</param>
		/// <returns>The value associated with the specified key. If the specified key is not found, a get operation throws a <see cref="KeyNotFoundException"/>, and a set operation creates a new element with the specified key.</returns>
		public TValue this[TKey key] {
			get {
				lock (_Padlock)
					return _Dictionary[key];
			}
			set {
				lock (_Padlock)
					_Dictionary[key] = value;
			}
		}

		/// <summary>
		/// Adds the specified key and value to the dictionary.
		/// </summary>
		/// <param name="key">The key of the element to add.</param>
		/// <param name="value">The value of the element to add. The value can be null for reference types.</param>
		public void Add (TKey key, TValue value) {
			lock (_Padlock) {
				if (_Dictionary.ContainsKey (key) == false)
					_Dictionary.Add (key, value);
			}
		}

		/// <summary>
		/// Removes specific key from the dictionary.
		/// </summary>
		/// <param name="key">The key of the element to remove.</param>
		public void Remove (TKey key) {
			lock (_Padlock) {
				_Dictionary.Remove (key);
			}
		}
	}
}
