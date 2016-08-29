using MjFSv2Lib.Domain;
using MjFSv2Lib.Manager;
using MjFSv2Lib.Model;
using MjFSv2Lib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace MjFSv2Lib.Database {
	/// <summary>
	/// Represents a database connection including all allowed operations on the database.
	/// </summary>
	public class DatabaseOperations {
		private readonly SQLiteConnection _connection;
		private readonly EntityContext _context;
		private string _bagPath;
		public String BagLocation {
			get { return _bagPath; }
		}

		public DatabaseOperations (EntityContext context) {
			this._connection = (SQLiteConnection)context.Database.Connection;
			this._context = context;


			if (_context.Database.Exists()) {
				if (_context.Database.CompatibleWithModel(false)) {
					try {
						_bagPath = this.GetBagLocation() + "\\";
						MjDebug.Log("Initialized DatabaseOperations for '" + _bagPath + "'");
					} catch (Exception ex) {
						// This happens when initializing an empty database
						//MjDebug.Halt("Error during database initialization", ex);
					}
				} else {
					MjDebug.Log("Connected database is not compatible with current model!");
				}
			}		
        }

		public bool TableExists(string tableName) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name = @tableName";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@tableName", tableName);
			SQLiteDataReader r = cmd.ExecuteReader();
			return r.Read();
		}

		// Hack
		public int InsertTableRow(TableRow row) {
			if (row != null) {
				StringBuilder sb;
				Dictionary<string, string> columns;

				using (row) {
					sb = new StringBuilder("INSERT INTO " + row.TableName + " (");
					columns = row.Columns;

					int i = 0;
					foreach (string columnName in columns.Keys) {
						sb.Append("\"" + columnName + "\"");
						if (i != columns.Keys.Count - 1) {
							sb.Append(", ");
						}
						i++;
					}
					sb.Append(") VALUES (");

					i = 0;
					foreach (string columnValue in columns.Values) {
						sb.Append("\"" + columnValue + "\"");
						if (i != columns.Values.Count - 1) {
							sb.Append(", ");
						}
						i++;
					}
					sb.Append(")");
				}
				SQLiteCommand cmd = new SQLiteCommand(_connection);
				cmd.CommandText = sb.ToString();
				cmd.Prepare();
				return cmd.ExecuteNonQuery();
			} else {
				return -1;
			}	
		}

		private string GetBagLocation() {
			return _context.Configs.First<Config>().location;
		}

		public int GetVersion() {
			var currentConfig = _context.Configs.First<Config>();
			return Convert.ToInt32(currentConfig.version);
		}

		/// <summary>
		/// Retrieve the list of meta tables that are root visible.
		/// </summary>
		/// <returns></returns>
		public List<MetaTable> GetRootTables() {
			var rootTables = from metaTable in _context.MetaTables
						   where metaTable.rootVisible == 1
						   select metaTable;

			return rootTables.ToList<MetaTable>();
		}

		/// <summary>
		/// Retrieve the list of all items in the table with this friendly name
		/// </summary>
		/// <param name="friendlyTableName"></param>
		/// <returns></returns>
		public List<ItemMeta> GetItemsByTableFriendlyName(string friendlyTableName) {
			var tables = from metaTable in _context.MetaTables
						where metaTable.friendlyName.ToLower() == friendlyTableName
						select metaTable;

			if (tables.Count<MetaTable>() == 0) {
				return new List<ItemMeta>();
			}
			MetaTable table = tables.First<MetaTable>(); // Friendlynames are unique
			return GetItemsByTableName(table.tableName);
		}

		public List<ItemMeta> GetItemsByTableName(string tableName) {
			var items = _context.Database.SqlQuery<ItemMeta>("SELECT DISTINCT * FROM ItemMeta WHERE itemId IN " + tableName);
			return items.ToList<ItemMeta>();
		}


		[Obsolete]
		public Item GetItem(string id) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT * FROM Item WHERE itemId = @id";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", id);
			SQLiteDataReader r = cmd.ExecuteReader();

			if (r.Read()) {
				return new Item(r[0].ToString(),
					r[1].ToString(), r[2].ToString(),
					Convert.ToInt64(r[3].ToString()), Convert.ToDateTime(r[5].ToString()),
					Convert.ToDateTime(r[6].ToString()), Convert.ToDateTime(r[7].ToString()),
					Helper.GetFileAttributesFromInt(Convert.ToInt32(r[4].ToString())));
			} else {
				return null;
			}
		}

		/// <summary>
		/// Return all items for the given list of tags
		/// </summary>
		/// <param name="tags"></param>
		/// <returns></returns>
		[Obsolete]
		public List<Item> GetItemsByCompositeTag(List<string> tags) {
			int c = tags.Count;
			List<Item> itemList = new List<Item>();

			// Build JOIN clauses
			StringBuilder sbJoin = new StringBuilder();
			for (int i = 0; i < c; i++) {
				sbJoin.Append("INNER JOIN ItemTag it" + i + " ON it" + i + ".tagId = @t" + i + " AND it" + i + ".itemId = i.id ");
			}

			// SQL prepared statement
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT i.* FROM Item i " + sbJoin.ToString();
			cmd.Prepare();

			// Fill all parameters with their corresponding values
			for (int i = 0; i < c; i++) {
				cmd.Parameters.AddWithValue("@t" + i, tags[i].ToLower());
			}
			SQLiteDataReader r = cmd.ExecuteReader();
			while (r.Read()) {
				Item it = new Item(r[0].ToString(),
					r[1].ToString(), r[2].ToString(),
					Convert.ToInt64(r[3].ToString()), Convert.ToDateTime(r[5].ToString()),
					Convert.ToDateTime(r[6].ToString()), Convert.ToDateTime(r[7].ToString()),
					Helper.GetFileAttributesFromInt(Convert.ToInt32(r[4].ToString())));

				itemList.Add(it);
			}
			return itemList;
		}

		/// <summary>
		/// Get the collection of all tags in the database
		/// </summary>
		/// <returns></returns>
		[Obsolete]
		public List<Tag> GetTags() {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT * FROM Tag";
			cmd.Prepare();
			SQLiteDataReader r = cmd.ExecuteReader();

			List<Tag> tagList = new List<Tag>();
			while (r.Read()) {
				tagList.Add(new Tag(r[0].ToString(), Convert.ToBoolean(r[1])));
			}
			return tagList;
		}

		[Obsolete]
		public List<string> GetInnerTags(List<string> tags) {
			int c = tags.Count;
			List<string> itemList = new List<string>();
			if (c > 0) {
				//The following piece of code is a bit dramatic in that it creates an SQL query on-demand #YOTH16

				// Build JOIN clauses
				StringBuilder sbJoin = new StringBuilder();
				for (int i = 0; i < c; i++) {
					sbJoin.Append("INNER JOIN ItemTag it" + i + " ON it" + i + ".tagId = @t" + i + " AND it" + i + ".itemId = i.id ");
				}

				// SQL prepared statement
				SQLiteCommand cmd = new SQLiteCommand(_connection);
				cmd.CommandText = "SELECT DISTINCT `tagId` FROM ItemTag WHERE `itemId` IN (SELECT i.id FROM Item i " + sbJoin.ToString() + ")";
				cmd.Prepare();
				
				// Fill all parameters with their corresponding values
				for (int i = 0; i < c; i++) {
					cmd.Parameters.AddWithValue("@t" + i, tags[i].ToLower());
				}
				SQLiteDataReader r = cmd.ExecuteReader();
				
				while (r.Read()) {
					itemList.Add(r[0].ToString());
				}
			}		
			return itemList;
		}


		/// <summary>
		/// Associate the tag to the given item
		/// </summary>
		/// <param name="item"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		[Obsolete]
		public int InsertItemTag(Item item, Tag tag) {
			return InsertItemTag(item, tag.Id);
		}

		[Obsolete]
		public int InsertItemTag(Item item, string tag) {
			if (tag != null) {
				SQLiteCommand cmd = new SQLiteCommand(_connection);
				cmd.CommandText = "INSERT INTO ItemTag(itemId, tagId) VALUES (@itemId, @tagId)";
				cmd.Prepare();
				cmd.Parameters.AddWithValue("@itemId", item.Id);
				cmd.Parameters.AddWithValue("@tagId", tag.ToLower().Trim());
				return cmd.ExecuteNonQuery();
			} else {
				return -1;
			}
		}

		/// <summary>
		/// Insert a tag into the database
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		[Obsolete]
		public int InsertTag(Tag tag) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "INSERT INTO Tag(id, rootVisible) VALUES (@id, @rootVisible)";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", tag.Id.ToLower().Trim());
			cmd.Parameters.AddWithValue("@rootVisible", Convert.ToInt32(false));
			return cmd.ExecuteNonQuery();
		}

		[Obsolete]
		public int InsertTag(string tag) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "INSERT INTO Tag(id, rootVisible) VALUES (@id, 0)";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", tag.ToLower().Trim());
			return cmd.ExecuteNonQuery();
		}

		
		public int UpdateItem(Item item) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "UPDATE `Item` SET name = @name, ext = @ext, size=@size WHERE id = @id;";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", item.Id);
			cmd.Parameters.AddWithValue("@name", item.Name);
			cmd.Parameters.AddWithValue("@ext", item.Extension);
			cmd.Parameters.AddWithValue("@size", item.Size);
			return cmd.ExecuteNonQuery();
		}


		/// <summary>
		/// Populate the database with tables by running the SQL creation script. Additionally, some configuration info will be injected in the database.
		/// </summary>
		/// <param name="bagLocation">The location of the bag relative to the drive. E.g. folder\bag\. Do not include the drive letter here.</param>
		public void CreateTables(string bagLocation) {
			_bagPath = Path.GetPathRoot(_connection.FileName) + bagLocation; // Build current bag location from drive and bag path
			_context.Database.ExecuteSqlCommand(Properties.Resources.database, new SQLiteParameter("@defaultBag", _bagPath)); // Insert bagPath into table creation script and execute			
		}

		public int TruncateTable(string tableName) {
			//TODO: don't use direct user input
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "DELETE FROM " + tableName;
			cmd.Prepare();
			return cmd.ExecuteNonQuery();
		}


	}
}
