Remove-Item -Recurse -Force ./IfsSync2
dotnet publish -c Release
Move-Item -Force -Path ./IfsSync2/Release/* -Destination ./IfsSync2/
Move-Item -Force -Path ./IfsSync2/runtimes/win-x64/native/SQLite.Interop.dll -Destination ./IfsSync2/
Copy-Item -Force -Path ./Lib/ -Destination ./IfsSync2/ -Recurse
Remove-Item -Recurse -Force ./IfsSync2/Release
