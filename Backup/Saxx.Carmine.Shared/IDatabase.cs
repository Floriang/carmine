using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Saxx.Carmine {
    public interface IDatabase : IDisposable {

        int ExecuteCommand(string sql, params object[] parameters);
        IDataReader ExecuteReader(string sql, params object[] parameters);

    }
}
