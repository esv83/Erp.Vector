<#
.SYNOPSIS
  Publie Vector.Api sur le serveur de DEV.

.DESCRIPTION
  La cible = un profil de publication (.pubxml) qui copie la build Release
  sur le partage UNC du serveur (\\192.168.1.112\dev_api\Vector.Api).

  Le web.config (ASPNETCORE_ENVIRONMENT + secrets) est gere MANUELLEMENT sur le
  serveur (csproj : IsTransformWebConfigDisabled=true) -- rien a tamponner ici.

  Prerequis (1re fois) : une session ouverte sur le partage cible
    net use \\192.168.1.112\dev_api /user:192.168.1.112\DeployApi *

.EXAMPLE
  .\deploy.ps1            # publie sur le serveur de dev
  .\deploy.ps1 dev
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('dev')]
    [string]$Target = 'dev'
)

$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'CaSoft.Erp.USVector.Api\CaSoft.Erp.USVector.Api.csproj'

# Cible -> profil pubxml
$profileOf = @{
    dev = 'IIS-DevServer'
}

function Publish-Target($key) {
    $profileName = $profileOf[$key]
    Write-Host ""
    Write-Host "-> Publication $($key.ToUpper()) (profil $profileName)" -ForegroundColor Cyan

    # Cible de copie reelle = publishUrl du profil (UNC). Source unique : le .pubxml.
    $pubxml = Join-Path $PSScriptRoot "CaSoft.Erp.USVector.Api\Properties\PublishProfiles\$profileName.pubxml"
    [xml]$x = Get-Content -LiteralPath $pubxml
    $url = ([string]$x.SelectSingleNode('//*[local-name()="publishUrl"]').InnerText).Trim()
    if (-not $url) { throw "publishUrl introuvable dans $pubxml." }

    dotnet publish $proj -c Release "/p:PublishProfile=$profileName"
    if ($LASTEXITCODE) { throw "Echec de la publication $key (profil $profileName)." }

    # Verification robuste : l'assembly applicatif le plus recent produit par la build (bin local)
    # doit exister a l'IDENTIQUE (meme horodatage) sur l'UNC. La copie de publication preserve
    # l'horodatage source, donc bin == UNC => ce build a bien atteint la cible.
    # Insensible : (a) a quel assembly a change (on prend le plus recent du bin, pas Api.dll seul),
    # (b) a un pre-build (on ne compare pas a un startUtc mais bin<->UNC).
    $binDir = Join-Path $PSScriptRoot 'CaSoft.Erp.USVector.Api\bin\Release'
    $localNewest = Get-ChildItem -LiteralPath $binDir -Recurse -Filter 'CaSoft.*.dll' -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $localNewest) { throw "Verif KO : build local introuvable sous $binDir." }

    $uncFile = Join-Path $url $localNewest.Name
    if (-not (Test-Path -LiteralPath $uncFile)) {
        throw "Verif KO : $($localNewest.Name) absent de $url. La publication n'a pas atteint la cible."
    }
    $uncWriteUtc = (Get-Item -LiteralPath $uncFile).LastWriteTimeUtc
    if ([Math]::Abs(($uncWriteUtc - $localNewest.LastWriteTimeUtc).TotalSeconds) -gt 2) {
        throw "Verif KO : $($localNewest.Name) sur $url ($uncWriteUtc) ne correspond pas au build local " +
              "($($localNewest.LastWriteTimeUtc)). La publication est peut-etre partie en local sans atteindre $url."
    }
    Write-Host "[OK] $($key.ToUpper()) publie et verifie -> $url" -ForegroundColor Green
    Write-Host "     Assembly verifie : $($localNewest.Name) @ $($localNewest.LastWriteTime)" -ForegroundColor DarkGray
}

Publish-Target $Target
