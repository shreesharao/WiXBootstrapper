
#function to read the bootstrapper version from version file
Function Get-BootstrapperVersion {
    $currentVersion = Get-Content "$PSScriptRoot\BootstrapperVersion.txt";
    Write-Host "Current version is $currentVersion";
    return $currentVersion;
}

#function to update and write the version to version file
Function Update-Version($ver) {
    Write-Host "Updating the bootstrapper's version";
    $major, $minor, $build = $ver.split('.');
    $build = $build -as [Int32];
    
    #increment the build number
    $build++;

    $ver = -join ("$major.", "$minor.", "$build");
    Write-Host "Version incremented to $ver";
    $ver | Out-File "$PSScriptRoot\BootstrapperVersion.txt"

}

Function Copy-Component($source, $destination) {
    try {
        Write-Host "Copying $source to $destination";
        Copy-Item $source $destination;
    }
    catch {
        Write-Host "An Exception has occured. Check the details below" -ForegroundColor Red;
        Write-Host "Exception Message: $($_.Exception.Message)" -ForegroundColor Red;
        Write-Host "StackTrace:$($_.Exception.StackTrace)" -ForegroundColor Red;

        throw "Error trying to copy the installers";
    }
    finally {
        #Free up resources if any
    }    
}

Function Execute-MSBuild {
    Write-Host "Excecuting MSBuild";
    Write-Debug "staging=$PSScriptRoot\staging";
    MSBuild.exe $PSScriptRoot\Bootstrapper.wixproj /t:rebuild "/p:DefineConstants=`"ProductVer=$bootstrapperVersion;`"" /p:OutputPath=target

}

Function Copy-Bootstrapper {
    $bootstrapperTarget = "$artifactRepo\Product\Bootstrapper\$bootstrapperVersion";
    
    #create the directory
    New-Item $bootstrapperTarget -ItemType Directory -Force;
    Write-Host "copying the bootstrapper to $bootstrapperTarget";
    Copy-Item "$PSScriptRoot/target/Reporting.exe" "$bootstrapperTarget\Reporting.exe";

    #copy the version file along with the bootstrapper.This file will be used to find out the version of MSIs included
    #in a bootstrapper
    Copy-Item "$PSScriptRoot/MSIVersion.txt" "$bootstrapperTarget\MSIsIn-$bootstrapperVersion-Bootstrapper.txt";

}

Function Main {
    
    try {
        $bootstrapperVersion = Get-BootstrapperVersion;
        $artifactRepo = "ArtifactRepo";
        Write-Debug "PSScriptRoot=$PSScriptRoot";
        Write-Host "Started building the EDMR Bootstrapper version $bootstrapperVersion";
    
        #start clean, delete any previous staging folders
        if (Test-Path -Path $PSScriptRoot\staging) {
            Remove-Item $PSScriptRoot\staging -Recurse;    
        }
        
        #create the staging folder
        New-Item  $PSScriptRoot\staging -ItemType Directory -Force;

        # Copy the MSIs to staging folder
        #Execute-Copy;
        
        #Execute MSBuild command. This will generate the MSI
        Execute-MSBuild;

        #copy the bootstrapper to ArtifactRepo
        #Copy-Bootstrapper;

        #update the version of bootstrapper if everything is successful
        Update-Version $bootstrapperVersion;

        #commit the updated version file to SVN
        #Commit-VersionFile;

        Write-Host "Execution completed successfully";
    }
    catch {
        Write-Host "An Exception has occured. Check the details below" -ForegroundColor Red;
        Write-Host "Exception Message: $($_.Exception.Message)" -ForegroundColor Red;
        Write-Host "StackTrace:$($_.Exception.StackTrace)" -ForegroundColor Red;
    }

    finally {
        #Free up resources if any
    }
    
}

#catching non-terminating errors. Without this only terminating errors will be caught in catch block
$ErrorActionPreference = "Stop";        

#Call Main function
Main;

