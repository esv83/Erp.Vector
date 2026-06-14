<#
.SYNOPSIS
    Scaffold sélectif d'entités EF Core Database First depuis la BD Mobile.

.DESCRIPTION
    Génère les classes d'entités C# pour une liste de tables MOB_*, dans
    CaSoft.Erp.Mobile.Infrastructure/Persistence/Entities/, avec namespace
    CaSoft.Erp.Mobile.Infrastructure.Persistence.Entities.

    Le DbContext généré par EF est dirigé vers un dossier temporaire puis
    supprimé : seul MobileDbContext (manuel) sert d'unité de configuration.
    Tu dois donc ajouter les DbSets et la config Fluent API à la main dans
    MobileDbContext.cs après le scaffold.

    Pré-requis :
      • dotnet-ef installé globalement :
            dotnet tool install --global dotnet-ef
      • Microsoft.EntityFrameworkCore.Design en PackageReference dans
        CaSoft.Erp.Mobile.Infrastructure (déjà présent).
      • Variable d'env $env:MOBILE_DB_CS définie OU paramètre -ConnectionString
        OU user-secret 'ConnectionStrings:MobileDb' du projet CaSoft.Erp.Mobile.Api.

.PARAMETER Tables
    Liste de tables à scaffolder. Par défaut : les 3 tables MVP (MOB-0).

.PARAMETER ConnectionString
    Chaîne SQL Server (BD_ERP_MOBILE_APP). Si omise : lue depuis
    $env:MOBILE_DB_CS puis depuis les user-secrets de CaSoft.Erp.Mobile.Api.

.EXAMPLE
    .\scripts\scaffold-tables.ps1
    Scaffold les 3 tables MVP.

.EXAMPLE
    .\scripts\scaffold-tables.ps1 -Tables 'MOB_SESSION'
    Re-scaffold une seule table.

.NOTES
    Localisation : CaSoft.Erp.Mobile.Infrastructure/scripts/
    À exécuter depuis la racine du projet CaSoft.Erp.Mobile.Infrastructure :
        cd CaSoft.Erp.Mobile.Infrastructure
        .\scripts\scaffold-tables.ps1
#>

param(
    [string[]] $Tables = @(
        'MOB_SESSION',
        'MOB_MISSION_STATE',
        'MOB_SIGNATURE'
    ),
    [string] $ConnectionString
)

$ErrorActionPreference = 'Stop'

# ── 1. Localiser le projet ────────────────────────────────────────────────────
$projectRoot = Split-Path -Parent $PSScriptRoot
$apiProject  = Join-Path (Split-Path -Parent $projectRoot) 'CaSoft.Erp.Mobile.Api'

if (-not (Test-Path "$projectRoot\CaSoft.Erp.Mobile.Infrastructure.csproj")) {
    throw "CaSoft.Erp.Mobile.Infrastructure.csproj introuvable dans $projectRoot. Exécute le script depuis CaSoft.Erp.Mobile.Infrastructure/scripts/."
}

# ── 2. Récupérer la connection string ────────────────────────────────────────
if (-not $ConnectionString) {
    $ConnectionString = $env:MOBILE_DB_CS
}

if (-not $ConnectionString) {
    Write-Host "→ Lecture du user-secret 'ConnectionStrings:MobileDb' de CaSoft.Erp.Mobile.Api..." -ForegroundColor DarkGray
    Push-Location $apiProject
    try {
        $secretLine = (dotnet user-secrets list 2>$null) | Where-Object { $_ -match '^ConnectionStrings:MobileDb\s*=' }
        if ($secretLine) {
            $ConnectionString = ($secretLine -split '=', 2)[1].Trim()
        }
    }
    finally { Pop-Location }
}

if (-not $ConnectionString) {
    throw "Aucune connection string fournie. Paramètre -ConnectionString, variable `$env:MOBILE_DB_CS, ou dotnet user-secrets de CaSoft.Erp.Mobile.Api requis."
}

Write-Host "→ Connection : $($ConnectionString -replace 'Password=[^;]+', 'Password=***')" -ForegroundColor DarkGray

# ── 3. Paramètres scaffold ────────────────────────────────────────────────────
$entityOutDir     = 'Persistence/Entities'
$tempCtxDir       = 'Persistence/_temp_scaffold'
$tempCtxName      = '_ScaffoldTempContext'
$entityNamespace  = 'CaSoft.Erp.Mobile.Infrastructure.Persistence.Entities'
$ctxNamespace     = 'CaSoft.Erp.Mobile.Infrastructure.Persistence._Scaffold'

$tableArgs = $Tables | ForEach-Object { @('--table', $_) } | ForEach-Object { $_ }

$dotnetArgs = @(
    'ef', 'dbcontext', 'scaffold',
    $ConnectionString,
    'Microsoft.EntityFrameworkCore.SqlServer',
    '--project',           $projectRoot,
    '--output-dir',        $entityOutDir,
    '--context',           $tempCtxName,
    '--context-dir',       $tempCtxDir,
    '--namespace',         $entityNamespace,
    '--context-namespace', $ctxNamespace,
    '--no-onconfiguring',
    '--use-database-names',
    '--force'
) + $tableArgs

# ── 4. Exécuter le scaffold ──────────────────────────────────────────────────
Write-Host "→ Scaffold de $($Tables.Count) table(s) : $($Tables -join ', ')" -ForegroundColor Cyan
Push-Location $projectRoot
try {
    & dotnet @dotnetArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef dbcontext scaffold a échoué (exit $LASTEXITCODE)."
    }
}
finally { Pop-Location }

# ── 5. Supprimer le DbContext temporaire ─────────────────────────────────────
$tempCtxFullPath = Join-Path $projectRoot $tempCtxDir
if (Test-Path $tempCtxFullPath) {
    Remove-Item $tempCtxFullPath -Recurse -Force
    Write-Host "→ DbContext temporaire supprimé : $tempCtxDir" -ForegroundColor DarkGray
}

# ── 6. Récap ─────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "✓ Scaffold terminé." -ForegroundColor Green
Write-Host "  Entités générées dans : $projectRoot\$entityOutDir" -ForegroundColor Gray
Write-Host ""
Write-Host "Étape suivante :" -ForegroundColor Yellow
Write-Host "  • Ajouter manuellement les DbSets correspondants dans MobileDbContext.cs" -ForegroundColor Yellow
Write-Host "  • Ajouter la config Fluent API (HasKey, HasIndex, …) dans OnModelCreating" -ForegroundColor Yellow
Write-Host "  • dotnet build pour vérifier" -ForegroundColor Yellow
