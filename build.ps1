    $config_name = "Release"
	$version_name = "1.0.0-acnh-nacho"
	
	dotnet --version

    dotnet publish -c $config_name -r win-x64 /p:Version=$version_name

    #7z a ryujinx$config_name$version_name-win_x64.zip $PSScriptRoot\Ryujinx\bin\$config_name\netcoreapp3.0\win-x64\publish\