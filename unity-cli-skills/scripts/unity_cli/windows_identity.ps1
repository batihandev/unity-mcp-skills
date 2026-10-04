[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$ExpectedProcessId,
    [Parameter(Mandatory)][string]$ExpectedStart,
    [Parameter(Mandatory)][string]$ExpectedProject,
    [Parameter(Mandatory)][long]$DeadlineUtcTicks
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$process = $null
$inputReader = $null
try {
    $process = [Diagnostics.Process]::GetProcessById($ExpectedProcessId)
    # Retain the native handle before reading start time so PID reuse cannot retarget checks.
    $handle = $process.Handle
    if ($handle -eq [IntPtr]::Zero -or $process.ProcessName -ine 'Unity' -or
        $process.StartTime.ToUniversalTime().Ticks.ToString() -cne $ExpectedStart -or $process.HasExited) {
        exit 2
    }
    $rows = @(Get-CimInstance Win32_Process -Filter "ProcessId=$ExpectedProcessId" -ErrorAction Stop)
    if ($rows.Count -ne 1 -or $process.HasExited -or
        $process.StartTime.ToUniversalTime().Ticks.ToString() -cne $ExpectedStart) { exit 2 }
    $commandLine = $rows[0].CommandLine
    $executable = $rows[0].ExecutablePath
    if ([string]::IsNullOrWhiteSpace($commandLine) -or [string]::IsNullOrWhiteSpace($executable)) { exit 2 }
    $inputReader = [IO.StreamReader]::new([Console]::OpenStandardInput(), [Text.Encoding]::UTF8)
    $sequence = 0
    $observer = $null
    while ($true) {
        $remainingTicks = $DeadlineUtcTicks - [DateTime]::UtcNow.Ticks
        if ($remainingTicks -le 0) { exit 3 }
        $read = $inputReader.ReadLineAsync()
        $milliseconds = [int][Math]::Min([int]::MaxValue, [Math]::Ceiling($remainingTicks / 10000.0))
        if (-not $read.Wait($milliseconds)) { exit 3 }
        $line = $read.Result
        if ($null -eq $line -or $line.Length -gt 4096) { exit 4 }
        $request = $line | ConvertFrom-Json
        $sequence += 1
        if ($sequence -eq 1) { $observer = $request.observer }
        if ($request.sequence -ne $sequence -or $request.pid -ne $ExpectedProcessId -or
            $request.startedAt -cne $ExpectedStart -or $request.project -cne $ExpectedProject -or
            $request.observer -cne $observer -or [string]::IsNullOrWhiteSpace($observer)) { exit 4 }
        if ([DateTime]::UtcNow.Ticks -ge $DeadlineUtcTicks) { exit 3 }
        $alive = -not $process.HasExited
        $response = [ordered]@{
            observer = $observer; sequence = $sequence; pid = $ExpectedProcessId;
            startedAt = $ExpectedStart; project = $ExpectedProject; alive = $alive
        }
        if ($sequence -eq 1) {
            $response['commandLine'] = $commandLine
            $response['executable'] = $executable
        }
        $response | ConvertTo-Json -Compress | ForEach-Object { [Console]::WriteLine($_) }
        if (-not $alive) { exit 2 }
    }
} catch {
    exit 5
} finally {
    if ($null -ne $process) { $process.Dispose() }
    if ($null -ne $inputReader) { $inputReader.Dispose() }
}
