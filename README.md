# OSDP.Net #

[![Build Status](https://dev.azure.com/jonathanhorvath/OSDP.Net/_apis/build/status/bytedreamer.OSDP.Net?branchName=master)](https://dev.azure.com/jonathanhorvath/OSDP.Net/_build/latest?definitionId=1&branchName=master)

OSDP.Net is a .NET framework implementation of the Open Supervised Device Protocol(OSDP). This protocol has been adopted by the Security Industry Association(SIA) to standardize communication to access control hardware. Further information can be found at [http://www.osdp-connect.com](http://www.osdp-connect.com).

The project is in early development and not ready for production usage. DM me on Twitter [![Follow NUnit](https://img.shields.io/twitter/follow/bytedreamer.svg?style=social)](https://twitter.com/bytedreamer) if you are interesting in helping.

There is a pre-release build available for download for testing the library. The test console application is built for the MacOS platform. It can be run on other platforms with .NET Core 3.0 already installed by running the following command:
<pre><code>dotnet Console.dll</code></pre>
Determine the COM port identifier of the 485 bus connected to the computer. This will need to be entered when starting the connection. Be sure to save configuration if needed before exiting.
