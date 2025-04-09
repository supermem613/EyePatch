@echo off
dotnet publish
copy .\bin\Release\net8.0\win-x64\publish\*.* "%onedrive%\bin"