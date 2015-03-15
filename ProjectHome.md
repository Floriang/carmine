Carmine is a small and light-weight Jabber-Bot written in C# / .NET 3.5. It can be run either as a command-line application or as a windows service.

# Live Demo available #
Just add **Carmine@jabber.org** to your Jabber client and play around with the demo running there. Or download the latest version and run it yourself.

# Features #
  * **Twitter Support:** Update your status and receive new statuses of your friends, through your IM-client ([more](PluginTwitter.md)).
  * **Google Calendar Integration:** Add new events and see all your upcoming events ([more](PluginGcal.md)).
  * **Quick reminder:** Never again forget your pizza in the oven ([more](PluginReminder.md)).

  * Works with all Jabber servers and automatically registers itself on the server if necessary (using jabber-net: http://code.google.com/p/jabber-net)
  * Supports plugins in compiled form (assemblies), but also plain C# or VB.NET source files
  * Automatically recognizes new plugins and changes behavior accordingly on the fly. No need to restart at all.
  * Plugins can use SQLite out of the box, through an especially easy interface (using System.Data.SQLite: http://sqlite.phxsoftware.com)
  * Extensive logging features (using log4net: http://logging.apache.org/log4net)
  * The main focus is on keeping the code for plugins as easy and short as possible

The actual work is done by plugins. Carmine already comes with a few useful, populiar plugins, but it is extremely easy to add new, custom plugins. Those are either compiled assemblies, or just plain C# or VB.NET source files dropped into a special directory.

# More Information #
  * [How to setup Carmine to run as a windows service](RunAsWindowsService.md)
  * [How to write a simple plugin](SimplePlugin.md)

# Available Plugins #
  * [Ping](PluginPing.md)
  * [Twitter](PluginTwitter.md)
  * [Google Calendar](PluginGcal.md)
  * [Reminder](PluginReminder.md)