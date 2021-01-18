# OSDP.Net

[![Build Status](https://dev.azure.com/jonathanhorvath/OSDP.Net/_apis/build/status/bytedreamer.OSDP.Net?branchName=develop)](https://dev.azure.com/jonathanhorvath/OSDP.Net/_build/latest?definitionId=1&branchName=develop)
[![NuGet](https://img.shields.io/nuget/v/OSDP.Net.svg?style=flat)](https://www.nuget.org/packages/OSDP.Net/)

OSDP.Net is a .NET framework implementation of the Open Supervised Device Protocol (OSDP). This protocol has been adopted by the Security Industry Association (SIA) to standardize access control hardware communication. Further information can be found at [SIA OSDP Homepage](https://www.securityindustry.org/industry-standards/open-supervised-device-protocol/).

## Getting Started

The OSDP.Net library provides a Nuget package to quickly add OSDP capablitity to a .NET Framework or Core project. You can install it using the NuGet Package Console window:

```
PM> Install-Package OSDP.Net
``` 

A control panel can be created and started with a few lines. Be sure to register events before start the connection.

```csharp
var panel = new ControlPanel();
panel.ConnectionStatusChanged += (sender, eventArgs) =>
{
    // NOTE: Avoid blocking the processing events
    Task.Run(async () =>
    {
        // Handle connection change event
    });
};
Guid connectionId = panel.StartConnection(new SerialPortOsdpConnection(portName, baudRate));
```

Once the connection has started, add Peripheral Devices (PD).

```csharp
panel.AddDevice(connectionId, address, useCrc, useSecureChannel);
```

## Test Console

There is a package of the test console application for all the major platforms available for download. It has all the required assemblies included to run as a self containsed executable. The latest version of the package can be found at [https://www.z-bitco.com/downloads/OSDPTestConsole.zip](https://www.z-bitco.com/downloads/OSDPTestConsole.zip)

NOTE: First determine the COM port identifier of the 485 bus connected to the computer. This will need to be entered when starting the connection. Be sure to save configuration before exiting.

## Documentation 
* [Supported Commands and Replies](docs/supported_commands.md)

## Contributing

The current goal is to properly support all the commands and replies outlined the OSDP v2.2 standard. The document that outlines the specific of the standard can be found on the [SIA website](https://mysia.securityindustry.org/ProductCatalog/Product.aspx?ID=16773). DM me on Twitter [![Follow NUnit](https://img.shields.io/twitter/follow/bytedreamer.svg?style=social)](https://twitter.com/bytedreamer) if you are interesting in helping.
