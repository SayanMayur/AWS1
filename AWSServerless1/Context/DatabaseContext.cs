using MySql.Data.MySqlClient;
//using TeleradiologyCore;
//using TeleradiologyCore.Configurations;
//using TeleradiologyCore.Extensions;
using TeleradiologyDataAccess.Uow;
using System;
using System.Data;
using System.Globalization;
using System.Threading;

namespace TeleradiologyDataAccess
{
    public class DatabaseContext : IDisposable, IDatabaseContext
    {
        public event EventHandler Commiteded;
        public event EventHandler<UnitOfWorkFailedEventArgs> Failed;

        private MySqlConnection _connection = null;
        private MySqlCommand _command = null;
        private MySqlTransaction _transaction = null;
        private bool _internalOpen;
        private bool _transactionBegan;
        private int timeout = 90000;
        public string error { get; set; }
        public string errorCode { get; set; }
        private bool _isCommitCalledBefore;
        private bool _succeed;
        private Exception _exception;
        //private readonly IConfigurationAccessor _configurationAccessor;
        private string _configurationAccessor = "";

        public Exception Exception => _exception;

        public DatabaseContext(string configurationAccessor)
        {
            _configurationAccessor = configurationAccessor;
        }
        
        public void Dispose()
        {
            
            if (_transactionBegan && !_isCommitCalledBefore)
            {
                Commit();
            }
            
            if (_command != null)
            {
                if (_command.Parameters != null) _command.Parameters.Clear();
                _command.Dispose();
            }

            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }

        /// <summary>
        /// Called to trigger <see cref="Commiteded"/> event.
        /// </summary>
        protected virtual void OnCommiteded()
        {
            //Commiteded.InvokeSafely(this);
        }

        /// <summary>
        /// Called to trigger <see cref="Failed"/> event.
        /// </summary>
        /// <param name="exception">Exception that cause failure</param>
        protected virtual void OnFailed(Exception exception)
        {
            //Failed.InvokeSafely(this, new UnitOfWorkFailedEventArgs(exception));
        }

        
        public void BeginTransaction()
        {
            PreventMultipleBegin();
            _succeed = false;
            _isCommitCalledBefore = false;
            if (_internalOpen)
            {
                if (_connection == null) OpenConnection();
                if (_connection.State != ConnectionState.Open) _connection.Open();
                if (_transaction != null)
                {
                    _transaction = _connection.BeginTransaction();
                }
            }
            else
            {
                OpenConnection();
                if (_transaction != null)
                {
                    _transaction = _connection.BeginTransaction();
                }
            }
        }
        public void Commit()
        {
            
            if (!_transactionBegan)
                return;
            try
            {
                
                PreventMultipleCommit();
                _transaction.Commit();
                _succeed = true;
                OnCommiteded();

            }
            catch (MySqlException DbEx)
            {
                _exception = DbEx;
                if (!_succeed)
                {
                    OnFailed(_exception);
                }
            }
            catch (Exception ex)
            {
                _exception = ex;
                if (!_succeed)
                {
                    OnFailed(_exception);
                }
            }
            finally
            {
                _transactionBegan = false;
                if (_transaction != null)
                    _transaction.Dispose();
                _transaction = null;
                _succeed = false;
            }
        }

        public void Rollback()
        {
            if (!_transactionBegan)
                return;
            try
            {
                _transaction.Rollback();

            }
            catch (MySqlException DbEx)
            {
                _exception = DbEx;
                throw;
            }
            catch (Exception ex)
            {
                _exception = ex;
                throw;
            }
            finally
            {
                _transactionBegan = false;
                _isCommitCalledBefore = false;
                _succeed = false;
                if (_transaction != null)
                    _transaction.Dispose();
                _transaction = null;
            }
        }

