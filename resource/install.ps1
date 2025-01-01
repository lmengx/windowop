$tempFolderName = [guid]::NewGuid().ToString()
$tempFolderPath = Join-Path -Path $env:TEMP -ChildPath $tempFolderName
$appDataPath = $env:APPDATA
New-Item -Path $tempFolderPath -ItemType Directory
$result = dotnet --list-runtimes | Select-String "9.0"
if (!$result) {
    $installProgramPath = Join-Path -Path $tempFolderPath -ChildPath ".NET_9.0_RUNTIME.exe"
    if ([Environment]::Is64BitOperatingSystem) {$downloadurl = "https://download.visualstudio.microsoft.com/download/pr/685792b6-4827-4dca-a971-bce5d7905170/1bf61b02151bc56e763dc711e45f0e1e/windowsdesktop-runtime-9.0.0-win-x64.exe"}
    else {$downloadurl = "https://download.visualstudio.microsoft.com/download/pr/8dfbde7b-c316-418d-934a-d3246253f342/69c6a35b77a4f01b95588e1df2bddf9a/windowsdesktop-runtime-9.0.0-win-x86.exe"}
$webClient = New-Object System.Net.WebClient
$webClient.DownloadFileAsync($downloadurl , $installProgramPath)
while ($webClient.IsBusy) {Start-Sleep -Milliseconds 100}
Start-Process $installProgramPath -Wait}
$downloadurl = "https://gitee.com/lmx12330/window-op/raw/master/resource/windowOP.zip"
$zipPath = Join-Path -Path $tempFolderPath -ChildPath "windowOP.zip"
$targetPath = Join-Path -Path $appDataPath -ChildPath "windowOP"
$exePath = Join-Path -Path $targetPath -ChildPath "windowOP.exe"
$webClient = New-Object System.Net.WebClient
$webClient.DownloadFileAsync($downloadurl , $zipPath)
while ($webClient.IsBusy) {Start-Sleep -Milliseconds 100}
Expand-Archive -Path $zipPath -DestinationPath $targetPath -Force
Start-Process $exePath
Remove-Item -Path $tempFolderPath -Recurse -Force
pause