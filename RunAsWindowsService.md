Per default, Carmine is configured to run as a command-line application, but it is also able to run as a Windows service. Use the "service" command-line switch to start Carmine in service-mode.

To install any application as a Windows service, you can use the "sc" command-line tool that comes with the Windows Resource Kit. You probably have it installed already, but see http://support.microsoft.com/kb/251192 for more information.


The install a new service open your command-line and run:

`sc create Carmine Binpath= "[YOUR PATH]\Saxx.Carmine.exe service" DisplayName= "Carmine"`


That adds a Carmine entry to the service registry. To start the service, just type:

`net start Carmine`

And to stop it:

`net stop Carmine`


Of course you can use Administrative Tools > Services to start and stop the service or to configure the startup type (for now it is set to "Manual"). Finally, if you want to remove the Carmine service again, type:

`sc delete Carmine`