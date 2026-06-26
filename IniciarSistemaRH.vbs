Dim shell
Set shell = CreateObject("WScript.Shell")

Dim pasta
pasta = CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName)

' Iniciar API
shell.Run "cmd /k cd /d """ & pasta & "\SistemaFerias.Api"" && dotnet run", 1, False

' Aguardar API inicializar
WScript.Sleep 5000

' Iniciar Worker
shell.Run "cmd /k cd /d """ & pasta & "\SistemaFerias.Worker"" && dotnet run", 1, False

' Aguardar Worker inicializar
WScript.Sleep 3000

' Iniciar Frontend
shell.Run "cmd /k cd /d """ & pasta & "\SistemaFerias.Frontend"" && npm run dev", 1, False

Set shell = Nothing