using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjFSv2Lib.Database {
	public class TableRow : IDisposable{
		private Dictionary<string, string> _colValueMap = new Dictionary<string, string>();
		private string _tableName;
		private bool _disposed;

		public Dictionary<string, string> Columns {
			get {
				if (!_disposed) {
					return _colValueMap;
				} else {
					throw new InvalidOperationException();
				}
			}
		}

		public string TableName {  get {
				return _tableName;
			}
		}

		public TableRow(string tableName) {
			this._tableName = tableName;
		}

		public void AddColumn(string column, string value) {
			if (!_disposed) {
				this._colValueMap.Add(column, value);
			} else {
				throw new InvalidOperationException();
			}
		}

		public void Dispose() {
			this._disposed = true;
		}
	}
}
