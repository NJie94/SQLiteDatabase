using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;

namespace SQLiteDatabase
{

    public class SQLiteDatabase
    {
        // Parameter & Properties
        private SQLiteConnection m_Connection;
        private SQLiteCommand m_Command;
        //private SQLiteDataAdapter DB;
        private SQLiteDataReader m_Read;

        public enum eDataType
        {
            BIT,
            BLOB,
            INTEGER,
            NUMERIC,
            REAL,
            TEXT
        }

        /// <summary>
        /// Establish SQLite Connection
        /// </summary>
        /// <param name="DBPath"> Connection Path with Database Name </param>
        /// <returns> True=Success; False=Fail </returns>
        public bool SetConnection(string DBPath)
        {
            bool ret = true;
            string strPath = DBPath;
            string strDataSource;
            strDataSource = string.Format("Data Source={0}; Version = 3", strPath);
            try
            {
                m_Connection = new SQLiteConnection(strDataSource, true);
            }
            catch (Exception e)
            {
                Debug.WriteLine("SQLite.dll:" + e.ToString());
                ret = false;
            }
            return ret;
        }

        object lockObject = new object();
        private bool ExecuteQuery(string Query)
        {
            lock (lockObject)
            {
                bool ret = true;

                m_Connection.Open();
                m_Command = m_Connection.CreateCommand();
                m_Command.CommandText = Query;
                try
                {
                    m_Command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("SQLite.dll:" + e.ToString());
                    ret = false;
                }
                m_Connection.Close();

                return ret;
            }
        }

        public string SQLQuery(string sSQLString, System.Data.DataTable oResultDataTable)
        {
            lock (lockObject)
            {
                string sResult = "";
                try
                {
                    m_Connection.Open();
                    SQLiteDataAdapter oSQLiteDataAdapter = new SQLiteDataAdapter(sSQLString, m_Connection);
                    oSQLiteDataAdapter.Fill(oResultDataTable);
                    oSQLiteDataAdapter.Dispose();
                    m_Connection.Close();
                }
                catch (Exception e)
                {
                    oResultDataTable = null;
                    sResult = e.Message;
                }
                return sResult;
            }
        }

