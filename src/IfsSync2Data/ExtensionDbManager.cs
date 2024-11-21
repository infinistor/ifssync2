/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version 
* 3 of the License.  See LICENSE for details
*
* 본 프로그램 및 관련 소스코드, 문서 등 모든 자료는 있는 그대로 제공이 됩니다.
* KSAN 프로젝트의 개발자 및 개발사는 이 프로그램을 사용한 결과에 따른 어떠한 책임도 지지 않습니다.
* KSAN 개발팀은 사전 공지, 허락, 동의 없이 KSAN 개발에 관련된 모든 결과물에 대한 LICENSE 방식을 변경 할 권리가 있습니다.
*/
using log4net;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace IfsSync2Data
{
	class ExtensionDbManager
	{
		#region Define
		const string STR_EXT_TABLE_NAME = "ExtensionList";
		const string STR_EXT_ID = "Id";
		const string STR_EXT_EXTENSION = "Extension";
		const string STR_EXT_GROUP = "Group";
		#endregion

		#region Attributes
		readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		readonly string _filePath = MainData.GetDBFilePath(MainData.EXTENSION_NAME);
		readonly Mutex _sqliteMutex;
		#endregion

		public ExtensionDbManager()
		{
			try
			{
				_sqliteMutex = new Mutex(false, MainData.MUTEX_NAME_JOB_SQL, out bool CreatedNew);

				if (!CreatedNew) _log.Debug($"Mutex({MainData.MUTEX_NAME_JOB_SQL})");
				else _log.Debug($"Mutex({MainData.MUTEX_NAME_JOB_SQL}) create");
			}
			catch (Exception e)
			{
				_log.Error($"Mutex({MainData.MUTEX_NAME_JOB_SQL}) fail", e);
			}
		}

		bool CreateDBFile()
		{
			try
			{
				MainData.CreateFile(_filePath);

				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();
				using var cmd = new SQLiteCommand(conn)
				{
					//GlobalScheduleList
					CommandText = "\n" +
					$"Create Table IF NOT EXISTS '{STR_EXT_TABLE_NAME}'(" +
								 $"'{STR_EXT_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{STR_EXT_EXTENSION}' TEXT NOT NULL, " +
								 $"'{STR_EXT_GROUP}' TEXT NULL);"
				};

				cmd.ExecuteNonQuery();

				_log.Debug($"Success : {cmd.CommandText}");
				return true;
			}
			catch (SQLiteException e) { _log.Error(e); return false; }
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public List<string> GetExtensionList()
		{
			var items = new List<string>();
			try
			{
				if (!File.Exists(_filePath)) DefaultExtensionList();

				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{STR_EXT_TABLE_NAME}'" };
				using var rdr = cmd.ExecuteReader();

				while (rdr.Read())
				{
					items.Add(rdr[STR_EXT_EXTENSION].ToString());
				}
				_log.Debug($"{STR_EXT_EXTENSION} : {items.Count}");
			}
			catch (Exception e) { _log.Error(e); }
			finally { _sqliteMutex.ReleaseMutex(); }
			return items;
		}

		public bool Insert(string extension)
		{
			if (!File.Exists(_filePath) && !CreateDBFile()) return false;

			try
			{
				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"INSERT INTO '{STR_EXT_TABLE_NAME}' ({STR_EXT_EXTENSION}) VALUES ('{extension}');" };

				int result = cmd.ExecuteNonQuery();
				if (result > 0)
				{
					_log.Debug($"Success : {cmd.CommandText}");
					return true;
				}
				else
				{
					_log.Error($"Failed : {cmd.CommandText}");
					return false;
				}
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}
		public bool Insert(List<string> extensions)

		{
			if (!File.Exists(_filePath) && !CreateDBFile()) return false;

			try
			{
				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				var query = new StringBuilder();

				foreach (string Ext in extensions)
				{
					query.Append($"INSERT INTO '{STR_EXT_TABLE_NAME}' ({STR_EXT_EXTENSION}) VALUES ('{Ext}');\n");
				}

				using var cmd = new SQLiteCommand(conn) { CommandText = query.ToString() };

				int result = cmd.ExecuteNonQuery();
				if (result > 0)
				{
					_log.Debug($"Success({result}) : {cmd.CommandText}");
					return true;
				}
				else
				{
					_log.Error($"Failed({result}) : {cmd.CommandText}");
					return false;
				}
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public bool Delete(string extension)
		{
			if (!File.Exists(_filePath)) return false;

			try
			{
				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"DELETE FROM '{STR_EXT_TABLE_NAME}' WHERE {STR_EXT_EXTENSION} = '{extension}'" };

				int result = cmd.ExecuteNonQuery();
				if (result > 0)
				{
					_log.Debug($"Success({result}) : {cmd.CommandText}");
					return true;
				}
				else
				{
					_log.Error($"Failed({result}) : {cmd.CommandText}");
					return false;
				}
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public bool Check(string extension)
		{
			if (!File.Exists(_filePath) && !CreateDBFile()) return false;

			try
			{
				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT count(*) FROM '{STR_EXT_TABLE_NAME}' WHERE {STR_EXT_EXTENSION} = '{extension}';" };

				int result = Convert.ToInt32(cmd.ExecuteScalar());
				if (result > 0)
				{
					_log.Debug($"Success({result}) : {cmd.CommandText}");
					return true;
				}
				else
				{
					_log.Error($"Failed({result}) : {cmd.CommandText}");
					return false;
				}
			}
			catch (SQLiteException e) { _log.Error(e); return false; }
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		void DefaultExtensionList()
		{
			Insert(new List<string>(MainData.DEFAULT_EXTENSION_LIST.Replace(" ", "").Split(',')));
		}
	}
}
