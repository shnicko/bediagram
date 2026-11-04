<#
.SYNOPSIS
    Checks the "one type per file" rule for BeDiagram.

.DESCRIPTION
    Every class, record, interface, enum and struct lives in its own .cs file named after the type:
      - at most one top-level type per file (several partial-class files of the same type are fine);
      - the file name, up to the first dot, matches the type (Designer.razor.cs and Designer.EditorContext.cs → Designer);
      - no public nested types (private / internal nested helpers may stay nested).
    Files without a type (Program.cs, top-level statements) are fine.

    Works in Windows PowerShell 5.1 and PowerShell 7.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Check-OneTypePerFile.ps1
#>
param(
    [string]$Root = (Join-Path $PSScriptRoot '..\BeDiagram')
)

$modifiers = '(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|file|unsafe|new)\s+)*'
$kinds = '(?:record\s+(?:struct|class)\s+|record\s+|class\s+|interface\s+|enum\s+|struct\s+)'
$topLevel = [regex]('^' + $modifiers + $kinds + '(\w+)')
$publicNested = [regex]('^\s+(?:(?:static|sealed|abstract|partial|readonly|new)\s+)*public\s+' + $modifiers + $kinds + '(\w+)')

$problems = New-Object System.Collections.Generic.List[string]
$files = Get-ChildItem -Path $Root -Recurse -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

foreach ($file in $files) {
    $rel = $file.FullName.Substring((Resolve-Path $Root).Path.Length).TrimStart('\', '/')
    $expected = $file.Name.Split('.')[0]
    $types = New-Object System.Collections.Generic.List[string]
    $lineNo = 0
    foreach ($line in [System.IO.File]::ReadAllLines($file.FullName)) {
        $lineNo++
        $m = $topLevel.Match($line)
        if ($m.Success) { $types.Add($m.Groups[1].Value) }
        $n = $publicNested.Match($line)
        if ($n.Success) { $problems.Add("$rel($lineNo): public nested type '$($n.Groups[1].Value)' — move it to $($n.Groups[1].Value).cs") }
    }
    $distinct = @($types | Select-Object -Unique)
    if ($distinct.Count -gt 1) {
        $problems.Add("${rel}: $($distinct.Count) types ($($distinct -join ', ')) — one type per file")
    }
    foreach ($t in $distinct) {
        if ($t -ne $expected) { $problems.Add("${rel}: type '$t' doesn't match the file name — expected $t.cs") }
    }
}

if ($problems.Count -gt 0) {
    Write-Host "One-type-per-file check: $($problems.Count) problem(s)" -ForegroundColor Red
    foreach ($p in $problems) { Write-Host "  $p" }
    exit 1
}
Write-Host "One-type-per-file check: OK ($($files.Count) files)" -ForegroundColor Green
exit 0
