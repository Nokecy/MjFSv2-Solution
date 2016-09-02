using System;

namespace MjFSv2Lib.Util {
	class MjDebug {
		public static readonly string LOG_PREFIX = "[MjFS] ";
		private static readonly LogSeverity CUR_LOG_PRIO = LogSeverity.LOW;
		private static readonly bool LOG_CUR_PRIO_ONLY = false;
		private static bool HALT_ON_CRITICAL = false;

		/// <summary
		/// Log this message with low priority
		/// </summary>
		/// <param name="msg"></param>
		public static void Log(string msg) {
			Log(msg, LogSeverity.LOW);
		}

		/// <summary>
		/// Log a message with the given priority. Note that, whatever the settings, critical priority will always go through.
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="prio"></param>
		public static void Log(string msg, LogSeverity prio, Exception ex) {
			if (LOG_CUR_PRIO_ONLY && prio != LogSeverity.CRITICAL) {
				if (prio == CUR_LOG_PRIO) {
					Console.WriteLine("[" + prio.ToString().ToUpper() + " " + DateTime.Now.ToLongTimeString() + " ] " + msg);
				}
			} else {
				if (prio >= CUR_LOG_PRIO) {
					Console.WriteLine("[" + prio.ToString().ToUpper() + " " + DateTime.Now.ToLongTimeString() + " ] " + msg);
				}
			}

			if (prio == LogSeverity.CRITICAL && HALT_ON_CRITICAL) {
				if (ex != null) {
					Console.WriteLine("[" + prio.ToString().ToUpper() + " " + DateTime.Now.ToLongTimeString() + " ] " + ex.Message);
					throw ex;
				} else {
					throw new Exception(msg);
				}
			}
		}

		public static void Log(string msg, LogSeverity prio) {
			Log(msg, prio, null);
		}

		/// <summary>
		/// Halt execution and log message with critical priority.
		/// </summary>
		/// <param name="msg"></param>
		public static void Halt(string msg, Exception ex) {
			HALT_ON_CRITICAL = true;
			Log(msg, LogSeverity.CRITICAL, ex);
		}
	}

	enum LogSeverity {
		LOW, MEDIUM, HIGH, CRITICAL 
	}
}
