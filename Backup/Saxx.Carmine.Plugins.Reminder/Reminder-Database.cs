using System;
using System.Collections.Generic;
using System.Linq;

namespace Saxx.Carmine.Plugins {
    public partial class Reminder : Plugin {

        private void SetupDatabase(IDatabase db) {
            try {
                db.ExecuteReader("SELECT Count(*) FROM [ReminderInfo];");
            }
            catch {
                db.ExecuteCommand("CREATE TABLE [ReminderInfo] ("
                    + "[Jid] NVARCHAR(1000) NOT NULL,"
                    + "[Message] NVARCHAR(1000) NOT NULL,"
                    + "[Date] TIMESTAMP NOT NULL"
                    + ");");
                db.ExecuteCommand("PRAGMA auto_vacuum = 1;");
            }
        }

        private string DatabaseName {
            get {
                return this.GetType().FullName;
            }
        }

        private void SetReminderInfos(IEnumerable<ReminderInfo> reminderInfos) {
            using (var db = GetDatabase(DatabaseName)) {
                SetupDatabase(db);

                db.ExecuteCommand("DELETE FROM [ReminderInfo];");
                foreach (var reminderInfo in reminderInfos)
                    db.ExecuteCommand("INSERT INTO [ReminderInfo] ([Jid], [Message], [Date]) VALUES (?, ?, ?);", reminderInfo.Jid, reminderInfo.Message, reminderInfo.Date);
            }

            _nextReminder = null;
        }

        private IEnumerable<ReminderInfo> GetReminderInfos() {
            var result = new List<ReminderInfo>();
            using (var db = GetDatabase(DatabaseName)) {
                SetupDatabase(db);

                var reader = db.ExecuteReader("SELECT [Jid], [Message], [Date] FROM [ReminderInfo] ORDER BY [Date];");
                while (reader.Read()) 
                    result.Add(new ReminderInfo((string)reader["Jid"], (string)reader["Message"], (DateTime)reader["Date"]));
            }

            if (result.Count > 0)
                _nextReminder = result.Min(x => x.Date);
            else
                _nextReminder = null;

            return result;
        }

    }
}
