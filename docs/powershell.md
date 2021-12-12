# PowerShell Support

The OSDP.Net library can called from PowerShell core. The sample below has been tested on Windows using version 7.2 of PowerShell. 

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
$serialPortName = "COM11"
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

