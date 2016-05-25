@setlocal 
@set /P TEMPVER= Enter the version number to build for:

msbuild.exe .\.build\libmspack4n.msbuild /p:TheVersion=%TEMPVER%
