#ESP Auto Livery Importer
##Automated livery importer for Microsoft ESP based simulators

This tool is designed to automatically import liveries for ESP basec simulators, such as Lockheed Martin Prepar3d, or Microsoft Flight Simulator X.

To use this tool, just run it via the command line, and follow the on-screen instructions.

Choose your import directory (The one containing your livery folder). The program will check for errors and conflicts.
Choose your output directory (The SimObjects\Something\ThePlaneYouWantToImportTo). More conflict checks will be done.

When you start importing the livery, the program will copy the file, and make the entry into the `aircraft.cfg` file for you. It also backs up the file beforehand in case of any issues.
