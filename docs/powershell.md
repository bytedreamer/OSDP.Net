# PowerShell Core Support

You don't have to be a developer to make use of the OSDP.Net library. Commands can be scripted using PowerShell Core. 

**OSDP.ps1**
```shell
# Install Nuget packages and assemblies if needed
if (([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object FullName -like '*Microsoft.Extensions.Logging.Abstractions*') -eq $null)
{
  Install-Package -Name Microsoft.Extensions.Logging.Abstractions -ProviderName NuGet -Scope CurrentUser -RequiredVersion 5.0.0 -SkipDependencies -Destination . -Force
  Add-Type -Path ./Microsoft.Extensions.Logging.Abstractions.5.0.0/lib/netstandard2.0/Microsoft.Extensions.Logging.Abstractions.dll
}
if (([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object FullName -like '*OSDP.Net*') -eq $null)
{
  Install-Package -Name OSDP.Net -ProviderName NuGet -Scope CurrentUser -RequiredVersion 2.0.14 -SkipDependencies -Destination . -Force
  Add-Type -Path ./OSDP.Net.2.0.14/lib/netstandard2.0/OSDP.Net.dll
}

# Settings
$serialPortName = "COM1"
$serialPortSpeed = 9600
$deviceAddress = 0
$secureChannel = $true

# Setup serial connection
$conn = [OSDP.Net.Connections.SerialPortOsdpConnection]::new($serialPortName, $serialPortSpeed)
$panel = [OSDP.Net.ControlPanel]::new()

# Register device connection status events
$Action = {
  Write-Host ("Address " +  $EventArgs.Address + " is connection status is " + $EventArgs.IsConnected)
}
Register-ObjectEvent -InputObject $panel -EventName "ConnectionStatusChanged" -Action $Action

# Start the connection
$id = $panel.StartConnection($conn)

# Add a device to the connection
$panel.AddDevice($id, $deviceAddress, $true, $secureChannel)

# Handle user input
Write-Host 'Press L key to flash LED';
Write-Host 'Press X key to exit...';
while($true)
{
  if ([console]::KeyAvailable)
  {
    $keyInfo = [Console]::ReadKey($false)
    
    switch ( $keyInfo.key)
    {
      L 
      { 
        $panel.ReaderLedControl($id, 1, [OSDP.Net.Model.CommandData.ReaderLedControls]::new(
          [OSDP.Net.Model.CommandData.ReaderLedControl[]]@([OSDP.Net.Model.CommandData.ReaderLedControl]::new(
            0, 
            0,
            [OSDP.Net.Model.CommandData.TemporaryReaderControlCode]::SetTemporaryAndStartTimer,
            10,
            10,
            [OSDP.Net.Model.CommandData.LedColor]::Red,
            [OSDP.Net.Model.CommandData.LedColor]::Green,
            100,
            [OSDP.Net.Model.CommandData.PermanentReaderControlCode]::Nop,
            1,
            0,
            [OSDP.Net.Model.CommandData.LedColor]::Black,
            [OSDP.Net.Model.CommandData.LedColor]::Black))))
      }
      X 
      { 
        $panel.Shutdown()
        exit
      }
      Default
      {
        Write-Host 'Press L key to flash LED';
        Write-Host 'Press X key to exit...';
      }
    }
  } 
  else
  {
    sleep 1
  }    
}
```

## Setup and Running

The sample code file above has been tested on Windows 10. Here are the steps to get it to run.

1) Download and install the latest version of [.NET Core Runtime or SDK](https://dotnet.microsoft.com/en-us/download)
2) Download and install the latest version of [PowerShell Core](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows)
3) Create a directory and create a OSDP.ps1 file with the above code
   * Update the settings to the correct values
4) Open command prompt and run the command ```pwsh``` to enter the shell
5) Change to the directory created in step 3
6) Run the script ```./OSDP.ps1```
    * The script requires an Internet connection to download the dependent assemblies

## Documentation of Commands

The documentation of the commands is the same for developers. It can be found [here](supported_commands.md).