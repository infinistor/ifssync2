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
using System.Threading;

namespace IfsSync2Data
{
    class ExtensionSqlManager
    {
        private const string CLASS_NAME = "ExtensionSqlManager";

        /************************ Attribute ****************************/
        private const string STR_EXT_TABLE_NAME = "ExtensionList";
        private const string STR_EXT_ID = "ID";
        private const string STR_EXT_EXTENSION = "Extension";
        private const string STR_EXT_GROUP = "Group";
        /*************************** ETC *******************************/
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Mutex SqliteMutex;
        private readonly string FilePath;

        public ExtensionSqlManager(string RootPath)
        {
            FilePath = MainData.CreateDBFileName(RootPath, MainData.EXTENSION_NAME);
            const string FUNCTION_NAME = "Init";
            try
            {
                SqliteMutex = new Mutex(false, MainData.MUTEX_NAME_JOB_SQL, out bool CreatedNew);

                if (!CreatedNew) log.DebugFormat("[{0}:{1}] Mutex({3})", CLASS_NAME, FUNCTION_NAME, "Mutex", MainData.MUTEX_NAME_JOB_SQL);
                else log.DebugFormat("[{0}:{1}] Mutex({3}) create", CLASS_NAME, FUNCTION_NAME, "Mutex", MainData.MUTEX_NAME_JOB_SQL);
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] Mutex({3}) fail : ", CLASS_NAME, FUNCTION_NAME, "Exception", MainData.MUTEX_NAME_JOB_SQL, e.Message);
            }
        }

        private bool CreateDBFile()
        {
            const string FUNCTION_NAME = "CreateDBFile";
            try
            {
                MainData.CreateDirectory(FilePath); 
                SQLiteConnection.CreateFile(FilePath);

                SqliteMutex.WaitOne();
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText =
                    //GlobalScheduleList
                    string.Format("\n" +
                    "Create Table '{0}'(" + //STR_EXT_TABLE_NAME
                                 "'{1}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +//STR_EXT_ID
                                 "'{2}' TEXT NOT NULL, " + //STR_EXT_EXTENSION
                                 "'{3}' TEXT NULL);"     , //STR_EXT_GROUP
                                 STR_EXT_TABLE_NAME,
                                 STR_EXT_ID,
                                 STR_EXT_EXTENSION,
                                 STR_EXT_GROUP)
                };

                cmd.ExecuteNonQuery();
                conn.Close();

                log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                return true;
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }

        public List<string> GetExtensionList()
        {
            const string FUNCTION_NAME = "GetExtensionList";
            if (!File.Exists(FilePath)) DefaultExtensionList();
            try
            {
                List<string> items = new List<string>();

                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    string msg = string.Format("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");

                    log.ErrorFormat(msg);
                    throw new Exception(msg);
                }
                conn.Open();

                SQLiteCommand Cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format("SELECT * FROM '{0}'", STR_EXT_TABLE_NAME)
                };
                SQLiteDataReader Rdr = Cmd.ExecuteReader();

                while (Rdr.Read())
                {
                    string Data = Rdr[STR_EXT_EXTENSION].ToString();
                    items.Add(Data);
                }
                Rdr.Close();

                conn.Close();
                log.DebugFormat("[{0}:{1}] {2} : {3}", CLASS_NAME, FUNCTION_NAME, STR_EXT_EXTENSION, items.Count);
                return items;
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                throw e;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                throw e;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        
        public bool Insert(string Extension)
        {
            const string FUNCTION_NAME = "Insert";
            if (!File.Exists(FilePath)) if (!CreateDBFile()) return false;

            try
            {
                SqliteMutex.WaitOne();
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }
                conn.Open();

                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format("INSERT INTO '{0}' ({1}) VALUES ('{2}');", STR_EXT_TABLE_NAME, STR_EXT_EXTENSION, Extension)
                };

                int result = cmd.ExecuteNonQuery();
                conn.Close();
                if (result > 0)
                {
                    log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                    return true;
                }
                else
                {
                    log.ErrorFormat("[{0}:{1}:{2}] Fail : ", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText);
                    return false;
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool Insert(List<string> ExtensionList)

        {
            const string FUNCTION_NAME = "Insert";
            if (!File.Exists(FilePath)) if (!CreateDBFile()) return false;

            try
            {
                SqliteMutex.WaitOne();
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }
                conn.Open();


                string QueryText = string.Empty;

                foreach(string Ext in ExtensionList)
                {
                    QueryText += string.Format("INSERT INTO '{0}' ({1}) VALUES ('{2}');\n", STR_EXT_TABLE_NAME, STR_EXT_EXTENSION, Ext);
                }

                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = QueryText
                };

                int result = cmd.ExecuteNonQuery();
                conn.Close();
                if (result > 0)
                {
                    log.DebugFormat("[{0}:{1}] Success({3}) : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText, result);
                    return true;
                }
                else
                {
                    log.ErrorFormat("[{0}:{1}:{2}] Fail : ", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText);
                    return false;
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool Delete(string Extension)
        {
            const string FUNCTION_NAME = "Delete";
            if (!File.Exists(FilePath)) return false;

            try
            {
                SqliteMutex.WaitOne();

                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }

                conn.Open();

                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format( "DELETE FROM '{0}' WHERE {1} = '{2}'", STR_EXT_TABLE_NAME, STR_EXT_EXTENSION, Extension)
                };

                int result = cmd.ExecuteNonQuery();
                conn.Close();

                if (result > 0)
                {
                    log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, Extension);
                    return true;
                }
                else
                {
                    log.ErrorFormat("[{0}:{1}:{2}] Fail : ", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", Extension);
                    return false;
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }

        public bool Check(string Extension)
        {
            const string FUNCTION_NAME = "Check";
            if (!File.Exists(FilePath)) if (!CreateDBFile()) return false;

            try
            {
                SqliteMutex.WaitOne();
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }
                conn.Open();

                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format("SELECT count(*) FROM '{0}' WHERE {1} = '{2}';",
                                                STR_EXT_TABLE_NAME, STR_EXT_EXTENSION, Extension)
                };

                int result = Convert.ToInt32(cmd.ExecuteScalar());
                conn.Close();
                if (result > 0)
                {
                    log.DebugFormat("[{0}:{1}] Success({3}) : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText, result);
                    return true;
                }
                else
                {
                    log.ErrorFormat("[{0}:{1}:{2}] Fail({4}) : {3}", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText, result);
                    return false;
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }

        private bool DefaultExtensionList()
        {
            return Insert(new List<string>(MainData.DEFAULT_EXTENSION_LIST.Replace(" ", "").Split(',')));
        }
    }
}
