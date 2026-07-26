$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$pagesRoot = Join-Path $root 'src/OrcaFacil.Web/Pages'
$files = Get-ChildItem -Path $pagesRoot -Filter '*.cshtml' -Recurse -File
$razorPages = $files | Where-Object {
    $_.FullName -notmatch '[\\/]Shared[\\/]' -and
    $_.Name -notlike '_*'
}

$errors = New-Object System.Collections.Generic.List[string]
foreach ($file in $razorPages) {
    $lines = Get-Content -LiteralPath $file.FullName
    $pageLines = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\s*@page(\s|$)') { $pageLines += ($i + 1) }
    }

    if ($pageLines.Count -eq 0) { $errors.Add("$($file.FullName): missing @page directive") }
    if ($pageLines.Count -gt 1) { $errors.Add("$($file.FullName): multiple @page directives at lines $($pageLines -join ', ')") }
    if ($pageLines.Count -eq 1 -and $pageLines[0] -ne 1) { $errors.Add("$($file.FullName): @page is not on first line (line $($pageLines[0]))") }
    foreach ($lineNumber in $pageLines) {
        if ($lines[$lineNumber - 1] -match '@page.*@(page|model|using|inject|inherits|implements)\b') {
            $errors.Add("$($file.FullName): @page is concatenated with another directive on line $lineNumber")
        }
    }
}

foreach ($file in $files) {
    $lineNumber = 0
    foreach ($line in (Get-Content -LiteralPath $file.FullName)) {
        $lineNumber++
        if ($line -match '\bclass\s+\w+\s*:\s*PageModel\b') {
            $errors.Add("$($file.FullName): PageModel class declared in Razor markup on line $lineNumber")
        }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Razor directives OK: $($razorPages.Count) pages checked; partials and layouts ignored."
