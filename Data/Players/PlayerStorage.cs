using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using XansCharacter.Character.PlayerCharacter.DataStorage;

namespace XansCharacter.Data.Players {

	/// <summary>
	/// A class storing ephermal (per-life) data. This information is not saved and it is destroyed upon respawning.
	/// </summary>
	public sealed class PlayerStorage {

		private static readonly ConditionalWeakTable<Player, PlayerStorage> _cache = new ConditionalWeakTable<Player, PlayerStorage>();
		private static bool _initialized = false;

		/// <summary>
		/// The player this storage exists for. This will be <see langword="null"/> if the player has been destroyed or garbage collected.
		/// </summary>
		public Player Player => _player.TryGetTarget(out Player player) ? player : null;

		private readonly WeakReference<Player> _player;
		private readonly Dictionary<BasePlayerStorageKey, object> _data = new Dictionary<BasePlayerStorageKey, object>();

		private PlayerStorage(Player target) {
			_player = new WeakReference<Player>(target);
			Log.LogTrace($"A runtime ephemeral storage object was created for: {target} (playing as {nameof(SlugcatStats)}.{nameof(SlugcatStats.Name)}.{target.slugcatStats.name}).");
		}

		/// <summary>
		/// Sets <paramref name="value"/> to the value associated with the provided <paramref name="key"/>, returning 
		/// <see langword="true"/> if the value exists and was set, and <see langword="false"/> if it was not.
		/// </summary>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryGet<TValue>(PlayerStorageKey<TValue> key, out TValue value) {
			if (_data.TryGetValue(key, out object valueObj)) {
				value = (TValue)valueObj;
				return true;
			}
			value = default;
			return false;
		}

		/// <summary>
		/// Sets the value associated with the provided <paramref name="key"/>, overwriting any previously stored data.
		/// </summary>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Put<TValue>(PlayerStorageKey<TValue> key, TValue value) {
			_data[key] = value;
		}

		/// <summary>
		/// Removes the value associated with the provided <paramref name="key"/>. Returns <see langword="true"/> if the value was removed, <<see langword="false"/> if not.
		/// </summary>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool Remove<TValue>(PlayerStorageKey<TValue> key) {
			return _data.Remove(key);
		}

		/// <summary>
		/// Clears all data
		/// </summary>
		public void Clear() {
			_data.Clear();
		}

		/// <summary>
		/// Creates a new <see cref="PlayerStorage"/> for the provided player, designed to store ephemeral information.
		/// </summary>
		/// <param name="forPlayer"></param>
		/// <returns></returns>
		public static PlayerStorage ForPlayer(Player forPlayer) {
			if (!_initialized) throw new InvalidOperationException("The storage must be initialized in the Awake() method of the mod!");

			if (_cache.TryGetValue(forPlayer, out PlayerStorage storage)) return storage;
			storage = new PlayerStorage(forPlayer);
			_cache.Add(forPlayer, storage);
			return storage;
		}
	}
}
