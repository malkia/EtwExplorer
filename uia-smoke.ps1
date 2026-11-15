param([string]$Exe = ".\EtwExplorer\bin\Debug\net11.0\EtwExplorer.exe")
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type @"
using System; using System.Collections.Generic; using System.Runtime.InteropServices; using System.Text;
public static class Win {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc p, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint code, uint type);
  public const int Up = 0x26, Down = 0x28;
  public static List<IntPtr> Find(uint pid) {
	var r = new List<IntPtr>();
	EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) r.Add(h); return true; }, IntPtr.Zero);
	return r;
  }
  public static string Title(IntPtr h) { var sb = new StringBuilder(512); GetWindowText(h, sb, 512); return sb.ToString(); }
  // Arrow keys are "extended" keys (bit 24); the key-up message also sets the previous-state and transition bits (30, 31)
  public static void Press(IntPtr h, int vk) {
	int down = 1 | ((int)MapVirtualKey((uint)vk, 0) << 16) | (1 << 24);
	PostMessage(h, 0x0100, (IntPtr)vk, (IntPtr)down);
	PostMessage(h, 0x0101, (IntPtr)vk, (IntPtr)(down | unchecked((int)0xC0000000)));
  }
}
"@
$A = [System.Windows.Automation.AutomationElement]
$C = [System.Windows.Automation.ControlType]
$P = [System.Windows.Automation.AutomationElement]
function Cond($type) { New-Object System.Windows.Automation.PropertyCondition($P::ControlTypeProperty, $type) }
function Wait-Windows($procId, $count, $sec = 20) {
  $t = [DateTime]::Now
  while (([DateTime]::Now - $t).TotalSeconds -lt $sec) { $w = [Win]::Find($procId); if ($w.Count -ge $count) { return $w }; Start-Sleep -Milliseconds 300 }
  throw "Timed out waiting for $count window(s)"
}
# Avalonia builds its UIA tree lazily, so poll until the probe returns something instead of assuming the element is there
function Wait-For($what, [scriptblock]$probe, $sec = 15) {
  $deadline = [DateTime]::Now.AddSeconds($sec)
  $lastError = $null
  do {
	try { $found = & $probe; if ($found) { return $found } } catch { $lastError = $_ }
	Start-Sleep -Milliseconds 200
  } while ([DateTime]::Now -lt $deadline)
  $detail = if ($lastError) { ": $lastError" }
  throw "Timed out waiting for $what$detail"
}
# Avalonia lists DataGrid rows in recycling order (not top to bottom) and creates only the visible ones. The expanded
# details of the selected Events row hold a nested grid, so take direct children only
function Get-Rows($grid) {
  @($grid.FindAll([System.Windows.Automation.TreeScope]::Children, (Cond $C::DataItem)) | Sort-Object { $_.Current.BoundingRectangle.Y })
}
function Get-RowText($row) {
  @($row.FindAll([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::Text)) | ForEach-Object { $_.Current.Name })
}
function Find-RowIndex($rows, $text) {
  for ($i = 0; $i -lt $rows.Count; $i++) { if ((Get-RowText $rows[$i]) -contains $text) { return $i } }
  -1
}
$proc = Start-Process $Exe -PassThru
try {
  $hw = Wait-Windows $proc.Id 1
  $mainH = $hw[0]
  $main = $A::FromHandle($mainH)
  "Main window: '$([Win]::Title($mainH))'"
  # Avalonia buttons have no inner Text child; the button itself carries the caption
  $open = Wait-For 'Open Provider button' { $main.FindAll([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::Button)) | Where-Object { $_.Current.Name -eq 'Open Provider...' } | Select-Object -First 1 }
  $open.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
  $hw = Wait-Windows $proc.Id 2
  $dlgH = $hw | Where-Object { $_ -ne $mainH } | Select-Object -First 1
  $dlg = $A::FromHandle($dlgH)
  "Dialog: '$([Win]::Title($dlgH))'"
  $edit = Wait-For 'dialog search box' { $dlg.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::Edit)) }
  $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Kernel-Process')
  Start-Sleep -Milliseconds 800
  $grid = Wait-For 'provider grid' { $dlg.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::DataGrid)) }
  # the filter also matches Microsoft-Windows-Kernel-Processor-Power, so look for the exact name
  $provider = 'Microsoft-Windows-Kernel-Process'
  $rows = @(Wait-For "provider row '$provider'" { $r = Get-Rows $grid; if ((Find-RowIndex $r $provider) -ge 0) { $r } })
  "Filtered rows: $($rows.Count)"
  $index = Find-RowIndex $rows $provider
  # Avalonia DataGrid rows have no SelectionItem pattern, so select with arrow keys posted to the dialog:
  # Up as many times as there are rows to get to the first one, then Down to the wanted one
  $grid.SetFocus()
  Start-Sleep -Milliseconds 300
  for ($i = 0; $i -lt $rows.Count; $i++) { [Win]::Press($dlgH, [Win]::Up) }
  for ($i = 0; $i -lt $index; $i++) { [Win]::Press($dlgH, [Win]::Down) }
  Start-Sleep -Milliseconds 500
  $ok = Wait-For 'OK button' { $dlg.FindAll([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::Button)) | Where-Object { $_.Current.Name -eq 'OK' } | Select-Object -First 1 }
  $ok.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
  # OK does nothing when no provider is selected, so the dialog staying open means the selection failed
  $null = Wait-For 'dialog to close (was a provider selected?)' { if (([Win]::Find($proc.Id)).Count -le 1) { $true } }
  $null = Wait-For 'tabs' { $main.FindAll([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::TabItem)) } 30
  Start-Sleep -Milliseconds 800
  $tabs = $main.FindAll([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::TabItem))
  "Tabs: $($tabs.Count) -> $(($tabs | ForEach-Object { $_.Current.Name }) -join ', ')"
  $texts = $main.FindAll([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::Text)) | ForEach-Object { $_.Current.Name }
  "Status has provider: $([bool]($texts -contains $provider))"
  if ($texts -notcontains $provider) { throw "Provider '$provider' is not shown in the main window" }
  "Title: '$([Win]::Title($mainH))'"
  # exercise the Events tab filter box and the Strings tab
  foreach ($t in $tabs) { if ($t.Current.Name -eq 'Events') { $t.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() } }
  Start-Sleep -Milliseconds 800
  $edit = Wait-For 'Events search box' { $main.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::Edit)) }
  $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('processstart')
  Start-Sleep -Milliseconds 800
  $grid = Wait-For 'Events grid' { $main.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::DataGrid)) }
  "Events filtered by 'processstart': $((Get-Rows $grid).Count) rows"
  foreach ($t in $tabs) { if ($t.Current.Name -eq 'Strings') { $t.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() } }
  Start-Sleep -Milliseconds 800
  $grid = Wait-For 'Strings grid' { $main.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (Cond $C::DataGrid)) }
  "Strings rows (visible): $((Get-Rows $grid).Count)"
  "Process alive: $(-not $proc.HasExited)"
}
finally {
  if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}

