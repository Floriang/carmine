using System;
using System.Collections.Generic;
using System.Linq;

namespace Saxx.Carmine.Plugins {
    public partial class Twitter : Plugin {

        private void SetupDatabase(IDatabase db) {
            try {
                db.ExecuteReader("SELECT Count(*) FROM [UserData];");
            }
            catch {
                db.ExecuteCommand("CREATE TABLE [UserData] ("
                    + "[Jid] NVARCHAR(1000) NOT NULL,"
                    + "[UserName] NVARCHAR(1000) NOT NULL,"
                    + "[Password] NVARCHAR(1000) NOT NULL,"
                    + "[LastId] LONG NOT NULL"
                    + ");");
                db.ExecuteCommand("PRAGMA auto_vacuum = 1;");
            }
        }

        private string DatabaseName {
            get {
                return this.GetType().FullName;
            }
        }

        private void SetUserData(IEnumerable<UserData> userData) {
            using (var db = GetDatabase(DatabaseName)) {
                SetupDatabase(db);

                db.ExecuteCommand("DELETE FROM [UserData];");
                foreach (var user in userData)
                    db.ExecuteCommand("INSERT INTO [UserData] ([Jid], [UserName], [Password], [LastId]) VALUES (?, ?, ?, ?);", user.Jid, user.UserName, user.Password, user.LastId);
            }
        }

        private IEnumerable<UserData> GetUserData() {
            var result = new List<UserData>();
            using (var db = GetDatabase(DatabaseName)) {
                SetupDatabase(db);

                var reader = db.ExecuteReader("SELECT [Jid], [UserName], [Password], [LastId] FROM [userData]");
                while (reader.Read())
                    result.Add(new UserData {
                        Jid = (string)reader["Jid"],
                        UserName = (string)reader["UserName"],
                        Password = (string)reader["Password"],
                        LastId = (long)reader["LastId"]
                    });
            }

            return result;
        }

        private class UserData {
            public string Jid {
                get;
                set;
            }

            public string UserName {
                get;
                set;
            }

            public string Password {
                get;
                set;
            }

            public long LastId {
                get;
                set;
            }
        }

    }
}
