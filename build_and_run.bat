set SOLUTION_NAME=FlightMobileServer
set EXE_NAME=FlightMobileWeb

REM Load Visual Studio Build Tools into the environment. 
REM You may change the location of the Visual Studio installation or edition
REM The grader will use Visual Studio 2019. 
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat"

REM This will regenerate the assets file needed 
REM for your .NET core packages
msbuild %SOLUTION_NAME%.sln -t:restore

REM Clean and Build the solution
msbuild %SOLUTION_NAME%.sln -t:clean

REM Verify the executable is no longer present in the output folder.
IF EXIST ./out/%EXE_NAME%.exe (
	echo Output exists after clean but before build, exiting
	EXIT /B -999
)

REM All warnings are considered as errors
msbuild %SOLUTION_NAME%.sln -t:build -warnaserror

REM run the Executable
cd out
START %EXE_NAME%.exe