        private List<string> ExecuteReader(string Table, string Query)
        {
            string strTemp = "";
            List<string> Data = new List<string>();
            List<string> ColumnName = GetColumnName(Table);
            lock (this)
            {
                m_Connection.Open();
                m_Command = m_Connection.CreateCommand();
                m_Command.CommandText = Query;
                try
                {
                    using (m_Read = m_Command.ExecuteReader())
                    {
                        while (m_Read.Read())
                        {
                            foreach (string item in ColumnName)
                            {
                                if (strTemp == "")
                                {
                                    strTemp = m_Read[item].ToString();
                                }
                                else
                                {
                                    strTemp = strTemp + "," + m_Read[item].ToString();
                                }
                            }
                            Data.Add(strTemp);
                            strTemp = "";
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("SQLite.dll:" + e.ToString());
                    m_Connection.Close();
                    return null;
                }
                m_Connection.Close();
            }
            return Data;
        }

        private List<string> GetColumnName(string Table)
        {
            List<string> ColumnName = new List<string>();
            lock (this)
            {
                m_Connection.Open();
                m_Command = m_Connection.CreateCommand();
                m_Command.CommandText = string.Format("PRAGMA TABLE_INFO({0})", Table);
                try
                {
                    using (m_Read = m_Command.ExecuteReader())
                    {
                        while (m_Read.Read())
                        {
                            ColumnName.Add(m_Read["NAME"].ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("SQLite.dll:" + e.ToString());
                    m_Connection.Close();
                    return null;
                }
                m_Connection.Close();
            }
            return ColumnName;
        }

        /// <summary>
        /// Create Table
        /// </summary>
        /// <param name="Name"> Name of Table </param>
        /// <param name="Columns"> Declare [] Columns with format "[column] datatype NULL/NOT NULL" </param>
        /// <returns> True=Success; False=Fail</returns>
        public bool CreateTable(string Name, string[] Columns)
        {
            bool ret = true;
            string _Columns = "";
            foreach (string item in Columns)
            {
                _Columns = _Columns + ", " + item;
            }
            string strCommand = @"Create Table IF NOT Exists " + Name +
                                @"([NO] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT"
                                + _Columns + ");";
            ret = ExecuteQuery(strCommand);
            return ret;
        }

        /// <summary>
        /// Insert one column with value
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="Column"> Declare Column in string </param>
        /// <param name="Value"> Declare Value in string </param>
        /// <returns> True=Success; False=Fail </returns>
        public bool Insert(string Table, string Column, string Value)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strCommand = string.Format("INSERT INTO {0} ({1}) VALUES ('{2}')", Table, Column, Value);
                ret = ExecuteQuery(strCommand);
                return ret;
            }
        }

        /// <summary>
        /// Insert multiple column with value
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="Column_Value"> Declare []Columns_Value with format "column=value"</param>
        /// <returns> True=Success; False=Fail </returns>
        public bool Insert(string Table, string[] Column_Value)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strColumnName = "";
                string strValue = "";
                string strCommand;
                strValue = "";
                foreach (string item in Column_Value)
                {
                    try
                    {
                        string[] Temp = item.Split('=');
                        // Column
                        if (strColumnName == "")
                        {
                            strColumnName = Temp[0];
                        }
                        else
                        {
                            strColumnName = strColumnName + ", " + Temp[0];
                        }
                        // Value
                        if (strValue == "")
                        {
                            strValue = "'" + Temp[1] + "'";
                        }
                        else
                        {
                            strValue = strValue + ", " + "'" + Temp[1] + "'";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("SQLite.dll:" + e.ToString());
                        return false;
                    }
                }
                strCommand = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", Table, strColumnName, strValue);
                ret = ExecuteQuery(strCommand);
                return ret;
            }
        }
        List<string> comm = new List<string>();
        List<string> cloname = new List<string>();

        public bool InsertSub(string path, string Table, List<string[]> Column_Value)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strColumnName = "";
                string strValue = "";
                string strCommand;
                strValue = "";
                comm.Clear();
                cloname.Clear();
                //string strConnectionPath = Path.Combine("@"Data Source = ",)
                foreach (string[] item in Column_Value)
                {
                    foreach (var a in item)
                    {
                        try
                        {
                            string[] Temp = a.Split('=');
                            // Column
                            if (strColumnName == "")
                            {
                                strColumnName = Temp[0];
                            }
                            else
                            {
                                strColumnName = strColumnName + ", " + Temp[0];
                            }
                            // Value
                            if (strValue == "")
                            {
                                strValue = "'" + Temp[1] + "'";
                            }
                            else
                            {
                                strValue = strValue + ", " + "'" + Temp[1] + "'";
                            }

                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("SQLite.dll:" + e.ToString());
                            return false;
                        }
                    }
                    comm.Add(strValue);
                    cloname.Add(strColumnName);
                    strValue = "";
                    strColumnName = "";

                }
                //using (var conn = new SQLiteConnection(@"Data Source=C:\Users\USER1\Desktop\(jz Test)\18YEE001_MagToMagAOIMachine\Projects\18YEE001_MagToMagInspectionMachine\bin\Debug\Database\LFInspectionDatabase.db"))
                //{
                try
                {
                    m_Connection.Open();
                }
                catch (Exception ex)
                {

                }
                using (var cmd = new SQLiteCommand(m_Connection))
                {
                    using (var transaction = m_Connection.BeginTransaction())
                    {
                        for (int i = 0; i < comm.Count; i++)
                        {
                            cmd.CommandText = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", Table, cloname[i], comm[i]);
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }
                m_Connection.Close();
                //}
                return ret;
            }
        }

        /// <summary>
        /// Insert a row (multiple column with value)
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="Value"> Value for all column in [] with format "string"</param>
        /// <returns> True=Success; False=Fail </returns>
        public bool InsertRow(string Table, string[] Value)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strColumnName = "";
                string strValue = "";
                string strCommand;
                List<string> ColumnName = GetColumnName(Table);
                strValue = "";
                for (int i = 1; i < ColumnName.Count; i = i + 1)
                {
                    if (strColumnName == "")
                    {
                        strColumnName = ColumnName[i];
                    }
                    else
                    {
                        strColumnName = strColumnName + ", " + ColumnName[i];
                    }
                }

                foreach (string item in Value)
                {
                    if (strValue == "")
                    {
                        strValue = "'" + item + "'";
                    }
                    else
                    {
                        strValue = strValue + ", " + "'" + item + "'";
                    }
                }
                strCommand = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", Table, strColumnName, strValue);
                ret = ExecuteQuery(strCommand);
                return ret;
            }
        }