        private void PreventMultipleBegin()
        {
            if (_transactionBegan)
            {
                throw new Exception("This transaction has started before. Can not call BeginTransaction method more than once.");
            }

            _transactionBegan = true;
        }
        private void PreventMultipleCommit()
        {
            if (_isCommitCalledBefore)
            {
                throw new Exception("Commit is called before!");
            }

            _isCommitCalledBefore = true;
        }
        public bool OpenConnection()
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
            if (!_internalOpen)
            {
                if(_connection == null) _connection=new MySqlConnection(_configurationAccessor);//.ConnectionString
                /*if (_connection.ConnectionString.IsNullOrEmpty())
                {
                    Thread.CurrentThread.CurrentCulture = currentCulture;
                    throw new Exception("Connection string was not set!");
                }*/

                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();

                    _internalOpen = true;
                }
            }
            else
            {
                if (_connection == null) _connection = new MySqlConnection(_configurationAccessor);//.ConnectionString
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }
            }
            Thread.CurrentThread.CurrentCulture = currentCulture;
            return _internalOpen;
        }

        public bool CloseConnection()
        {
            if (_internalOpen)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    if (_transactionBegan)
                    {
                        Commit();
                    }
                    _connection.Close();

                    _internalOpen = false;
                }
            }
            else
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    if (_transactionBegan)
                    {
                        Commit();
                    }
                    _connection.Close();
                }
            }
            return true;
        }

        public int ExecuteNonQuery(string sql)
        {
            return ExecuteNonQuery(sql, CommandType.StoredProcedure);
        }

        public DataSet ExecuteDataSet(string sql)
        {
            return ExecuteDataSet(sql, CommandType.StoredProcedure);
        }
        public DataSet ExecuteDataSet(string sql, CommandType commandType)
        {
            try
            {
                PrepareCommand(commandType, sql);
                MySqlDataAdapter da = new MySqlDataAdapter();
                DataSet ds = new DataSet();
                da.SelectCommand = _command;
                var retval = da.Fill(ds);

                GetResultStatus();

                _command.Parameters.Clear();
                return ds;
            }
            catch (MySqlException DbEx)
            {
                throw DbEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (_command != null)
                {
                    _command.Dispose();
                }
                _command = null;
            }
        }

        private void PrepareCommand(CommandType commandType, string sql)
        {
            OpenConnection();
            if (_command == null) _command = new MySqlCommand();
            else _command.Parameters.Clear();
            _command.CommandType = commandType;
            _command.CommandTimeout = timeout;
            _command.CommandText = sql;
            _command.Connection = _connection;
            if (commandType == CommandType.StoredProcedure)
            {
                MySqlParameter parameter = new MySqlParameter();
                parameter.ParameterName = "RETURN_VALUE";
                parameter.Direction = ParameterDirection.ReturnValue;
                parameter.MySqlDbType = MySqlDbType.Int32;
                _command.Parameters.Add(parameter);
            }
        }

        public int ExecuteNonQuery(string sql, CommandType commandType)
        {
            var result = 0;
            try
            {
                PrepareCommand(commandType, sql);
                result = _command.ExecuteNonQuery();
                GetResultStatus();

                _command.Parameters.Clear();
            }
            catch (MySqlException DbEx)
            {
                throw DbEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (_command != null)
                {
                    _command.Dispose();
                }
                _command = null;
            }
            return result;
        }

        public MySqlDataReader ExecuteDataReader(string sql)
        {
            return ExecuteDataReader(sql, CommandType.StoredProcedure);
        }
        public MySqlDataReader ExecuteDataReader(string sql, CommandType commandType)
        {

            try
            {
                PrepareCommand(commandType, sql);

                var dr = _command.ExecuteReader();

                GetResultStatus();

                return dr;
            }
            catch (MySqlException DbEx)
            {
                throw DbEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (_command != null)
                {
                    _command.Parameters.Clear();
                    _command.Dispose();
                }
                _command = null;
            }
        }

        public bool AddParameter(string name, Object value, MySqlDbType type)
        {
            return AddParameter(name, value, type, 0, ParameterDirection.Input);
        }
        public bool AddParameter(string name, Object value, MySqlDbType type, int size)
        {
            return AddParameter(name, value, type, size, ParameterDirection.Input);
        }
        public bool AddParameter(string name, Object value, MySqlDbType type, int size, ParameterDirection direction)
        {

            if (_command == null) _command = new MySqlCommand();
            else _command.Parameters.Clear();
            MySqlParameter parameter = new MySqlParameter();

            parameter.ParameterName = name;
            if (value == null)
            {
                parameter.Value = DBNull.Value;
            }
            else
            {
                parameter.Value = value;
            }
            parameter.MySqlDbType = type;

            if (type == MySql.Data.MySqlClient.MySqlDbType.Int16 || type == MySql.Data.MySqlClient.MySqlDbType.Int32 || type == MySqlDbType.Decimal || type == MySqlDbType.DateTime)
            {
                ;
            }
            else if (size == 0)
            {
                parameter.Size = value.ToString().Length;
            }
            else
            {
                parameter.Size = size;
            }
            parameter.Direction = direction;

            _command.Parameters.Add(parameter);

            return true;
        }

        private void GetResultStatus()
        {
            if (_command.Parameters.Contains("@error_code"))
            {
                errorCode = Convert.ToString(_command.Parameters["@error_code"].Value);
            }
            if (_command.Parameters.Contains("@error_msg"))
            {
                error = Convert.ToString(_command.Parameters["@error_msg"].Value);
            }
        }
    }
}
