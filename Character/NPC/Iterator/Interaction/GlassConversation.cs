using HUD;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using XansTools.Utilities;
using XansTools.Exceptions;
using XansTools.Utilities.RW;
using Music;

namespace XansCharacter.Character.NPC.Iterator.Interaction {

	/// <summary>
	/// A more capable conversation type designed for the newer <see cref="ParameterizedEvent"/> system.
	/// <para/>
	/// This operates fundamentally differently than the base <see cref="Conversation"/> type.
	/// </summary>
	public abstract class GlassConversation : Conversation {

		private int _eventIndex = 0;
		private int _lastResetEventIndex = -1;
		private readonly Dictionary<string, LabelEvent> _labels = new Dictionary<string, LabelEvent>();

		public GlassConversation(IOwnAConversation interfaceOwner, ID id, DialogBox dialogBox, Player withPlayer) : base(interfaceOwner, id, dialogBox) {
			currentSaveFile = withPlayer?.SlugCatClass;
		}

		public sealed override void Update() {
			if (paused) return;
			if (_eventIndex >= events.Count) {
				Destroy();
				return;
			}

			ResetCurrentEventIfNeeded();
			DialogueEvent evt = events[_eventIndex];
			evt.Update();
			if (evt.IsOver) {
				_eventIndex++;
			}
		}

		public void Terminate() {
			_eventIndex = events.Count;
			Destroy();
		}

		public sealed override void AddEvents() {
			AddCustomEvents();
		}

		public abstract void AddCustomEvents();

		/// <summary>
		/// Returns a label by its name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected LabelEvent GetLabelEventByName(string name) {
			if (_labels.TryGetValue(name, out LabelEvent label)) {
				return label;
			}
			return null;
		}

		/// <summary>
		/// Returns whether or not there is a label present with the provided name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected bool HasLabel(string name) => _labels.ContainsKey(name);

