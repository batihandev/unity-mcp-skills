[CmdletBinding()]
param(
    [ValidateSet('Inspect', 'Cancel', 'Close')][string]$Action = 'Inspect',
    [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$ExpectedProcessId,
    [Parameter(Mandatory)][string]$ExpectedStart,
    [Parameter(Mandatory)][string]$ExpectedProject,
    [Parameter(Mandatory)][long]$DeadlineUtcTicks
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Result {
    param([string]$State, [string]$Reason, [hashtable]$Details = @{})
    $result = [ordered]@{ supported = $true; state = $State; reason = $Reason }
    foreach ($key in $Details.Keys) { $result[$key] = $Details[$key] }
    $result | ConvertTo-Json -Compress
}

function Initialize-NativeApi {
    if ('UnitySessionModal.Native' -as [type]) { return }
    Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
namespace UnitySessionModal {
  public static class Native {
    public delegate bool EnumWindowsProc(IntPtr handle, IntPtr state);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr state);
    [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr state);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr handle);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr handle, StringBuilder text, int maxCount);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextLength(IntPtr handle);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr handle, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] public static extern IntPtr SendMessageTimeout(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);
  }
}
'@
}

function Test-ExpectedEditor {
    $process = Get-Process -Id $ExpectedProcessId -ErrorAction SilentlyContinue
    if ($null -eq $process -or $process.ProcessName -ine 'Unity') { return $false }
    if ($process.StartTime.ToUniversalTime().Ticks.ToString() -cne $ExpectedStart) { return $false }
    $cim = Get-CimInstance Win32_Process -Filter "ProcessId=$ExpectedProcessId" -ErrorAction SilentlyContinue
    if ($null -eq $cim -or [string]::IsNullOrWhiteSpace($cim.CommandLine)) { return $false }
    $match = [regex]::Match($cim.CommandLine, '(?i)(?:^|\s)-projectPath\s+(?:"([^"]+)"|(\S+))')
    if (-not $match.Success) { return $false }
    $actual = if ($match.Groups[1].Success) { $match.Groups[1].Value } else { $match.Groups[2].Value }
    $actual = [IO.Path]::GetFullPath($actual).TrimEnd('\')
    $expected = [IO.Path]::GetFullPath($ExpectedProject).TrimEnd('\')
    return [string]::Equals($actual, $expected, [StringComparison]::OrdinalIgnoreCase)
}

function Get-Dialogs {
    Initialize-NativeApi
    $dialogs = [Collections.Generic.List[object]]::new()
    $callback = [UnitySessionModal.Native+EnumWindowsProc]{
        param([IntPtr]$Handle, [IntPtr]$Unused)
        [uint32]$ownerPid = 0
        [void][UnitySessionModal.Native]::GetWindowThreadProcessId($Handle, [ref]$ownerPid)
        if ($ownerPid -ne $ExpectedProcessId -or -not [UnitySessionModal.Native]::IsWindowVisible($Handle)) { return $true }
        $className = [Text.StringBuilder]::new(256)
        [void][UnitySessionModal.Native]::GetClassName($Handle, $className, $className.Capacity)
        if ($className.ToString() -ne '#32770') { return $true }
        $title = [Text.StringBuilder]::new(([UnitySessionModal.Native]::GetWindowTextLength($Handle) + 1))
        [void][UnitySessionModal.Native]::GetWindowText($Handle, $title, $title.Capacity)
        $dialogs.Add([pscustomobject]@{ Handle = $Handle; Title = $title.ToString() })
        return $true
    }
    [void][UnitySessionModal.Native]::EnumWindows($callback, [IntPtr]::Zero)
    return @($dialogs)
}

function Get-CancelButtons {
    param([Parameter(Mandatory)][IntPtr]$Dialog)
    Initialize-NativeApi
    $buttons = [Collections.Generic.List[IntPtr]]::new()
    $callback = [UnitySessionModal.Native+EnumWindowsProc]{
        param([IntPtr]$Handle, [IntPtr]$Unused)
        $className = [Text.StringBuilder]::new(256)
        [void][UnitySessionModal.Native]::GetClassName($Handle, $className, $className.Capacity)
        $title = [Text.StringBuilder]::new(([UnitySessionModal.Native]::GetWindowTextLength($Handle) + 1))
        [void][UnitySessionModal.Native]::GetWindowText($Handle, $title, $title.Capacity)
        if ($className.ToString() -ceq 'Button' -and $title.ToString() -ceq 'Cancel') { $buttons.Add($Handle) }
        return $true
    }
    [void][UnitySessionModal.Native]::EnumChildWindows($Dialog, $callback, [IntPtr]::Zero)
    return @($buttons)
}

function Get-SceneDialog {
    $dialogs = @(Get-Dialogs)
    if ($dialogs.Count -eq 0) { return @{ Ok = $false; Reason = 'modal-absent' } }
    if ($dialogs.Count -ne 1) { return @{ Ok = $false; Reason = 'multiple-dialogs' } }
    if ($dialogs[0].Title -cne 'Scene(s) Have Been Modified') {
        return @{ Ok = $false; Reason = 'other-dialog'; Title = $dialogs[0].Title }
    }
    return @{ Ok = $true; Reason = 'scene-modal-found'; Handle = $dialogs[0].Handle }
}

function Invoke-Recovery {
    $deadline = [DateTime]::new($DeadlineUtcTicks, [DateTimeKind]::Utc)
    if ([DateTime]::UtcNow -ge $deadline) { return Write-Result 'refused' 'deadline-expired' }
    if (-not (Test-ExpectedEditor)) { return Write-Result 'refused' 'process-identity-changed' }
    if ($Action -eq 'Close') {
        if (-not (Test-ExpectedEditor)) { return Write-Result 'refused' 'process-identity-changed' }
        $process = Get-Process -Id $ExpectedProcessId -ErrorAction SilentlyContinue
        if (-not (Test-ExpectedEditor)) { return Write-Result 'refused' 'process-identity-changed' }
        if ([DateTime]::UtcNow -ge $deadline) { return Write-Result 'refused' 'deadline-expired' }
        if (-not $process.CloseMainWindow()) { return Write-Result 'refused' 'close-main-window-rejected' }
        return Write-Result 'requested' 'close-main-window' @{ method = 'CloseMainWindow' }
    }
    if ([DateTime]::UtcNow -ge $deadline) { return Write-Result 'refused' 'deadline-expired' }
    $dialog = Get-SceneDialog
    if ($dialog.Ok) {
        if ($Action -eq 'Inspect') { return Write-Result 'inspected' 'scene-modal-found' @{ title = 'Scene(s) Have Been Modified' } }
        if (-not (Test-ExpectedEditor)) { return Write-Result 'refused' 'process-identity-changed' }
        $unchanged = Get-SceneDialog
        if (-not $unchanged.Ok -or $unchanged.Handle -ne $dialog.Handle) { return Write-Result 'refused' 'dialog-changed-before-cancel' }
        $buttons = @(Get-CancelButtons -Dialog $dialog.Handle)
        if ($buttons.Count -ne 1) { return Write-Result 'refused' 'cancel-button-absent-or-ambiguous' }
        if (-not (Test-ExpectedEditor)) { return Write-Result 'refused' 'process-identity-changed' }
        $remainingMs = [int][Math]::Floor(($deadline - [DateTime]::UtcNow).TotalMilliseconds)
        if ($remainingMs -le 0) { return Write-Result 'refused' 'deadline-expired' }
        $nativeResult = [IntPtr]::Zero
        $sent = [UnitySessionModal.Native]::SendMessageTimeout($dialog.Handle, 0x0111, [IntPtr]2, $buttons[0], 0x0002, [uint32][Math]::Min(1000, $remainingMs), [ref]$nativeResult)
        if ($sent -eq [IntPtr]::Zero) { return Write-Result 'refused' 'cancel-command-timeout-or-failure' }
        do {
            $remainingMs = [int][Math]::Floor(($deadline - [DateTime]::UtcNow).TotalMilliseconds)
            if ($remainingMs -le 0) { return Write-Result 'refused' 'modal-still-present-after-cancel' }
            Start-Sleep -Milliseconds ([Math]::Min(100, $remainingMs))
            $after = Get-SceneDialog
            if ($after.Reason -eq 'modal-absent') { return Write-Result 'cancelled' 'modal-dismissed' }
            if (-not $after.Ok) { return Write-Result 'refused' 'dialog-changed-after-cancel' }
        } while ([DateTime]::UtcNow -lt $deadline)
        return Write-Result 'refused' 'modal-still-present-after-cancel'
    }
    if ($dialog.Reason -eq 'other-dialog') { return Write-Result 'inspected' 'other-dialog' @{ title = $dialog.Title } }
    if ($dialog.Reason -ne 'modal-absent') { return Write-Result 'refused' $dialog.Reason }
    if ($Action -eq 'Inspect' -and $dialog.Reason -eq 'modal-absent') { return Write-Result 'inspected' 'modal-absent' }
    return Write-Result 'refused' 'modal-absent-before-cancel'
}

Invoke-Recovery
