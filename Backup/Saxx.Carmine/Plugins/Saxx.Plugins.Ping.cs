using System;
using Saxx.Carmine;

public class Ping : Plugin {

    public override void Message(string from, string message) {
        if (!string.IsNullOrEmpty(message) && message.Equals("ping", StringComparison.InvariantCultureIgnoreCase)) {           
            Log(LogType.Info, "Pinging back to " + from);
            SendMessage(from, "Hey " + from + ", it's " + DateTime.Now + ".");
        }
    }

    public override string ToString() {
        return "The Ping plugin";
    }

}