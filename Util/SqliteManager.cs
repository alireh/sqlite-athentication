using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using Athentication.Entity;
using Microsoft.Extensions.Logging;

namespace Athentication.Util
{
    public class SqliteManager
    {
        public static ILogger<SqliteManager> logger;
        private static volatile SqliteManager instance;
        private static object syncRoot = new Object();

        private static SQLiteConnection _sqlCon;
        private SQLiteCommand _sqlCmd;
        private SQLiteDataAdapter DB;
        private readonly DataSet _userDs = new DataSet();
        private DataTable _userDt = new DataTable();
        private const string _userTable = "USERS";
        private const string _userColumns = "USERNAME, FIRSTNAME, LASTNAME, EMAIL, PASSWORD, AGE, GENDER, TOKEN";

        private readonly DataSet _reportDs = new DataSet();
        private DataTable _reportDt = new DataTable();
        private const string _reportTable = "REPORT";
        private const string _reportColumns = "STATE, ACTION_DATE, USERNAME, USER_ID";
        public static SqliteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            var connectionString = Directory.GetCurrentDirectory() + "\\AuthenticationDB.db";
                            instance = new SqliteManager();
                            if (!File.Exists(connectionString))
                            {
                                var dir = Path.GetDirectoryName(connectionString);
                                if (!Directory.Exists(dir))
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                SQLiteConnection.CreateFile(connectionString);

                                using (var sqlite2 = new SQLiteConnection(string.Format("Data Source={0}", connectionString)))
                                {
                                    sqlite2.Open();
                                    string sql = $"CREATE TABLE {_userTable} ( [ID] INTEGER  PRIMARY KEY NOT NULL" +
                                                                            ", [USERNAME] TEXT NOT NULL" +
                                                                            ", [FIRSTNAME] TEXT NOT NULL" +
                                                                            ", [LASTNAME] TEXT  NOT NULL" +
                                                                            ", [EMAIL] TEXT NULL" +
                                                                            ", [PASSWORD] TEXT NOT NULL" +
                                                                            ", [AGE] INTEGER NOT NULL" +
                                                                            ", [GENDER] INTEGER NOT NULL" +
                                                                            ", [TOKEN] TEXT NULL);";
                                    SQLiteCommand command = new SQLiteCommand(sql, sqlite2);
                                    command.ExecuteNonQuery();
                                }

                                using (var sqlite2 = new SQLiteConnection(string.Format("Data Source={0}", connectionString)))
                                {
                                    sqlite2.Open();
                                    string sql = $"CREATE TABLE {_reportTable} ( [ID] INTEGER  PRIMARY KEY NOT NULL" +
                                                                            ", [STATE] TEXT NOT NULL" +
                                                                            ", [ACTION_DATE] TEXT NOT NULL" +
                                                                            ", [USERNAME] TEXT NOT NULL" +
                                                                            ", [USER_ID] INTEGER NOT NULL);";
                                    SQLiteCommand command = new SQLiteCommand(sql, sqlite2);
                                    command.ExecuteNonQuery();
                                }
                            }
                            SetConnection(connectionString);
                        }
                    }
                }

                return instance;
            }
        }

        private static void SetConnection(string connectionStringModel)
        {
            var con = string.Format("DATA SOURCE=" + connectionStringModel + ";VERSION=3;");
            _sqlCon = new SQLiteConnection(con);
        }

        public void DeleteUser(double id)
        {
            try
            {
                logger?.LogInformation("DeleteUser");
                string txtSqlQuery = String.Format("DELETE FROM {0} WHERE ID = {1}", _userTable, id);
                ExecuteQuery(txtSqlQuery);
            }
            catch (Exception ex)
            {
                _sqlCon.Close();
                logger?.LogError($"DeleteUser Exception Message : {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public int AddUser(UserRawInfo userInfo)
        {
            try
            {
                logger?.LogInformation("AddUser");
                var cnt = GetUserCount();
                if (cnt != -1)
                {
                    var id = cnt + 1;
                    var query = string.Format("INSERT INTO {0}(ID, {1}) VALUES({2} , {3});", _userTable, _userColumns, id, ConvertToQueryValue(userInfo));
                    ExecuteQuery(query);
                    return id;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"DeleteUser Exception Message : {ex.Message}");
                _sqlCon.Close();
                return -1;
            }
            return -1;
        }

        public void AddReport(ReportInfo reportInfo)
        {
            try
            {
                logger?.LogInformation("AddReport");
                var cnt = GetReportCount();
                if (cnt != -1)
                {
                    var id = cnt + 1;
                    var query = string.Format("INSERT INTO {0}(ID, {1}) VALUES({2} , {3});", _reportTable, _reportColumns, id, ConvertToQueryValue(reportInfo));
                    ExecuteQuery(query);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"AddReport Exception Message : {ex.Message}");
                _sqlCon.Close();
            }
        }

        public int GetUserCount()
        {
            try
            {
                logger?.LogInformation("GetUserCount");
                var count = ExecuteScalar(string.Format("SELECT COUNT(*) FROM {0}", _userTable));
                return count;
            }
            catch (Exception ex)
            {
                logger?.LogError($"GetUserCount Exception Message : {ex.Message}");
                _sqlCon.Close();
                return -1;
            }
        }

        public int GetReportCount()
        {
            try
            {
                logger?.LogInformation("GetReportCount");
                var count = ExecuteScalar(string.Format("SELECT COUNT(*) FROM {0}", _reportTable));
                return count;
            }
            catch (Exception ex)
            {
                logger?.LogError($"GetReportCount Exception Message : {ex.Message}");
                _sqlCon.Close();
                return -1;
            }
        }

        public bool ExistsUser(string username, string password)
        {
            try
            {
                logger?.LogInformation("GetUserCount");
                var count = ExecuteScalar($"SELECT COUNT(*) FROM {_userTable} WHERE USERNAME = '{username}' AND PASSWORD = '{password}' ");
                var exists = count > 0;
                if (exists)
                {
                    string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                    var user = GetUser(username);
                    UpdateUserToken(user.Id, token);
                }
                return exists;
            }
            catch (Exception ex)
            {
                logger?.LogError($"GetUserCount Exception Message : {ex.Message}");
                _sqlCon.Close();
                return false;
            }
        }

        public bool ExistsUser(string username)
        {
            try
            {
                logger?.LogInformation("GetUserCount");
                var count = ExecuteScalar($"SELECT COUNT(*) FROM {_userTable} WHERE USERNAME = '{username}' ");
                return count > 0;
            }
            catch (Exception ex)
            {
                logger?.LogError($"GetUserCount Exception Message : {ex.Message}");
                _sqlCon.Close();
                return false;
            }
        }

        private string ConvertToQueryValue(UserRawInfo userInfo)
        {
            try
            {
                logger?.LogInformation("ConvertToQueryValue");
                var gender = userInfo.Gender == null ? -1 : (userInfo.Gender == true ? 1 : 0);
                return $"'{userInfo.Username}','{userInfo.Firstname}','{userInfo.Lastname}','{userInfo.Email}','{userInfo.Password}','{userInfo.Age}','{gender}','{userInfo.Token}'";
            }
            catch (Exception ex)
            {
                logger?.LogError($"ConvertToQueryValue Exception Message : {ex.Message}");
            }
            return null;
        }

        private string ConvertToQueryValue(ReportInfo reportInfo)
        {
            try
            {
                logger?.LogInformation("ConvertToQueryValue");
                return $"'{reportInfo.State}','{reportInfo.ActionDate}','{reportInfo.Username}','{reportInfo.UserId}'";
            }
            catch (Exception ex)
            {
                logger?.LogError($"ConvertToQueryValue Exception Message : {ex.Message}");
            }
            return null;
        }

        private void ExecuteQuery(string txtQuery)
        {
            try
            {
                logger?.LogInformation("ExecuteQuery");
                _sqlCon.Open();

                _sqlCmd = _sqlCon.CreateCommand();
                _sqlCmd.CommandText = txtQuery;

                _sqlCmd.ExecuteNonQuery();
                _sqlCon.Close();
            }
            catch (Exception ex)
            {
                _sqlCon.Close();
                logger?.LogError($"ExecuteQuery Exception Message : {ex.Message}");
            }
        }

        private int ExecuteScalar(string txtQuery)
        {
            try
            {
                logger?.LogInformation("ExecuteScalar");
                _sqlCon.Open();
                _sqlCmd = new SQLiteCommand(txtQuery, _sqlCon);
                var result = _sqlCmd.ExecuteScalar();
                _sqlCon.Close();
                return int.Parse(result.ToString());
            }
            catch (Exception ex)
            {
                _sqlCon.Close();
                logger?.LogError($"ExecuteScalar Exception Message : {ex.Message}");
            }
            return -1;
        }

        public void EditUser(UserInfo userInfo)
        {
            try
            {
                logger?.LogInformation("EditUser");
                string txtSqlQuery = $"UPDATE {_userTable} SET USERNAME =\"{userInfo.Username}\", FIRSTNAME = \"{userInfo.Firstname}\", LASTNAME = \"{userInfo.Lastname}\", EMAIL =\"{userInfo.Email}\", PASSWORD =\"{userInfo.Password}\", AGE =\"{userInfo.Age}\", GENDER = \"{userInfo.Gender}\", TOKEN = \"{userInfo.Token}\" WHERE ID ={ userInfo.Id} ";
                ExecuteQuery(txtSqlQuery);
            }
            catch (Exception ex)
            {
                logger?.LogError($"EditUser Exception Message : {ex.Message}");
                _sqlCon.Close();
            }
        }

        public bool UpdateUserToken(int id, string token)
        {
            try
            {
                logger?.LogInformation("EditUser");
                string txtSqlQuery = $"UPDATE {_userTable} SET TOKEN =\"{token}\" WHERE ID ={id} ";
                ExecuteQuery(txtSqlQuery);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError($"EditUser Exception Message : {ex.Message}");
                return false;
            }
        }

        public UserInfo GetUser(int id)
        {
            try
            {
                logger?.LogInformation("GetUser");
                _sqlCon.Open();

                _sqlCmd = _sqlCon.CreateCommand();
                string commandText = $"select id,{_userColumns} from {_userTable} where id = {id}";
                DB = new SQLiteDataAdapter(commandText, _sqlCon);
                _userDs.Reset();
                DB.Fill(_userDs);
                _userDt = _userDs.Tables[0];
                _sqlCon.Close();


                var item = _userDt.Rows[0];
                bool? gender = item[7].ToString() == "1";

                var userInfo = new UserInfo(item[1].ToString(),
                                            item[2].ToString(),
                                            item[3].ToString(),
                                            item[4].ToString(),
                                            item[5].ToString(),
                                            int.Parse(item[6].ToString()),
                                            gender,
                                            item[8].ToString());

                userInfo.Id = id;

                return userInfo;
            }
            catch (Exception ex)
            {
                logger?.LogError($"GetUser Exception Message : {ex.Message}");
                _sqlCon.Close();
                return null;
            }
        }

        public UserInfo GetUser(string username)
        {
            try
            {
                logger?.LogInformation("GetUser");
                _sqlCon.Open();

                _sqlCmd = _sqlCon.CreateCommand();
                string commandText = $"SELECT ID,{_userColumns} FROM {_userTable} WHERE USERNAME = '{username}'";
                DB = new SQLiteDataAdapter(commandText, _sqlCon);
                _userDs.Reset();
                DB.Fill(_userDs);
                _userDt = _userDs.Tables[0];
                _sqlCon.Close();


                var item = _userDt.Rows[0];
                bool? gender = item[7].ToString() == "1";

                var userInfo = new UserInfo(item[1].ToString(),
                                            item[2].ToString(),
                                            item[3].ToString(),
                                            item[4].ToString(),
                                            item[5].ToString(),
                                            int.Parse(item[6].ToString()),
                                            gender,
                                            item[8].ToString());

                userInfo.Id = int.Parse(item[0].ToString());

                return userInfo;
            }
            catch (Exception ex)
            {
                logger?.LogError($"GetUser Exception Message : {ex.Message}");
                _sqlCon.Close();
                return null;
            }
        }

        public List<UserInfo> GetUsers()
        {
            try
            {
                logger?.LogInformation("GetUsers");
                var result = new List<UserInfo>();
                _sqlCon.Open();

                _sqlCmd = _sqlCon.CreateCommand();
                string commandText = $"SELECT ID,{_userColumns} FROM {_userTable}";
                DB = new SQLiteDataAdapter(commandText, _sqlCon);
                _userDs.Reset();
                DB.Fill(_userDs);
                _userDt = _userDs.Tables[0];
                _sqlCon.Close();
                for (int i = 0; i < _userDt.Rows.Count; i++)
                {
                    var item = _userDt.Rows[i];
                    bool? gender = item[7].ToString() == "1";

                    var id = int.Parse(item[0].ToString());
                    var userInfo = new UserInfo(item[1].ToString(),
                                                item[2].ToString(),
                                                item[3].ToString(),
                                                item[4].ToString(),
                                                item[5].ToString(),
                                                int.Parse(item[6].ToString()),
                                                gender,
                                                item[8].ToString());

                    userInfo.Id = id;
                    result.Add(userInfo);
                }
                return result;
            }
            catch (Exception ex)
            {
                logger?.LogError($"GetUsers Exception Message : {ex.Message}");
                _sqlCon.Close();
                return null;
            }
        }

        public List<ReportInfo> GetReports()
        {
            try
            {
                logger?.LogInformation("GetReports");
                var result = new List<ReportInfo>();
                _sqlCon.Open();

                _sqlCmd = _sqlCon.CreateCommand();
                string commandText = $"SELECT ID,{_reportColumns} FROM {_reportTable}";
                DB = new SQLiteDataAdapter(commandText, _sqlCon);
                _reportDs.Reset();
                DB.Fill(_reportDs);
                _reportDt = _reportDs.Tables[0];
                _sqlCon.Close();
                for (int i = 0; i < _reportDt.Rows.Count; i++)
                {
                    var item = _reportDt.Rows[i];
                    bool? gender = item[7].ToString() == "1";

                    var id = int.Parse(item[0].ToString());
                    var reportInfo = new ReportInfo
                    {
                        State = item[1].ToString(),
                        ActionDate = item[2].ToString(),
                        Username = item[3].ToString(),
                        UserId = int.Parse(item[4].ToString()),
                    };

                    reportInfo.Id = id;
                    result.Add(reportInfo);
                }
                return result;
            }
            catch (Exception ex)
            {
                logger?.LogError($"GetReports Exception Message : {ex.Message}");
                _sqlCon.Close();
                return null;
            }
        }
    }
}
