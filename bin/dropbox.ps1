param (
    [Parameter(Mandatory=$false)][string]$dir = [IO.Path]::Combine($HOME, "Dropbox", "Statistics", "Arbetsformedlingen"),
    [Parameter(Mandatory=$true)][string]$command
)
Push-Location
try {
    $arbetsformedlingen = [IO.Path]::Combine($PSScriptRoot, ".." , "src", "Stacka.Arbetsformedlingen")
    Set-Location $arbetsformedlingen
    dotnet run --dir $dir $command
}
finally {
    Pop-Location
}
