using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XansCharacter.Configs;

namespace XansCharacter {
	internal static class Log {
		private static ManualLogSource _logSource;

		internal static void Initialize(ManualLogSource logSource) {
			_logSource = logSource;
		}
		private static string ToString(object o) {
			return o?.ToString() ?? "null";
		}

		/// <summary>
		/// Does nothing if <see cref="Configuration.TraceLogging"/> is <see langword="false"/>. If it is <see langword="true"/>, it will write to debug level, prefixing the
		/// message with [TRACE]: before writing it.
		/// </summary>
		/// <param name="data"></param>
		internal static void LogTrace(object data) {
			if (!Configuration.TraceLogging) return;
			StackTrace stack = new StackTrace();
			StackFrame super = stack.GetFrame(1);
			MethodBase caller = super.GetMethod();
			string result = $"[TRACE // {caller.DeclaringType.Name}::{caller.Name}]: ";
			result += ToString(data);
			_logSource.Log(LogLevel.Debug, result);
		}
		internal static void LogDebug(object data) => _logSource.LogDebug(ToString(data));
		internal static void LogError(object data) => _logSource.LogError(ToString(data));
		internal static void LogFatal(object data) => _logSource.LogFatal(ToString(data));
		internal static void LogInfo(object data) => _logSource.LogInfo(ToString(data));
		internal static void LogMessage(object data) => _logSource.LogMessage(ToString(data));
		internal static void LogWarning(object data) => _logSource.LogWarning(ToString(data));
	}
}
