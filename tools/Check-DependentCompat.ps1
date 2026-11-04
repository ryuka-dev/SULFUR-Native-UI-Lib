<#
.SYNOPSIS
    Checks that mods compiled against an older SULFUR Native UI Lib still bind to a new build.

.DESCRIPTION
    For every dependent DLL, collects each type and member reference into the
    "SULFUR Native UI Lib" assembly and resolves it, with its full signature, against the
    library in -LibDir. Anything that does not resolve would fail at run time with
    TypeLoadException / MissingMethodException / MissingFieldException.

    Only metadata is read (Mono.Cecil, shipped with BepInEx); nothing is loaded or run.
    Reflection lookups by name (e.g. AccessTools.TypeByName("Ryuka.Sulfur.NativeUI.SulfurToastApi"))
    are NOT covered: check the dependent's string literals for those separately.

    Run it once against the previous release as a control: every reference must resolve there.

.EXAMPLE
    .\tools\Check-DependentCompat.ps1 -LibDir .\bin\Debug -Dependents "C:\dl\SULFUR Together.dll","C:\dl\SulfurNoGore.dll"

.NOTES
    Published dependents (Thunderstore, 2026-11-05): ryuka_labs-SULFUR_Together,
    ryuka_labs-SULFUR_Config, Nemophila-SimpleChat, h3nw-Sulfur_NoGore. Refresh the list from
    https://thunderstore.io/c/sulfur/api/v1/package/ (packages whose latest version depends on
    ryuka_labs-SULFUR_Native_UI_Lib).
#>
param(
    [Parameter(Mandatory = $true)] [string] $LibDir,
    [Parameter(Mandatory = $true)] [string[]] $Dependents,
    [string] $BepInExCore = "$env:APPDATA\com.kesomannen.gale\sulfur\profiles\Default\BepInEx\core",
    [string] $GameManaged = "D:\SteamLibrary\steamapps\common\SULFUR\Sulfur_Data\Managed"
)

$libName = "SULFUR Native UI Lib"
Add-Type -Path (Join-Path $BepInExCore "Mono.Cecil.dll")

$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory((Resolve-Path $LibDir).Path)
$resolver.AddSearchDirectory($GameManaged)
$resolver.AddSearchDirectory($BepInExCore)
$readerParams = New-Object Mono.Cecil.ReaderParameters
$readerParams.AssemblyResolver = $resolver

$failed = $false

foreach ($dep in $Dependents) {
    $module = [Mono.Cecil.ModuleDefinition]::ReadModule($dep, $readerParams)
    $libRef = $module.AssemblyReferences | Where-Object { $_.Name -eq $libName }
    $total = 0
    $broken = @()

    foreach ($t in $module.GetTypeReferences()) {
        if ($t.Scope.Name -ne $libName) { continue }
        $total++
        try { $r = $t.Resolve() } catch { $r = $null }
        if ($r -eq $null) { $broken += "type   " + $t.FullName }
    }

    foreach ($m in $module.GetMemberReferences()) {
        $decl = $m.DeclaringType
        while ($decl -is [Mono.Cecil.TypeSpecification]) { $decl = $decl.ElementType }
        if ($decl -eq $null -or $decl.Scope.Name -ne $libName) { continue }
        $total++
        try { $r = $m.Resolve() } catch { $r = $null }
        if ($r -eq $null) { $broken += "member " + $m.FullName }
    }

    $version = if ($libRef) { $libRef.Version } else { "-" }
    "{0}: compiled against lib {1}, {2} references, {3} unresolved" -f (Split-Path $dep -Leaf), $version, $total, $broken.Count
    $broken | ForEach-Object { "    MISSING " + $_ }
    if ($broken.Count -gt 0) { $failed = $true }

    $module.Dispose()
}

if ($failed) { exit 1 }
