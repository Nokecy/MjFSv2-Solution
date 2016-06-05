using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using MjFSv2Lib.Database;
using MjFSv2Lib.Util;

namespace MjFSv2Lib.Manager {
	/// <summary>
	/// Manages the life-time of all database connections. 
	/// </summary>
	class DatabaseManager {
		public static readonly int SQLITE_VERSION = 3;
		public static readonly List<int> SUPPORTED_DB_VER = new List<int>(){ 3 };
		public static readonly int CURRENT_DB_VER = 3;

		private static DatabaseManager instance = new DatabaseManager();
		private Dictionary<DatabaseOperations, SQLiteConnection> _connections = new Dictionary<DatabaseOperations, SQLiteConnection>();

		private DatabaseManager() {}

		public static DatabaseManager GetInstance() {
			return instance;
		}

		/// <summary>
		/// Open a connection to the given database and register it with the manager
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public DatabaseOperations OpenConnection(string filePath) {
			if (File.Exists(filePath)) {
				string connectionString = "Data Source=" + filePath + ";Version=" + SQLITE_VERSION;
				SQLiteConnection con = new SQLiteConnection(connectionString);
				con.Open();
				DatabaseOperations op = new DatabaseOperations(con);

				int loadedVer = op.GetVersion();

				if (!SUPPORTED_DB_VER.Contains(loadedVer)) {
					throw new NotSupportedException("Database version " + loadedVer + " is not supported.");
				}

				if (op == null) {
					DebugLogger.Log("WARNING: op null at creation time");
				}

				_connections.Add(op, con);
				return op;
			} else {
				throw new FileNotFoundException("The specified database file does not exist");
			}
		}

		/// <summary>
		/// Close the connection contained in the operation object and unregister it
		/// </summary>
		/// <param name="op"></param>
		public void CloseConnection(DatabaseOperations op) {
			SQLiteConnection con;

			if (_connections.TryGetValue(op, out con)) {
				con.Close();
				GC.Collect();
				GC.WaitForPendingFinalizers();
				_connections.Remove(op);
			} else {
				throw new ArgumentException("The operations object is not registered with the database manager");
			}
		}
	}
}
