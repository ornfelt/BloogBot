$directoryPath = Get-Location

# Get all the files with the .output.txt extension
Get-ChildItem -Path $directoryPath -Recurse -Filter "*.output.txt" | ForEach-Object {
    # Remove each file
    Remove-Item $_.FullName -Force
    Write-Host "Removed $($_.FullName)"
}

# Recursively find and remove all .output.txt files (both versions work)
# Get-ChildItem -Path $directoryPath -Recurse -Filter "*.output.txt" | Remove-Item -Force -Confirm:$false


Write-Host "Cleanup completed."