		/// <summary>
		/// Returns the numeric index of the label with the provided name, or -1 if no such label exists.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected int GetLabelIndex(string name) {
			LabelEvent label = GetLabelEventByName(name);
			if (label == null) return -1;
			return events.IndexOf(label);
		}


		/// <summary>
		/// Returns the numeric index of the provided label, or -1 if no such label exists.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected int GetLabelIndex(LabelEvent label) => events.IndexOf(label);

		protected internal void Jump(JumpEvent src, string label) {
			if (_labels.TryGetValue(label, out LabelEvent labelEvent)) {
				int idx = events.IndexOf(labelEvent);
				if (idx < 0) {
					dialogBox.currentColor = Color.red;
					Interrupt($"<error: {nameof(JumpEvent)} attempted to jump to label [{label}]. This label was registered but its object was destroyed?>", 5);
					return;
				}

				_eventIndex = idx;
				ResetCurrentEventIfNeeded();
			} else {
				dialogBox.currentColor = Color.red;
				Interrupt($"<error: {nameof(JumpEvent)} attempted to jump to label [{label}] but no such label was registered>", 5);
			}
		}

		/// <summary>
		/// Checks <see cref="_lastResetEventIndex"/> and resets the current event (see <see cref="_eventIndex"/>) if needed.
		/// This is a required mechanism for jumping to work, as it effectively wipes the slate of the applicable event and allows it to be recycled.
		/// </summary>
		private void ResetCurrentEventIfNeeded() {
			if (_lastResetEventIndex != _eventIndex) {
				DialogueEvent @event = events[_eventIndex];
				@event.age = 0;
				@event.isActivated = false;
				_lastResetEventIndex = _eventIndex;
			}
		}

		/// <summary>
		/// Returns a file name for the provided conversation in the provided language, for the provided slugcat.
		/// If a language isn't found, it will fall back to English. If there is no English copy, it will return null.
		/// </summary>
		/// <param name="conversationName"></param>
		/// <param name="optionallyOnlyFor"></param>
		/// <param name="translator"></param>
		/// <param name="lang"></param>
		/// <returns></returns>
		private static string GetFilePathForText(string conversationName, SlugcatStats.Name optionallyOnlyFor, InGameTranslator translator, InGameTranslator.LanguageID lang = null) {
			lang = lang ?? InGameTranslator.LanguageID.English;
			string scugIdentifier = string.Empty;
			if (optionallyOnlyFor != null) {
				scugIdentifier = $"-{optionallyOnlyFor.value}";
			}
			string fileName = AssetManager.ResolveFilePath(translator.SpecificTextFolderDirectory(lang) + Path.DirectorySeparatorChar + conversationName + scugIdentifier + ".txt");
			if (!File.Exists(fileName)) {
				if (lang != InGameTranslator.LanguageID.English) return GetFilePathForText(conversationName, optionallyOnlyFor, translator, null);
				return null;
			}
			return fileName;
		}

		/// <summary>
		/// A special variant of the loader that finds formatted files for my conversation format.
		/// </summary>
		/// <param name="conversationName"></param>
		/// <param name="optionallyOnlyFor"></param>
		protected void LoadGlassConversation(string conversationName, SlugcatStats.Name optionallyOnlyFor) {
			RainWorld rw = interfaceOwner.rainWorld;
			InGameTranslator translator = rw.inGameTranslator;
			InGameTranslator.LanguageID currentLanguage = translator.currentLanguage;
			string path = GetFilePathForText(conversationName, optionallyOnlyFor, translator, currentLanguage);
			string[] contents = File.ReadAllLines(path, Encoding.UTF8);

			events.Clear();
			Log.LogTrace($"Preparing conversation: {conversationName}...");
			List<string> currentBlock = new List<string>(4); // Usually 4 is the max.
			StringBuilder builtBlock = new StringBuilder();
			for (int lineNumber = 0; lineNumber < contents.Length; lineNumber++) {
				string line = contents[lineNumber];
				if (line.StartsWith("EVT::")) {
					Log.LogTrace("Found event.");
					if (line.Length > 5) {
						// This line is an event line.
						string data = line.Substring(5);
						if (ParameterizedEvent.TryParseParameterizedEvent(this, data, lineNumber, out ParameterizedEvent evt)) {
							Log.LogTrace($"Event parsed successfully! Event name: {evt.EventName} (type: {evt.GetType().Name})");
							events.Add(evt);
							if (evt is LabelEvent label) {
								if (HasLabel(label.Target)) {
									throw new InvalidOperationException($"More than one label was declared with the name {label.Target}! Error occurred on line {lineNumber} in conversation {this}.");
								}
								_labels[label.Target] = label;
								Log.LogTrace($"Registered jump label: {label.Target}");
							}
						} else {
							Log.LogWarning($"Event failed to parse! Event line contents (line {lineNumber}): {line}");
						}
					} else {
						//Log.LogWarning("A line declared a new event EVT:: but had nothing after that.");
						throw new InvalidOperationException($"EVT:: declared on line {lineNumber}, but it had no name!");
					}
					continue;
				} else if (line.StartsWith("#") && currentBlock.Count == 0) {
					Log.LogTrace($"Encountered comment on line {lineNumber}, skipping...");
					continue;
				} else if (line.StartsWith("\\")) {
					if (line.Length > 1) {
						Log.LogTrace($"Encountered escape on line {lineNumber}, skipping the first character for the contents of the line...");
						line = line.Substring(1);
					} else {
						continue;
					}
				}
				if (string.IsNullOrWhiteSpace(line)) {
					// This is an empty line. Blocks of text can be separated by a single break (as opposed to <LINE>)
					// The empty line indicates a new block.
					if (currentBlock.Count > 0) {
						int countMinusOne = currentBlock.Count - 1;
						for (int i = 0; i < currentBlock.Count; i++) {
							string str = currentBlock[i];
							builtBlock.Append(str);
							if (i < countMinusOne) {
								builtBlock.Append("<LINE>");
							}
						}
						events.Add(new UndrivenTextEvent(this, 0, builtBlock.ToString(), 2));
					}
					currentBlock.Clear();
					builtBlock.Clear();
				} else {
					currentBlock.Add(line);
				}
			}
			if (currentBlock.Count > 0) {
				int countMinusOne = currentBlock.Count - 1;
				for (int i = 0; i < currentBlock.Count; i++) {
					string str = currentBlock[i];
					builtBlock.Append(str);
					if (i < countMinusOne) {
						builtBlock.Append("<LINE>");
					}
				}
				events.Add(new UndrivenTextEvent(this, 0, builtBlock.ToString(), 2));
			}
		}

		public override string ToString() {
			return $"{GetType().FullName}"; // TODO: More text?
		}
		protected internal static uint JumpDelegate_GetSlugcatEquality(GlassConversation src, JumpEvent evt) {
			if (evt.validScugIDs.Contains(src.currentSaveFile.value)) {
				return 1;
			}
			return 0;
		}

		/// <summary>
		/// An expansion upon <see cref="Conversation.SpecialEvent"/> that can store a dictionary of data.
		/// <para/>
		/// The parameterized event is much more capable than its counterpart and contains more behaviors.
		/// </summary>
		public class ParameterizedEvent : SpecialEvent {

			public GlassConversation Conversation => (GlassConversation)owner;

			private static readonly char[] SPLIT_BY_COMMA_ARRAY = new char[] { '=' };

			private readonly int _lineNumber;

			/// <summary>
			/// Tries to parse the data of an event line, returning whether or not that was successful.
			/// <para/>
			/// This is the correct technique to create an event. It may return a subclass of <see cref="ParameterizedEvent"/>.
			/// </summary>
			/// <param name="linePastEvt"></param>
			/// <returns></returns>
			public static bool TryParseParameterizedEvent(Conversation owner, string linePastEvt, int lineNumber, out ParameterizedEvent evt) {
				string[] info = linePastEvt.Split(',');
				evt = null;
				if (info.Length == 0) {
					Log.LogError($"Unable to parse parameterized event [{linePastEvt}] - It has no event (you can't just declare an empty event block without at least a name i.e. EVT::NameHere)");
					evt = null;
					return false;
				}
				Dictionary<string, string> @params = new Dictionary<string, string>();
				string eventName = info[0];
				if (string.IsNullOrWhiteSpace(eventName)) {
					Log.LogError($"Unable to parse parameterized event [{linePastEvt}] - It has no event (you can't just declare an empty event block without at least a name i.e. EVT::NameHere)");
					evt = null;
					return false;
				}
				for (int i = 1; i < info.Length; i++) {
					string data = info[i];
					if (!data.Contains('=')) {
						Log.LogError($"Unable to parse parameterized event [{linePastEvt}] - key/value pair no. {i} is missing an = sign. Please input text in the form of key=value (received \"{data}\")");
						evt = null;
						return false;
					}
					string[] splitData = data.Split(SPLIT_BY_COMMA_ARRAY, 2);
					string key = splitData[0];
					string value = splitData[1];
					if (string.IsNullOrWhiteSpace(key)) {
						Log.LogError($"Unable to parse parameterized event [{linePastEvt}] - key/value pair no. {i} has a key that is empty or white space. Only values can be empty or white space (received \"{data}\")");
						evt = null;
						return false;
					}
					@params[key] = value;
				}
				if (eventName == "Jmp") {
					evt = new JumpEvent(owner, eventName, lineNumber, @params);
				} else if (eventName == "Label") {
					evt = new LabelEvent(owner, eventName, lineNumber, @params);
				} else if (eventName == "TerminateConversation") {
					evt = new TerminateEvent(owner, eventName, lineNumber, @params);
				} else if (eventName == "PlayMusic") {
					ProcessManager mgr = WorldTools.Game.manager;
					MusicPlayer plr = mgr.musicPlayer;
					if (@params.TryGetValue("songName", out string songName)) {
						if (plr.song != null) {
							plr.song.FadeOut(100f);
						}
						plr.nextSong = new Song(plr, songName, MusicPlayer.MusicContext.StoryMode);
						plr.nextSong.playWhenReady = false;
					} else {
						Log.LogWarning("PlayMusic event was missing its songName parameter.");
					}
					if (@params.TryGetValue("baseVolumeOverride", out string volumeStr) && float.TryParse(volumeStr, out float volume)) {
						plr.nextSong.baseVolume = volume;
					}
				} else if (eventName == "StopMusic") {
					ProcessManager mgr = WorldTools.Game.manager;
					MusicPlayer plr = mgr.musicPlayer;
					if (plr.song != null) {
						plr.song.FadeOut(100f);
					}
					plr.nextSong = null;
				} else if (eventName == "SetColor") {
					bool success = @params.TryGetValue("r", out string rs);
					success &= @params.TryGetValue("g", out string gs);
					success &= @params.TryGetValue("b", out string bs);
					success &= float.TryParse(rs, out float r);
					success &= float.TryParse(gs, out float g);
					success &= float.TryParse(bs, out float b);
					if (success) {
						owner.dialogBox.currentColor = new Color(r, g, b);
					}
				}
				if (evt == null) {
					evt = new ParameterizedEvent(owner, eventName, lineNumber, @params);
				}

				// TO FUTURE XAN: EVT::SetField/property, EVT::Call
				return true;
			}

			public string EventName => eventName;

			private readonly IReadOnlyDictionary<string, string> _parameters;

			public override bool IsOver => isActivated;

			protected ParameterizedEvent(Conversation owner, string eventName, int lineNumber, Dictionary<string, string> parameters) : base(owner, 0, eventName) {
				_parameters = parameters;
				_lineNumber = lineNumber;
				bool hasInitialWait = false;
				if (TryGetParameterAs("initialWait", out int wait)) {
					initialWait = wait;
					hasInitialWait = true;
				}
				if (TryGetParameterAs("delay", out float waitSec)) {
					if (hasInitialWait) {
						throw new InvalidOperationException($"Attempt to declare both initialWait and delay parameters on an event ({eventName}, at line {lineNumber})");
					}
					initialWait = Mathf.RoundToInt(waitSec * Mathematical.RW_TICKS_PER_SECOND);
				}
				age = 0;
			}

			public override void Activate() {
				base.Activate();
				if (owner.interfaceOwner is IParameterizedEventReceiver paramRecv) {
					paramRecv.EventFired(this);
				}
			}

			/// <summary>
			/// Tries to return a parameter in the provided type, returning whether or not it was successful.
			/// If the value was unable to be converted, this will simply behave as if it does not exist.
			/// <para/>
			/// Raises <see cref="NotSupportedException"/> if the type is not a primitive numeric type or string.
			/// </summary>
			/// <remarks>
			/// For optimization, this requires that an unmanaged type is used (this also limits the value appropriately).
			/// For strings, use the special overload <see cref="TryGetParameterAs(string, out string)"/>.
			/// </remarks>
			/// <typeparam name="T"></typeparam>
			/// <param name="name"></param>
			/// <param name="value"></param>
			/// <returns></returns>
			/// <exception cref="NotSupportedException"></exception>
			public bool TryGetParameterAs<T>(string name, out T value) where T : unmanaged {
				unsafe {
					Type type = typeof(T);
					if (!_parameters.TryGetValue(name, out string rawValue)) {
						value = default;
						return false;
					}

					if (type == typeof(bool)) {
						if (bool.TryParse(rawValue, out bool result)) {
							value = *(T*)&result;
							return true;
						}
					} else if (type == typeof(sbyte)) {
						if (sbyte.TryParse(rawValue, out sbyte result)) {
							value = *(T*)&result;
							return true;
						}
					} else if (type == typeof(byte)) {
						if (byte.TryParse(rawValue, out byte result)) {
							value = *(T*)&result;
							return true;
						}
					} else if (type == typeof(short)) {
						if (short.TryParse(rawValue, out short result)) {
							value = *(T*)&result;
							return true;
						}
					} else if (type == typeof(ushort)) {
						if (ushort.TryParse(rawValue, out ushort result)) {
							value = *(T*)&result;
							return true;
						}
					} else if (type == typeof(int)) {
						if (int.TryParse(rawValue, out int result)) {
							value = *(T*)&result;
							return true;
						}
					} else if (type == typeof(uint)) {
						if (uint.TryParse(rawValue, out uint result)) {
							value = *(T*)&result;
							return true;
						}
					} else if (type == typeof(long)) {
						if (long.TryParse(rawValue, out long result)) {
							value = *(T*)&result;
							return true;
						}
					} else if (type == typeof(ulong)) {
						if (ulong.TryParse(rawValue, out ulong result)) {
							value = *(T*)&result;
							return true;
						}
					} else if (type == typeof(float)) {
						if (float.TryParse(rawValue, out float result)) {
							value = *(T*)&result;
							return true;
						}
					} else if (type == typeof(double)) {
						if (double.TryParse(rawValue, out double result)) {
							value = *(T*)&result;
							return true;
						}
					} else {
						throw new NotSupportedException($"The desired type ({type.FullName}) is not able to be parameterized. Only primitive value types are allowed, as well as strings. Error occurred on line {_lineNumber} in conversation {owner}.");
					}

					value = default;
					return false;
				}
			}

			/// <inheritdoc cref="TryGetParameterAs{T}(string, out T)"/>
			public bool TryGetParameterAs(string name, out string value) => _parameters.TryGetValue(name, out value);
		}

		/// <summary>
		/// The undriven text event explicitly displays text. It does not care about conversation data, it does not mutate anything about the conversation
		/// except for the current message.
		/// </summary>
		public class UndrivenTextEvent : TextEvent {

			public UndrivenTextEvent(Conversation owner, int initialWait, string text, int textLinger) : base(owner, initialWait, text, textLinger) { }

			public override void Activate() {
				isActivated = true;
				owner.dialogBox.NewMessage((owner.interfaceOwner != null) ? owner.interfaceOwner.ReplaceParts(text) : text, textLinger);
			}
		}

		/// <summary>
		/// This event marks a label in a conversation. Much like a programming label, it can be jumped to.
		/// </summary>
		public class LabelEvent : ParameterizedEvent {

			public string Target { get; }

			internal LabelEvent(Conversation owner, string eventName, int lineNumber, Dictionary<string, string> @params) : base(owner, eventName, lineNumber, @params) {
				if (TryGetParameterAs("name", out string target)) {
					Target = target;
				} else {
					throw new InvalidOperationException($"Attempt to create a Label event without its required name property. Error occurred on line {lineNumber} in conversation {owner}.");
				}
			}


		}

		/// <summary>
		/// This event tells the conversation to jump to a label.
		/// </summary>
		public class JumpEvent : ParameterizedEvent {

			/// <summary>
			/// A regex key used to match the range key for numeric jump instructions [x-y]
			/// </summary>
			//language=regex
			public const string RANGE_REGEX_TERM = @"\[(\d+)\-(\d+|\$)\]";

			/// <summary>
			/// The prefix that must be on all jump delegate method names.
			/// </summary>
			public const string JUMP_DELEGATE_METHOD_NAME_PREFIX = "JumpDelegate_";

			/// <summary>
			/// This regex can be used to match a range for numeric jump instructions.
			/// </summary>
			public static readonly Regex RANGE_REGEX = new Regex(RANGE_REGEX_TERM);

			private readonly bool _isConst;
			private readonly string _constLabel;
			private readonly JumpDelegate _delegate;
			public readonly string[] validScugIDs;

			/// <summary>
			/// A list of all functions to check where to jump. These must be parsed in order, and the first non-null return value must be used.
			/// </summary>
			private readonly List<Func<uint, string>> _jumpGetters = new List<Func<uint, string>>();

			public JumpEvent(Conversation owner, string eventName, int lineNumber, Dictionary<string, string> parameters) : base(owner, eventName, lineNumber, parameters) {
				Log.LogTrace("Constructing jump event.");
				if (TryGetParameterAs("label", out string label)) {
					Log.LogTrace("label= parameter was detected. This is a constant (unconditional) jump. Validating that no garbage data is here...");
					//throw new InvalidOperationException($"Missing required label parameter on jump instruction.")
					// This is an unconditional jump. Verify valid setup.
					Regex rangeMatch = new Regex(@"\[(\d+)\-(\d+|\$)\]");
					if (parameters.Keys.Count(key => key == "pass" || key == "fail" || uint.TryParse(key, out _) || RANGE_REGEX.IsMatch(key)) > 0) {
						throw new InvalidOperationException($"Jump event declares its label property but also declares other properties that affect the outcome of the jump. Label MUST be used without any other jump instructions (pass/fail/(numbers)/[range])! Error occurred on line {lineNumber} in conversation {owner}.");
					}

					_isConst = true;
					_constLabel = label;
					Log.LogTrace("All is well, conditional jump constructed successfully.");
					return;
				}

				bool hadSlugcatsPresetDelegate = false;
				bool hadExplicitDelegate = false;
				if (TryGetParameterAs("slugcats", out string scugs)) {
					Log.LogTrace("slugcats= branch shortcut detected!");
					string[] names = scugs.Split('|');
					hadSlugcatsPresetDelegate = true;
					validScugIDs = names;
				}
				if (parameters.TryGetValue("delegate", out string delegateName)) {
					Log.LogTrace("delegate= branch detected!");
					if (hadSlugcatsPresetDelegate) {
						throw new InvalidOperationException($"Attempt to declare both slugcats and delegate parameters on a conditional jump event. Error occurred on line {lineNumber} in conversation {owner}.");
					}
					hadExplicitDelegate = true;
					validScugIDs = null;
				} else {
					if (!hadSlugcatsPresetDelegate) {
						throw new NotSupportedException($"There is a type of jump that is not supported. A jump's type is denoted with the existence of a slugcats property (i.e. EVT::CndJmp,slugcats=Survivor|Monk) or the existence of a delegate property (i.e. EVT::CndJmp|delegate=SomeCSharpMethodNameOfConversationClass). Error occurred on line {lineNumber} in conversation {owner}.");
					}
				}

				if (hadSlugcatsPresetDelegate) {
					_delegate = JumpDelegate_GetSlugcatEquality;
					Log.LogTrace("Set delegate to the preset slugcat equality checker.");
				} else if (hadExplicitDelegate) {
					_delegate = GetJumpDelegate(Conversation, delegateName);
					Log.LogTrace("Set delegate to a specific method in the conversation class.");
				}

				bool hasPass = TryGetParameterAs("pass", out string passLabel);
				bool hasFail = TryGetParameterAs("fail", out string failLabel);

				HashSet<uint> existingSingleKeys = new HashSet<uint>();
				HashSet<ulong> existingRanges = new HashSet<ulong>();
				Log.LogTrace($"Parsing jump locations! Has pass block? {hasPass} // Has fail block? {hasFail}");
				foreach (KeyValuePair<string, string> pair in parameters) {
					if (uint.TryParse(pair.Key, out uint equalityValue)) {
						Log.LogTrace("A singular numeric key n= was detected. Processing...");
						if (equalityValue == 0 && hasFail) {
							throw new InvalidOperationException($"Jump attempted to declare a fail condition as well as an explicitly declared 0= condition. These two conflict with eachother. Remove one or the other. Error occurred on line {lineNumber} in conversation {owner}.");
						}
						if (!existingSingleKeys.Add(equalityValue)) {
							throw new InvalidOperationException($"Jump attempted to declare a numeric equality condition, but this value ({equalityValue}) was already declared by this same jump statement! Error occurred on line {lineNumber} in conversation {owner}.");
						}
						_jumpGetters.Add(v => {
							if (v == equalityValue) {
								return pair.Value;
							}
							return null;
						});
						Log.LogTrace("Successfully implemented single numeric key.");
						continue;
					} else {
						Log.LogTrace("Checking for range operator...");
						Match match = RANGE_REGEX.Match(pair.Key);
						if (match.Success) {
							// Self-reminder: These are 1 indexed, 0 is the entire match.
							uint min = uint.Parse(match.Groups[1].Value);
							uint max = match.Groups[2].Value == "$" ? uint.MaxValue : uint.Parse(match.Groups[2].Value);
							Log.LogTrace($"Range was found! Range: [{min}, {max}]");
							if (min > max) {
								throw new InvalidOperationException($"Jump attempted to declare a value range where the minimum was greater than the maximum. Error occurred on line {lineNumber} in conversation {owner}.");
							}
							if (min == 0 && hasFail) {
								throw new InvalidOperationException($"Jump attempted to declare a fail condition as well as a range condition beginning with [0-. These two conflict with eachother. Remove one or the other. Error occurred on line {lineNumber} in conversation {owner}.");
							}
							ulong encodedRange = (ulong)(min << 32) | max;
							if (!existingRanges.Add(encodedRange)) {
								throw new InvalidOperationException($"Jump attempted to declare a value range [{min}-{max}], but this exact range has been declared already! Error occurred on line {lineNumber} in conversation {owner}.");
							}

							if (min == max) {
								Log.LogWarning($"Zero-width range found! If a value needs to be matched exactly for a jump, do not use a range and instead place the value verbatim (instead of [{min}-{max}]={pair.Value}, do {min}={pair.Value}). Issue is present on line {lineNumber} in conversation {owner}.");
								_jumpGetters.Add(v => {
									if (v == min) {
										return pair.Value;
									}
									return null;
								});
							} else {
								if (min == 1 && max == uint.MaxValue && hasPass) {
									throw new InvalidOperationException($"Jump attempted to declare a pass condition, but has a range spanning [1-{uint.MaxValue}]. These two conflict with eachother. Remove one or the other. Error occurred on line {lineNumber} in conversation {owner}.");
								}
								_jumpGetters.Add(v => {
									if (v >= min && v <= max) {
										return pair.Value;
									}
									return null;
								});
							}
							Log.LogTrace($"Range operator added successfully.");
							continue;
						}
					}

					// Unsupported parameter if it's here. For now do nothing but warn?
					if (pair.Key != "pass" && pair.Key != "fail") {
						Log.LogWarning($"Unrecognized jump parameter {pair.Key} encountered.");
					}
				}
				if (hasFail) {
					_jumpGetters.Add(v => {
						if (v == 0) {
							return failLabel;
						}
						return null;
					});
				}
				if (hasPass) {
					_jumpGetters.Add(v => {
						if (v != 0) {
							return passLabel;
						}
						return null;
					});
				}
				Log.LogTrace($"Pass/Fail operators added, if present.");
			}

			public override void Activate() {
				base.Activate();
				if (_isConst) {
					Conversation.Jump(this, _constLabel);
				} else {

					string target = GetJumpTargetName(_delegate(Conversation, this));
					if (target != null) {
						Conversation.Jump(this, target);
					}
				}
			}

			/// <summary>
			/// Returns the name of the label to jump to for the provided delegate return value.
			/// <para/>
			/// A null return means to take no jump.
			/// </summary>
			/// <param name="retn"></param>
			/// <returns></returns>
			public string GetJumpTargetName(uint retn) {
				if (_isConst) return _constLabel;
				foreach (Func<uint, string> jumpTest in _jumpGetters) {
					string result = jumpTest(retn);
					if (result != null) return result;
				}
				return null;
			}

			private static JumpDelegate GetJumpDelegate(GlassConversation @this, string name) {
				string finalName = JUMP_DELEGATE_METHOD_NAME_PREFIX + name;
				MethodInfo del = @this.GetType().GetMethod(finalName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				if (del == null) {
					throw new MissingMethodException($"Failed to find a member of {@this.GetType().FullName} named {finalName}.");
				}

				#region Delegate Validation
				string delegateErrorPrefixMsg = $"When building conversation {@this}, one of its jumps wants to use the delegate method {finalName}.";
				if (del.IsPublic) {
					Log.LogWarning($"{delegateErrorPrefixMsg}. This method exists, but is public. In general these methods should avoid being public unless necessary.");
				}
				if (del.ReturnParameter.ParameterType != typeof(uint)) {
					throw new MethodSignatureMismatchException($"{delegateErrorPrefixMsg} This method exists, but does not return uint!");
				}
				ParameterInfo[] @params = del.GetParameters().Where(p => !p.IsRetval).ToArray();
				if (@params.Length != 2) {
					throw new MethodSignatureMismatchException($"{delegateErrorPrefixMsg} This method exists, but does not accept two parameters! The method should accept ({nameof(GlassConversation)}, {nameof(JumpEvent)}).");
				}
				if (@params[0].ParameterType != typeof(GlassConversation)) {
					throw new MethodSignatureMismatchException($"{delegateErrorPrefixMsg} This method exists, but its first parameter is incorrect! The method should accept ({nameof(GlassConversation)}, {nameof(JumpEvent)}).");
				}
				if (@params[1].ParameterType != typeof(JumpEvent)) {
					throw new MethodSignatureMismatchException($"{delegateErrorPrefixMsg} This method exists, but its first parameter is incorrect! The method should accept ({nameof(GlassConversation)}, {nameof(JumpEvent)}).");
				}
				#endregion

				if (del.IsStatic) {
					return (JumpDelegate)del.CreateDelegate(typeof(JumpDelegate));
				}
				return (JumpDelegate)del.CreateDelegate(typeof(JumpDelegate), @this);
			}

			private delegate uint JumpDelegate(GlassConversation convo, JumpEvent evt);
		}

		/// <summary>
		/// This event terminates the conversation.
		/// </summary>
		public class TerminateEvent : ParameterizedEvent {
			public TerminateEvent(Conversation owner, string eventName, int lineNumber, Dictionary<string, string> parameters) : base(owner, eventName, lineNumber, parameters) { }

			public override void Activate() {
				base.Activate();
				Conversation.Terminate();
			}
		}

		public interface IParameterizedEventReceiver {

			void EventFired(ParameterizedEvent evt);

		}
	}
}
