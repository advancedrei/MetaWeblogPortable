@echo off
echo Would you like to push the packages to NuGet when finished?
set /p choice="Enter y/n: "

del *.nupkg
@echo on
".nuget/nuget.exe" pack MetaWeblog.Portable.nuspec -symbols
if /i %choice% equ y (
    ".nuget/nuget.exe" push *.nupkg
)
echo Because we pushed the package and the symbols in together, the second error can be safely ignored.
pause