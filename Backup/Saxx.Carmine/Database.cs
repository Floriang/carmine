using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace Saxx.Carmine {
    public class Database : MarshalByRefObject, IDatabase {

        private SQLiteConnection _connection;

        public Database(string fileName) {
            if (!fileName.ToLower().EndsWith(".s3db"))
                fileName += ".s3db";
            _connection = new SQLiteConnection("Data Source=" + Path.Combine(Settings.PluginsDirectory, fileName));
            _connection.Open();
        }

        public Database() {
            _connection = new SQLiteConnection("Data Source=" + Path.Combine(Settings.PluginsDirectory, "Saxx.Carmine.s3db"));
            _connection.Open();
        }

        public void Dispose() {
            _connection.Close();
        }

        public int ExecuteCommand(string sql, params object[] parameters) {
            var command = new SQLiteCommand(sql, _connection);
            foreach (var p in parameters)
                command.Parameters.Add(GetParameter(p));
            return command.ExecuteNonQuery();
        }

        public IDataReader ExecuteReader(string sql, params object[] parameters) {
            var command = new SQLiteCommand(sql, _connection);
            foreach (var p in parameters)
                command.Parameters.Add(GetParameter(p));
            return command.ExecuteReader();
        }

        private SQLiteParameter GetParameter(object parameter) {
            if (parameter is string)
                return new SQLiteParameter(DbType.String, parameter);
            if (parameter is int)
                return new SQLiteParameter(DbType.Int32, parameter);
            if (parameter is long)
                return new SQLiteParameter(DbType.Int64, parameter);
            if (parameter is DateTime)
                return new SQLiteParameter(DbType.DateTime, parameter);
            if (parameter is bool)
                return new SQLiteParameter(DbType.Boolean, parameter);
            return null;
        }


    }
}
