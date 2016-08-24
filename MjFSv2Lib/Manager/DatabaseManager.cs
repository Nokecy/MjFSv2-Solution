using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;
using MjFSv2Lib.Database;
using MjFSv2Lib.Domain;

namespace MjFSv2Lib.Manager {
	/// <summary>
	/// Manages the life-time of all database connections. 
	/// </summary>
	class DatabaseManager {
		private static readonly int SQLITE_VERSION = 3;
		public static readonly int CURRENT_DB_VER = 6;
		public static readonly List<int> SUPPORTED_DB_VER = new List<int>(){ CURRENT_DB_VER };

		private static DatabaseManager instance = new DatabaseManager();
		private readonly Dictionary<DatabaseOperations, SQLiteConnection> _connections = new Dictionary<DatabaseOperations, SQLiteConnection>();

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
			/*if (!File.Exists(filePath)) {
				SQLiteConnection.CreateFile(filePath);
			}*/
			
			string connectionString = "Data Source=" + filePath + ";Version=" + SQLITE_VERSION;
			SQLiteConnection con = new SQLiteConnection(connectionString);
			con.Open();
					

			EntityContext entities = new EntityContext(con);

			DatabaseOperations op = new DatabaseOperations(entities);
 	
			_connections.Add(op, (SQLiteConnection)entities.Database.Connection);
			return op;
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
