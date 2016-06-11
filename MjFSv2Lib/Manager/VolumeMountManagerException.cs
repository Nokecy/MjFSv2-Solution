using System;

namespace MjFSv2Lib.Manager {
	public class VolumeMountManagerException : Exception{
		public VolumeMountManagerException(string msg) : base(msg) { }
		public VolumeMountManagerException() : base() { }
	}
}
