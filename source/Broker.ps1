param([ValidateSet('media','notifications','brightness','setbrightness','bluetooth','radios','wifi-on','wifi-off','bluetooth-on','bluetooth-off')][string]$Action='media',[int]$Value=50)
$ErrorActionPreference='Stop'
function Await($Operation,$Type) {
 $method=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {$_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'} | Select-Object -First 1
 $task=$method.MakeGenericMethod($Type).Invoke($null,@($Operation)); if(-not $task.Wait(7000)){throw 'Windows did not respond in time.'}; return $task.Result
}
try {switch($Action) {
 'media' {
 Add-Type -AssemblyName System.Runtime.WindowsRuntime
 $type=[Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager,Windows.Media.Control,ContentType=WindowsRuntime]
 $manager=Await ($type::RequestAsync()) $type; $session=$manager.GetCurrentSession()
 if($null -eq $session){'{"available":false}';break}
 $props=Await ($session.TryGetMediaPropertiesAsync()) ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties,Windows.Media.Control,ContentType=WindowsRuntime]);$timeline=$session.GetTimelineProperties()
 @{available=$true;title=$props.Title;artist=$props.Artist;source=$session.SourceAppUserModelId;status=$session.GetPlaybackInfo().PlaybackStatus.ToString();position=$timeline.Position.TotalSeconds;duration=$timeline.EndTime.TotalSeconds}|ConvertTo-Json -Compress
 }
 'brightness' {$b=Get-CimInstance -Namespace root/WMI -ClassName WmiMonitorBrightness | Select-Object -First 1;if($null -eq $b){throw 'No supported built-in display.'};@{value=$b.CurrentBrightness}|ConvertTo-Json -Compress}
 'setbrightness' {Get-CimInstance -Namespace root/WMI -ClassName WmiMonitorBrightnessMethods | Invoke-CimMethod -MethodName WmiSetBrightness -Arguments @{Timeout=[uint32]1;Brightness=[byte]$Value}|Out-Null;'{"ok":true}'}
 'bluetooth' {
 Add-Type -AssemblyName System.Runtime.WindowsRuntime
 $type=[Windows.Devices.Bluetooth.BluetoothDevice,Windows.Devices.Bluetooth,ContentType=WindowsRuntime]
 $di=[Windows.Devices.Enumeration.DeviceInformation,Windows.Devices.Enumeration,ContentType=WindowsRuntime]
 $collection=[Windows.Devices.Enumeration.DeviceInformationCollection,Windows.Devices.Enumeration,ContentType=WindowsRuntime]
 $found=Await ($di::FindAllAsync($type::GetDeviceSelectorFromPairingState($true))) $collection
 $devices=@(foreach($item in $found){$device=$null;try{$device=Await ($type::FromIdAsync($item.Id)) $type;if($null -ne $device){@{FriendlyName=$device.Name;Status=$device.ConnectionStatus.ToString()}}}finally{if($null -ne $device){$device.Dispose()}}})
 ConvertTo-Json -InputObject $devices -Compress
 }
 'notifications' {
 Add-Type -AssemblyName System.Runtime.WindowsRuntime
 $type=[Windows.UI.Notifications.Management.UserNotificationListener,Windows.UI.Notifications,ContentType=WindowsRuntime]
 $listener=$type::Current
 $access=Await ($listener.RequestAccessAsync()) ([Windows.UI.Notifications.Management.UserNotificationListenerAccessStatus,Windows.UI.Notifications,ContentType=WindowsRuntime])
 if($access.ToString() -ne 'Allowed'){throw "Notification access: $access. This feature may require packaged app identity."}
 $notificationType=[Windows.UI.Notifications.UserNotification,Windows.UI.Notifications,ContentType=WindowsRuntime]
 $listType=[System.Collections.Generic.IReadOnlyList``1].MakeGenericType($notificationType)
 $kind=[Windows.UI.Notifications.NotificationKinds,Windows.UI.Notifications,ContentType=WindowsRuntime]
 $items=Await ($listener.GetNotificationsAsync($kind::Toast)) $listType
 $out=@(foreach($item in $items){$text=@($item.Notification.Visual.Bindings|ForEach-Object {$_.GetTextElements()}|ForEach-Object {$_.Text});@{id=$item.Id.ToString();title=$item.AppInfo.DisplayInfo.DisplayName;detail=($text -join "`n")}})
 ConvertTo-Json -InputObject $out -Compress
 }

 default {
 Add-Type -AssemblyName System.Runtime.WindowsRuntime
 $rType=[Windows.Devices.Radios.Radio,Windows.System.Devices,ContentType=WindowsRuntime];$listType=[System.Collections.Generic.IReadOnlyList``1].MakeGenericType($rType);$radios=Await ($rType::GetRadiosAsync()) $listType
 if($Action -eq 'radios'){@($radios|ForEach-Object {@{name=$_.Name;kind=$_.Kind.ToString();state=$_.State.ToString()}})|ConvertTo-Json -Compress}
 else {$kind=if($Action -like 'wifi-*'){'WiFi'}else{'Bluetooth'};$radio=$radios|Where-Object {$_.Kind.ToString() -eq $kind}|Select-Object -First 1;if(!$radio){throw "No $kind radio available."};$stateType=[Windows.Devices.Radios.RadioState,Windows.System.Devices,ContentType=WindowsRuntime];$newState=if($Action -like '*-on'){$stateType::On}else{$stateType::Off};$access=Await ($radio.SetStateAsync($newState)) ([Windows.Devices.Radios.RadioAccessStatus,Windows.System.Devices,ContentType=WindowsRuntime]);if($access.ToString() -ne 'Allowed'){throw "Windows denied radio access: $access. Use Windows Settings."};'{"ok":true}'}
 }
}}catch{@{error=$_.Exception.Message}|ConvertTo-Json -Compress;exit 1}
