using MjFSv2Lib.Manager;
using MjFSv2Lib.Util;
using System;

namespace MjFSv2Lib {
	class Program {
		static void Main(string[] args) {
			Console.WriteLine("MjFS v2 - An SQL based file system");
			Console.WriteLine();

			if (!Helper.IsUserAdministrator()) {
				Console.WriteLine("Please run this program with administrator privilleges.");
				Console.Read();
			} else {
				VolumeMountManager vMan = VolumeMountManager.GetInstance();
				vMan.MountBagVolumes();
				SynchronizationManager.GetInstance().StartSynchronization(vMan.DiscoveredBagVolumes);
				vMan.MountMainVolume();
			}
			Console.WriteLine("Application exited");

			Console.Read();
		}
	}
}
