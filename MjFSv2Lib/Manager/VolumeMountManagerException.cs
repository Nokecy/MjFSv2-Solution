using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjFSv2Lib.Manager {
	public class VolumeMountManagerException : Exception{
		public VolumeMountManagerException(string msg) : base(msg) { }
		public VolumeMountManagerException() : base() { }
	}
}
