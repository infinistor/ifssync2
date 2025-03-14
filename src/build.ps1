clear
Remove-Item -Recurse -Force ./IfsSync2
dotnet publish -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "빌드 실패: dotnet publish 명령이 실패했습니다."
    exit $LASTEXITCODE
}
Move-Item -Force -Path ./IfsSync2/Release/publish/* -Destination ./IfsSync2/
Move-Item -Force -Path ./IfsSync2/runtimes/win-x64/native/SQLite.Interop.dll -Destination ./IfsSync2/
Copy-Item -Force -Path ./Lib/ -Destination ./IfsSync2/ -Recurse
Remove-Item -Recurse -Force ./IfsSync2/Release
iscc "IfsSync2InnoBuild-x64.iss"