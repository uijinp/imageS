$Server = "121.78.128.204"
$Port = 9000

Write-Host "Testing connection to $Server : $Port ..."
try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $connect = $tcp.BeginConnect($Server, $Port, $null, $null)
    $wait = $connect.AsyncWaitHandle.WaitOne(3000, $false)
    
    if (!$wait) {
        Write-Error "Connection Timeout: Unable to reach $Server on port $Port within 3 seconds."
        $tcp.Close()
    } else {
        $tcp.EndConnect($connect)
        Write-Host "SUCCESS: Connection established!" -ForegroundColor Green
        $tcp.Close()
    }
} catch {
    Write-Error "Connection Failed: $_"
}

Write-Host "`nPress any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
