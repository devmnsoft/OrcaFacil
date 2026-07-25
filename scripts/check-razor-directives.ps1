$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$files = Get-ChildItem -Path $root -Filter '*.cshtml' -Recurse -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|node_modules)[\\/]' }

$errors = New-Object System.Collections.Generic.List[string]
foreach ($file in $files) {
    $lines = Get-Content -LiteralPath $file.FullName
    $pageLines = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\s*@page(\s|$)') { $pageLines += ($i + 1) }
    }

    if ($pageLines.Count -gt 1) { $errors.Add("$($file.FullName): multiple @page directives at lines $($pageLines -join ', ')") }
    if ($pageLines.Count -eq 1 -and $pageLines[0] -ne 1) { $errors.Add("$($file.FullName): @page is not on first line (line $($pageLines[0]))") }
    foreach ($lineNumber in $pageLines) {
        if ($lines[$lineNumber - 1] -match '@page\s+.*@model|@model\s+.*@page') { $errors.Add("$($file.FullName): @page and @model are on the same line $lineNumber") }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Razor directives OK: $($files.Count) files checked."
