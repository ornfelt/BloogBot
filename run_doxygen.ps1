# Define the directory to start the search from (current directory)
$rootDirectory = Get-Location

# Define the name of the doxyfile
$doxyfileName = "doxyfile"

# Check if any arguments were provided
if ($args.Count -gt 0) {
    # Print the directory where the Doxyfile exists and exit
    $doxyfiles = Get-ChildItem -Path $rootDirectory -Filter $doxyfileName -File -Recurse
    $doxyfiles | ForEach-Object { Write-Host "Doxyfile directory: $($_.DirectoryName)" }
    return
}

# Recursively search for doxyfile in the root directory and its sub-directories
$doxyfiles = Get-ChildItem -Path $rootDirectory -Filter $doxyfileName -File -Recurse

# Iterate through each directory containing doxyfile and run doxygen if "generated-docs" directory doesn't exist
foreach ($doxyfile in $doxyfiles) {
    $directory = $doxyfile.DirectoryName
    $generatedDocsDir = Join-Path -Path $directory -ChildPath "generated-docs"
    
    # Check if the "generated-docs" directory already exists
    if (-Not (Test-Path -Path $generatedDocsDir -PathType Container)) {
        Write-Host "Running doxygen in directory: $directory"
        
        # Change the current directory to the one containing doxyfile
        Set-Location -Path $directory
        
        # Run the doxygen command
        doxygen
        
        # Restore the current directory
        Set-Location -Path $rootDirectory
    } else {
        Write-Host "Skipping Doxygen in directory: $directory (generated-docs directory already exists)"
    }
}

# Return to the original current directory
Set-Location -Path $rootDirectory