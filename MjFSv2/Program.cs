using MjFSv2Lib.Database;
using MjFSv2Lib.FileSystem;
using MjFSv2Lib.Manager;
using MjFSv2Lib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjFSv2Lib {
	class Program {
		static void Main(string[] args) {
			Console.WriteLine("MjFS v2 - A SQL based file system");
			Console.WriteLine();


			if (!Helper.IsUserAdministrator()) {
				Console.WriteLine("Please run this program with administrator privilleges.");
				Console.Read();
			} else {
				VolumeMountManager vMan = VolumeMountManager.GetInstance();
				vMan.MountAllBags();
				vMan.Mount();
			}
		}
	}
}
