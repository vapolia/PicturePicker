######NuGet.CommandLine is too old and does not support Xamarin.iOS libs
#####Please install the package "NuGe t.CommandLine" from https://chocolatey.org/ before running this script
#####After chocolatey is installed, type: choco install NuGet.CommandLine
#####Before running this script, download nuget.exe from @echo https://nuget.codeplex.com/releases/view/133091
#####and put nuget.exe in the path.

#####set /p nugetServer=Enter base nuget server url (with /): 
$nugetServer="http://nugets.vapolia.fr/"
$msbuild = 'C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe'
$version="3.0.7"

#####################
#Build release config
cd ..
nuget restore
$msbuildparams = '/t:Clean;Build', '/p:Configuration=Release', '/p:Platform=Any CPU', 'Vapolia.Mvvmcross.PicturePicker.sln'
& $msbuild $msbuildparams
cd .nuget

del *.nupkg

nuget pack "Vapolia.Mvvmcross.PicturePicker.nuspec" -Version $version
nuget push "Vapolia.Mvvmcross.PicturePicker*.nupkg" -Source $nugetServer

