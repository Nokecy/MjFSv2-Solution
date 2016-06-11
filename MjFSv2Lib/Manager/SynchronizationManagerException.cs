using System;

namespace MjFSv2Lib.Manager {
	class SynchronizationManagerException : Exception{
		public SynchronizationManagerException(string msg) : base(msg) { }
		public SynchronizationManagerException() : base() { }
	}
}
