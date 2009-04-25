using System;
using System.ServiceProcess;

namespace Saxx.Carmine {
    class Program {

        static void Main(string[] args) {
            try {
                if (args.Length > 0 && args[0].Equals("service", StringComparison.CurrentCultureIgnoreCase)) {
                    ServiceBase.Run(new ServiceBase[] { new Service() });
                }
                else {
                    Logger.Log(LogType.Info, "Starting in console mode");

                    var bot = new Bot();
                    bot.Connect(Settings.Get("user", "Carmine"), Settings.Get("server", "jabber.org"), Settings.Get("password", "asdf"));

                    Console.WriteLine("Type \"quit\" to end.");
                    while (!Console.ReadLine().Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                        Console.WriteLine("Type \"quit\" to end.");

                    Logger.Log(LogType.Info, "Quitting from console mode");
                    bot.Disconnect();
                }
            }
            catch (Exception ex) {
                Logger.Log(LogType.Fatal, "An exception was thrown: ", ex);
                throw ex;
            }
        }

    }
}
