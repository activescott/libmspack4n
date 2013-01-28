@echo off

echo !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! 
echo This builds the libmspack into mspack.dll on
echo Windows with Microsoft compiler.
echo After compilation find the library in the 
echo directory mspack 
echo !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! 


echo #define inline __inline > config.h
echo #define HAVE_STRING_H 1 >> config.h
echo #define HAVE_LIMITS_H 1 >> config.h
echo #define HAVE_MEMCMP 1 >> config.h

del /Q .\mspack\mspack.dll
del /Q .\mspack\mspack.lib
del /Q .\mspack\*.obj

cd mspack
REM debug.c is only for copy/paste code and fails to compile. The script exits on a failed compile so I'm renaming it:
IF EXIST debug.c ren debug.c debug.c.ignore

cl /O2 -I.. -I. /DHAVE_CONFIG_H /c *.c
if %errorlevel% neq 0 goto :ERROR_COMP
link *.obj /DLL /DEF:mspack.def /IMPLIB:mspack.lib /OUT:mspack.dll

cd ..

dir .\mspack\mspack.dll
dir .\mspack\mspack.lib

goto :EOF
:ERROR_COMP
cd ..
echo !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! 
echo ERROR COMPILING
echo !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! !!! 
:EOF