using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XansCharacter.Data.Players {

	/// <summary>
	/// For internal use only.
	/// </summary>
	public abstract class BasePlayerStorageKey {

		/// <summary>
		/// A name for this key.
		/// </summary>
		public string Name { get; }

		protected BasePlayerStorageKey(string name) {
			Name = name;
		}

	}

	/// <summary>
	/// A key to be used in <see cref="PlayerStorage"/> for stats. It is designed for fast lookup in dictionaries.
	/// </summary>
	/// <typeparam name="TValue">The value type that is associated with this key.</typeparam>
	public class PlayerStorageKey<TValue> : BasePlayerStorageKey {

		private static uint _lastHash = 0;

		private readonly int _hash;

		public PlayerStorageKey(string name) : base(name) {
			_hash = unchecked((int)_lastHash);
			_lastHash = unchecked(_lastHash + 48185);
		}

		public override bool Equals(object obj) {
			if (obj is PlayerStorageKey<TValue> otherKey) {
				return ReferenceEquals(otherKey, this); // TODO: Any other equality?
			}
			return false;
		}

		public override int GetHashCode() {
			return _hash;
		}

		public override string ToString() {
			return $"PlayerStorageKey[Name={Name}, Type={typeof(TValue).FullName}]";
		}

	}
}
