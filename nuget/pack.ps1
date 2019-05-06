######NuGet.CommandLine is too old and does not support Xamarin.iOS libs
#####Please install the package "NuGe t.CommandLine" from https://chocolatey.org/ before running this script
#####After chocolatey is installed, type: choco install NuGet.CommandLine
#####Before running this script, download nuget.exe from @echo https://nuget.codeplex.com/releases/view/133091
#####and put nuget.exe in the path.

#####set /p nugetServer=Enter base nuget server url (with /): 
$nugetServer="http://nugets.vapolia.fr/"
$msbuild = 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe'

#####################
#Build release config
$version="3.1.1"
$msbuildparams = '/t:Clean;Build', '/p:Configuration=Release', '/p:Platform=Any CPU', 'Vapolia.Mvvmcross.PicturePicker.sln'

cd ..
nuget restore
& $msbuild $msbuildparams
cd nuget

del *.nupkg

nuget pack "Vapolia.Mvvmcross.PicturePicker.nuspec" -Version $version
nuget push "Vapolia.Mvvmcross.PicturePicker*.nupkg" -Source $nugetServer

