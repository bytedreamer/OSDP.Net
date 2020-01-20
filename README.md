# OSDP.Net #

[![Build Status](https://dev.azure.com/jonathanhorvath/OSDP.Net/_apis/build/status/bytedreamer.OSDP.Net?branchName=master)](https://dev.azure.com/jonathanhorvath/OSDP.Net/_build/latest?definitionId=1&branchName=master)

OSDP.Net is a .NET framework implementation of the Open Supervised Device Protocol(OSDP). This protocol has been adopted by the Security Industry Association(SIA) to standardize access control hardware communication. Further information can be found at [http://www.osdp-connect.com](http://www.osdp-connect.com).

The project is in early development and not ready for production usage. The short term goal is to get all the commands and replies defined in the library. The next step will be to package the library into a Nuget package. DM me on Twitter [![Follow NUnit](https://img.shields.io/twitter/follow/bytedreamer.svg?style=social)](https://twitter.com/bytedreamer) if you are interesting in helping.

There is a package of the test console application for all the major platforms available for download. It has all the required assemblies included to run as a self containsed executable. The latest version of the package can be found at [https://www.z-bitco.com/downloads/OSDPTestConsole.zip](https://www.z-bitco.com/downloads/OSDPTestConsole.zip)

NOTE: Determine the COM port identifier of the 485 bus connected to the computer. This will need to be entered when starting the connection. Be sure to save configuration before exiting.
