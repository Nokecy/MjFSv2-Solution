using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjFSv2Lib.Manager {
	class SynchronizationManagerException : Exception{
		public SynchronizationManagerException(string msg) : base(msg) { }
		public SynchronizationManagerException() : base() { }
	}
}
