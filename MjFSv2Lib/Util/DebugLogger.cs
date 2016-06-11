using System;

namespace MjFSv2Lib.Util {
	class DebugLogger {
		public static readonly string LOG_PREFIX = "[MjFS] ";
		private static readonly LogPriority LOG_PRIO = LogPriority.MODERATE;
		private static readonly bool LOG_CUR_PRIO_ONLY = true;

		public static void Log(string msg) {
			Log(msg, LogPriority.MODERATE);
		}

		public static void Log(string msg, LogPriority prio) {
			if (LOG_CUR_PRIO_ONLY) {
				if (prio == LOG_PRIO) {
					Console.WriteLine(LOG_PREFIX + DateTime.Now.ToLongTimeString() + " " + msg);
				}
			} else {
				if (prio <= LOG_PRIO) {
					Console.WriteLine(LOG_PREFIX + DateTime.Now.ToLongTimeString() + " " + msg);
				}
			}		
		}
	}

	enum LogPriority {
		LOW, MODERATE, HIGH, ALL
	}
}
