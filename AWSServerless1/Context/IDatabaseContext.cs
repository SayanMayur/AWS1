using MySql.Data.MySqlClient;
using TeleradiologyCore.Dependency;
using System;
using System.Data;

namespace TeleradiologyDataAccess
{
    public interface IDatabaseContext: ITransientDependency
    {
        string error { get; }
        string errorCode { get; }
        Exception Exception { get; }
        void BeginTransaction();
        bool OpenConnection();
        bool CloseConnection();
        void Commit();
        void Rollback();
        void Dispose();
        MySqlDataReader ExecuteDataReader(string sql);
        MySqlDataReader ExecuteDataReader(string sql, CommandType commandType);
        DataSet ExecuteDataSet(string sql);
        DataSet ExecuteDataSet(string sql, CommandType commandType);
        int ExecuteNonQuery(string sql);
        int ExecuteNonQuery(string sql, CommandType commandType);

        bool AddParameter(string name, object value, MySqlDbType type);
        bool AddParameter(string name, object value, MySqlDbType type, int size);
        bool AddParameter(string name, object value, MySqlDbType type, int size, ParameterDirection direction);

    }
}