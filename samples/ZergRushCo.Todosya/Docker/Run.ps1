Start-Process -Wait -NoNewWindow -PassThru 'C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe' @('-verb:sync', "-source:package='C:\Temp\Zerg.zip'", '-dest:auto', "-setParam:name='IIS Web Application Name',value='Default Web Site/'")
