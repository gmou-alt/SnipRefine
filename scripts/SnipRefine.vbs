Option Explicit
' SnipRefine.vbs - silent launcher (no console window, no compilation).
' Double-click this file to start SnipRefine straight from the source tree.

Dim fso, sh, dir, cmd
Set fso = CreateObject("Scripting.FileSystemObject")
Set sh = CreateObject("WScript.Shell")
dir = fso.GetParentFolderName(WScript.ScriptFullName)

cmd = "powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & dir & "\SnipRefine.ps1"""
sh.Run cmd, 0, False
