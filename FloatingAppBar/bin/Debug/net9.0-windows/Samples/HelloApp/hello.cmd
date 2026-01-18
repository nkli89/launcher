@echo off
set "OUT=%TEMP%\floatingappbar-hello.txt"
echo Hello from FloatingAppBar! > "%OUT%"
notepad "%OUT%"