        /// <summary>
        /// Read a Table contains
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="Data"> Data return as out List<string>XXXX </param>
        /// <returns> True=Success; False=Fail </returns>
        public bool Read(string Table, out List<string> Data)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strCommand = string.Format("SELECT * FROM {0} ", Table);
                Data = ExecuteReader(Table, strCommand);
                try
                {
                    if (Data == null || Data.Count == 0)
                    {
                        ret = false;
                    }
                }
                catch
                {
                    ret = false;
                }
                return ret;
            }
        }
        public void ReadTable(string _TableName, out DataTable _OutTable)
        {
            string strCommand = string.Format("SELECT * FROM {0} ", _TableName);
            DataTable dt = new DataTable();
            _OutTable = dt;

            lock (this)
            {
                m_Connection.Open();
                using (SQLiteDataAdapter da = new SQLiteDataAdapter(strCommand, m_Connection))
                {
                    using (new SQLiteCommandBuilder(da))
                    {
                        da.Fill(dt);
                        da.Update(dt);
                    }
                }
                m_Connection.Close();
                _OutTable = dt;
            }
        }

        public bool ReadSub(string Table, System.Data.DataTable Data)
        {
            string strCommand = string.Format("SELECT * FROM {0} ", Table);
            return (SQLQuery(strCommand, Data) == "");
        }

        /// <summary>
        /// Read particular value for a column with a condition
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="Column"> Search Column </param>
        /// <param name="Value"> Search Value </param>
        /// <param name="Data"> Data return as out List<string>XXXX </param>
        /// <returns> True=Success; False=Fail </returns>
        public bool Read(string Table, string Column, string Value, out List<string> Data)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strCommand = string.Format("SELECT * FROM {0} WHERE {1} IN ('{2}')", Table, Column, Value);
                Data = ExecuteReader(Table, strCommand);
                if ((Data == null) || (!Data.Any()) || Data.Count == 0)
                {
                    ret = false;
                }
                return ret;
            }
        }

        public bool ReadSub(string Table, string Column, string Value, System.Data.DataTable Data)
        {
            string strCommand = string.Format("SELECT * FROM {0} WHERE {1} IN ('{2}')", Table, Column, Value);
            return (SQLQuery(strCommand, Data) == "");
        }

        /// <summary>
        /// Read particular value for a column with multiple condition
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="Column_Value"> Search Column and Value with [] format "column=value" </param>
        /// <param name="Data"> Data return as out List<string>XXXX </param>
        /// <returns> True=Success; False=Fail </returns>
        public bool Read(string Table, string[] Column_Value, out List<string> Data)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strCol_Val = "";
                foreach (string item in Column_Value)
                {
                    try
                    {
                        string[] Temp = item.Split('=');
                        if (strCol_Val == "")
                        {
                            strCol_Val = string.Format("{0} = '{1}'", Temp[0], Temp[1]);
                        }
                        else
                        {
                            strCol_Val = strCol_Val + string.Format(" and {0} = '{1}'", Temp[0], Temp[1]);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("SQLite.dll:" + e.ToString());
                        Data = null;
                        return false;
                    }
                }
                string strCommand = string.Format("SELECT * FROM {0} WHERE {1} ", Table, strCol_Val);
                Data = ExecuteReader(Table, strCommand);
                if (Data != null && Data.Count == 0)
                {
                    ret = false;
                }
                return ret;
            }
        }

        public bool ReadSub(string Table, string[] Column_Value, System.Data.DataTable Data)
        {
            string strCol_Val = "";
            foreach (string item in Column_Value)
            {
                try
                {
                    string[] Temp = item.Split('=');
                    if (strCol_Val == "")
                    {
                        strCol_Val = string.Format("{0} = '{1}'", Temp[0], Temp[1]);
                    }
                    else
                    {
                        strCol_Val = strCol_Val + string.Format(" and {0} = '{1}'", Temp[0], Temp[1]);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("SQLite.dll:" + e.ToString());
                    Data = null;
                    return false;
                }
            }
            string strCommand = string.Format("SELECT * FROM {0} WHERE {1} ", Table, strCol_Val);
            return (SQLQuery(strCommand, Data) == "");
        }

        /// <summary>
        /// Read table all data
        /// </summary>
        /// <param name="Table">Table Name</param>
        /// <param name="Data"> Data return as out List<string>XXXX</param>
        /// <returns></returns>
        public bool ReadtTableAllData(string Table, out List<string> Data)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strCommand = string.Format("SELECT * FROM {0}", Table);
                Data = ExecuteReader(Table, strCommand);
                if ((Data == null) || (!Data.Any()) || Data.Count == 0)
                {
                    ret = false;
                }
                return ret;
            }
        }

        /// <summary>
        /// Get data from Table within period
        /// </summary>
        /// <param name="TableName">Name of Table</param>
        /// <param name="strColumnName">Column name used for DateTime</param>
        /// <param name="strDateTimeFormat">DateTime format</param>
        /// <param name="dtStartDateTime">Start DateTime</param>
        /// <param name="dtEndDateTime">End DateTime</param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public bool ReadtTableDataWithinPeriod(string TableName, string strColumnName, string strDateTimeFormat, DateTime dtStartDateTime, DateTime dtEndDateTime, out List<string> Data)
        {
            lock (lockObject)
            {
                string strCommand;
                bool ret = true;

                strCommand = string.Format("SELECT * FROM {0} WHERE {1} >= '{2}' AND {3} <= '{4}'",
                    TableName, strColumnName, dtStartDateTime.ToString(strDateTimeFormat), strColumnName, dtEndDateTime.ToString(strDateTimeFormat));

                //strCommand = string.Format("SELECT * FROM {0} WHERE {1} BETWEEN {2} AND {3}", TableName, )

                Data = ExecuteReader(TableName, strCommand);
                if ((Data == null) || (!Data.Any()) || Data.Count == 0)
                {
                    ret = false;
                }
                return ret;
            }
        }

        public bool ReadtTableDataWithinPeriodSub(string TableName, string strColumnName, string strDateTimeFormat, DateTime dtStartDateTime, DateTime dtEndDateTime, System.Data.DataTable Data)
        {
            string strCommand = string.Format("SELECT * FROM {0} WHERE {1} >= '{2}' AND {3} <= '{4}'",
                TableName, strColumnName, dtStartDateTime.ToString(strDateTimeFormat), strColumnName, dtEndDateTime.ToString(strDateTimeFormat));
            return (SQLQuery(strCommand, Data) == "");
        }

        /// <summary>
        /// Get data from Table since period
        /// </summary>
        /// <param name="TableName">Name of Table</param>
        /// <param name="strColumnName">Column name used for DateTim</param>
        /// <param name="strDateTimeFormat">DateTime format</param>
        /// <param name="dtStartDateTime">Start DateTime</param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public bool ReadtTableDataSincePeriod(string TableName, string strColumnName, string strDateTimeFormat, DateTime dtStartDateTime, out List<string> Data)
        {
            lock (lockObject)
            {
                string strCommand;
                bool ret = true;

                strCommand = string.Format("SELECT * FROM {0} WHERE {1} >= '{2}'", TableName, strColumnName, dtStartDateTime.ToString(strDateTimeFormat));

                Data = ExecuteReader(TableName, strCommand);
                if ((Data == null) || (!Data.Any()) || Data.Count == 0)
                {
                    ret = false;
                }
                return ret;
            }
        }

        public bool ReadtTableDataBeforePeriod(string TableName, string strColumnName, string strDateTimeFormat, DateTime dtEndDateTime, out List<string> Data)
        {
            lock (lockObject)
            {
                string strCommand;
                bool ret = true;

                strCommand = string.Format("SELECT * FROM {0} WHERE {1} <= '{2}'", TableName, strColumnName, dtEndDateTime.ToString(strDateTimeFormat));

                Data = ExecuteReader(TableName, strCommand);
                if ((Data == null) || (!Data.Any()) || Data.Count == 0)
                {
                    ret = false;
                }
                return ret;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public bool ReadTableLastData(string Table, out List<string> Data)
        {
            lock (lockObject)
            {
                string strCommand;
                bool ret = true;
                Data = new List<string>();
                strCommand = string.Format("SELECT * FROM {0} ORDER BY {1} DESC LIMIT 1", Table, "NO");

                Data = ExecuteReader(Table, strCommand);
                if ((Data == null) || (!Data.Any()) || Data.Count == 0)
                {
                    ret = false;
                }

                return ret;
            }
        }

        public bool ReadTableLastDataSub(string Table, System.Data.DataTable Data)
        {
            string strCommand = string.Format("SELECT * FROM {0} ORDER BY {1} DESC LIMIT 1", Table, "NO");
            return (SQLQuery(strCommand, Data) == "");
        }

        /// <summary>
        /// Update a column value with a condition
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="TargetCol"> Target Column </param>
        /// <param name="TargetVal"> Target Value </param>
        /// <param name="Column_Value"> Column and value want to update in format "Column=Value" as string </param>
        /// <returns> True=Success; False=Fail </returns>
        public bool Update(string Table, string TargetCol, string TargetVal, string Column_Value) //TargetCol=UserID, TargetVal=davidtan, LastLogin=Datetime.now
        {
            lock (lockObject)
            {
                bool ret = true;
                string Column;
                string Value;
                try
                {
                    string[] Temp = Column_Value.Split('=');
                    Column = Temp[0];
                    Value = Temp[1];
                }
                catch (Exception e)
                {
                    Debug.WriteLine("SQLite.dll:" + e.ToString());
                    return false;
                }
                string strCommand = string.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = '{4}'", Table, Column, Value, TargetCol, TargetVal);
                ret = ExecuteQuery(strCommand);
                return ret;
            }
        }

        /// <summary>
        /// Update mulitple columns value with a condition
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="TargetCol"> Target Column </param>
        /// <param name="TargetVal"> Target Value </param>
        /// <param name="Column_Value"> Column and value want to update in format "Column=Value" as string[] </param>
        /// <returns> True=Success; False=Fail </returns>
        public bool Update(string Table, string TargetCol, string TargetVal, string[] Column_Value)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strColumn_Value = "";
                string strCommand;
                strColumn_Value = "";
                foreach (string item in Column_Value)
                {
                    try
                    {
                        string[] Temp = item.Split('=');
                        if (strColumn_Value == "")
                        {
                            strColumn_Value = Temp[0] + " = '" + Temp[1] + "'";
                        }
                        else
                        {
                            strColumn_Value = strColumn_Value + ", " + Temp[0] + " = '" + Temp[1] + "'";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("SQLite.dll:" + e.ToString());
                        return false;
                    }
                }
                strCommand = string.Format("UPDATE {0} SET {1} WHERE {2} = '{3}'", Table, strColumn_Value, TargetCol, TargetVal);
                ret = ExecuteQuery(strCommand);
                return ret;
            }
        }

        /// <summary>
        /// Update a column value with multiple condition
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="TargCol_TargVal"> Target Column and target value want to update in format "TargetColumn=TargetValue" as string[] </param>
        /// <param name="Column_Value"> Column and value want to update in format "Column=Value" as string </param>
        /// <returns></returns>
        public bool Update(string Table, string[] TargCol_TargVal, string Column_Value)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strTargCol_TargVal = "";
                string Column = "";
                string Value = "";
                string strCommand;
                foreach (string item in TargCol_TargVal)
                {
                    try
                    {
                        string[] Temp = item.Split('=');
                        if (strTargCol_TargVal == "")
                        {
                            strTargCol_TargVal = Temp[0] + " = '" + Temp[1] + "'";
                        }
                        else
                        {
                            strTargCol_TargVal = strTargCol_TargVal + " and " + Temp[0] + " = '" + Temp[1] + "'";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("SQLite.dll:" + e.ToString());
                        return false;
                    }
                }
                try
                {
                    string[] Temp = Column_Value.Split('=');
                    Column = Temp[0];
                    Value = Temp[1];
                }
                catch (Exception e)
                {
                    Debug.WriteLine("SQLite.dll:" + e.ToString());
                    return false;
                }
                strCommand = string.Format("UPDATE {0} SET {1} = '{2}' WHERE {3}", Table, Column, Value, strTargCol_TargVal);
                ret = ExecuteQuery(strCommand);
                return ret;
            }
        }

        /// <summary>
        ///  Update multiple column value with multiple condition
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="TargCol_TargVal"> Target Column and target value want to update in format "TargetColumn=TargetValue" as string[] </param>
        /// <param name="Column_Value"> Column and value want to update in format "Column=Value" as string[] </param>
        /// <returns></returns>
        public bool Update(string Table, string[] TargCol_TargVal, string[] Column_Value)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strTargCol_TargVal = "";
                string strColumn_Value = "";
                string strCommand;
                strColumn_Value = "";
                foreach (string item in TargCol_TargVal)
                {
                    try
                    {
                        string[] Temp = item.Split('=');
                        if (strTargCol_TargVal == "")
                        {
                            strTargCol_TargVal = Temp[0] + " = '" + Temp[1] + "'";
                        }
                        else
                        {
                            strTargCol_TargVal = strTargCol_TargVal + " and " + Temp[0] + " = '" + Temp[1] + "'";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("SQLite.dll:" + e.ToString());
                        return false;
                    }
                }
                foreach (string item in Column_Value)
                {
                    try
                    {
                        string[] Temp = item.Split('=');
                        if (strColumn_Value == "")
                        {
                            strColumn_Value = Temp[0] + " = '" + Temp[1] + "'";
                        }
                        else
                        {
                            strColumn_Value = strColumn_Value + ", " + Temp[0] + " = '" + Temp[1] + "'";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("SQLite.dll:" + e.ToString());
                        return false;
                    }
                }
                strCommand = string.Format("UPDATE {0} SET {1} WHERE {2}", Table, strColumn_Value, strTargCol_TargVal);
                ret = ExecuteQuery(strCommand);
                return ret;
            }
        }

        /// <summary>
        /// Remove a row
        /// </summary>
        /// <param name="Table"> Name of Table </param>
        /// <param name="Column_Value"> Column and value want to remove in format "Column=Value" as string </param>
        /// <returns> True=Success; False=Fail </returns>
        public bool Remove(string Table, string Column_Value)
        {
            lock (lockObject)
            {
                bool ret = true;
                string strCommand;
                string Column = "";
                string Value = "";
                try
                {
                    string[] Temp = Column_Value.Split('=');
                    Column = Temp[0];
                    Value = Temp[1];
                }
                catch (Exception e)
                {
                    Debug.WriteLine("SQLite.dll:" + e.ToString());
                    return false;
                }
                strCommand = string.Format("DELETE FROM {0} WHERE {1} = '{2}'", Table, Column, Value);
                ret = ExecuteQuery(strCommand);
                return ret;
            }
        }
    }
}
