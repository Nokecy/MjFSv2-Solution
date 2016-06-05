using MjFSv2Lib.Database;
using MjFSv2Lib.Manager;
using MjFSv2Lib.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjFSv2Watcher {
	class Program {
		private VolumeMountManager vMan = VolumeMountManager.GetInstance();
		private Dictionary<DriveInfo, DatabaseOperations> driveBagMap;

		static void Main(string[] args) {
			Console.WriteLine("MjFS v2 Watcher - An SQL based file system");
			Console.WriteLine();


			if (!Helper.IsUserAdministrator()) {
				Console.WriteLine("Please run this program with administrator privilleges.");
				Console.Read();
			} else {
				Program p = new Program();
				p.ProcessInput(Console.ReadLine());
			}
		}

		public void ProcessInput(string input) {
			Console.WriteLine("Processsing input '" + input + "'");

			string[] cInput = input.ToLower().Split(new char[] { ' ' });

			string command = cInput[0];
			string[] args = new string[command.Length -1];
				
			Array.Copy(cInput, 1, args, 0, cInput.Length - 1);

			if(command == "help") {
				PrintUsage();
			} else if (command == "ls") {
				PrintList();
			} else if (command == "add") {
				
			} else if (command == "stat") {

			} else if (command == "remove") {

			} else {
				Console.WriteLine("Unknown command '" + command + "'");
			}
			Console.WriteLine();
			ProcessInput(Console.ReadLine());
		}

		void PrintUsage() {
			Console.WriteLine("ls -- list all volumes configured for MjFS");
			Console.WriteLine("stat [volume index] -- show detailed status of the given volume");
			Console.WriteLine("add [paht to dir] -- configure the given drive for MjFS with the given folder as bag");
			Console.WriteLine("add -- configure any drive for MjFS");
			Console.WriteLine("remove [volume index] -- remove the MjFS configuration of the given volume");
		}

		void PrintList() {
			driveBagMap = vMan.GetBagConfigurations();
			int i = 0;
			foreach(KeyValuePair<DriveInfo, DatabaseOperations> entry in driveBagMap) {
				Console.WriteLine("[" + i + "] " + entry.Key);
				i++;
			}
		}

		void PrintStats(string[] args) {
			driveBagMap = vMan.GetBagConfigurations();

		}

	}

	
}
