param([ValidateSet('brightness','setbrightness','notifications')][string]$Action='brightness',[int]$Value=50,[string]$DisplayId='')
$ErrorActionPreference='Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
function Await($Operation,$Type) {
 $method=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {$_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'} | Select-Object -First 1
 $task=$method.MakeGenericMethod($Type).Invoke($null,@($Operation));if(-not $task.Wait(7000)){throw 'Windows did not respond in time.'};return $task.Result
}
function BrightnessMonitor($Class) {
 $items=@(Get-CimInstance -Namespace root/WMI -ClassName $Class)
 if($DisplayId){$items=@($items|Where-Object {$_.InstanceName.StartsWith($DisplayId,[StringComparison]::OrdinalIgnoreCase)})}
 if($items.Count -ne 1){throw 'No unambiguous WMI brightness control for the selected display.'}
 return $items[0]
}
try {switch($Action) {
 'brightness' {$b=BrightnessMonitor 'WmiMonitorBrightness';@{value=$b.CurrentBrightness}|ConvertTo-Json -Compress}
 'setbrightness' {if($Value -lt 0 -or $Value -gt 100){throw 'Brightness must be 0–100.'};$b=BrightnessMonitor 'WmiMonitorBrightnessMethods';$b|Invoke-CimMethod -MethodName WmiSetBrightness -Arguments @{Timeout=[uint32]1;Brightness=[byte]$Value}|Out-Null;'{"ok":true}'}
 'notifications' {
 Add-Type -AssemblyName System.Runtime.WindowsRuntime
 $type=[Windows.UI.Notifications.Management.UserNotificationListener,Windows.UI.Notifications,ContentType=WindowsRuntime];$listener=$type::Current
 $access=Await ($listener.RequestAccessAsync()) ([Windows.UI.Notifications.Management.UserNotificationListenerAccessStatus,Windows.UI.Notifications,ContentType=WindowsRuntime])
 if($access.ToString() -ne 'Allowed'){throw "Notification access: $access. This feature may require packaged app identity."}
 $notificationType=[Windows.UI.Notifications.UserNotification,Windows.UI.Notifications,ContentType=WindowsRuntime]
 $listType=[System.Collections.Generic.IReadOnlyList``1].MakeGenericType($notificationType)
 $kind=[Windows.UI.Notifications.NotificationKinds,Windows.UI.Notifications,ContentType=WindowsRuntime]
 $items=Await ($listener.GetNotificationsAsync($kind::Toast)) $listType
 $out=@(foreach($item in $items){$text=@($item.Notification.Visual.Bindings|ForEach-Object {$_.GetTextElements()}|ForEach-Object {$_.Text});@{id=($item.Id.ToString()+'@'+$item.CreationTime.UtcDateTime.Ticks);title=$item.AppInfo.DisplayInfo.DisplayName;detail=($text -join "`n")}})
 ConvertTo-Json -InputObject $out -Compress
 }
}}catch{@{error=$_.Exception.Message}|ConvertTo-Json -Compress;exit 1}
