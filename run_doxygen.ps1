# Define the directory to start the search from (current directory)
$rootDirectory = Get-Location

# Define the name of the doxyfile
$doxyfileName = "doxyfile"

# Recursively search for doxyfile in the root directory and its sub-directories
$doxyfiles = Get-ChildItem -Path $rootDirectory -Filter $doxyfileName -File -Recurse

# Iterate through each directory containing doxyfile and run doxygen
foreach ($doxyfile in $doxyfiles) {
    $directory = $doxyfile.DirectoryName
    Write-Host "Running doxygen in directory: $directory"
    
    # Change the current directory to the one containing doxyfile
    Set-Location -Path $directory
    
    # Run the doxygen command
    doxygen
    
    # Restore the current directory
    Set-Location -Path $rootDirectory
}

# Return to the original current directory
Set-Location -Path $rootDirectory