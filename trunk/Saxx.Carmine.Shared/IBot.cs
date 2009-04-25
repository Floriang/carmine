using System;
using System.Collections.Generic;

namespace Saxx.Carmine {
    public interface IBot {

        void SendMessage(string to, string message);
        void SetStatus(string status);
        bool IsOperator(string from);
       
        void Log(LogType type, string message, Exception ex);
        void Log(LogType type, string message);

        IDatabase GetDatabase(string fileName);
    }

    public enum LogType {
        Debug,
        Warn,
        Error,
        Info,
        Fatal
    }
}
