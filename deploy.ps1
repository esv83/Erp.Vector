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
    $dll = Join-Path $url 'CaSoft.Erp.USVector.Api.dll'

    $startUtc = (Get-Date).ToUniversalTime()

    dotnet publish $proj -c Release "/p:PublishProfile=$profileName"
    if ($LASTEXITCODE) { throw "Echec de la publication $key (profil $profileName)." }

    # Verification : le binaire DOIT avoir ete (re)ecrit sur le partage a l'instant.
    # Evite un faux "[OK]" si la publication est partie en local sans atteindre l'UNC.
    if (-not (Test-Path -LiteralPath $dll)) { throw "Verif KO : $dll introuvable apres publication." }
    $writeUtc = (Get-Item -LiteralPath $dll).LastWriteTimeUtc
    if ($writeUtc -lt $startUtc) {
        throw "Verif KO : $dll non mis a jour (ecrit $writeUtc, debut $startUtc). Rien n'a atteint $url."
    }
    Write-Host "[OK] $($key.ToUpper()) publie et verifie -> $url" -ForegroundColor Green
    Write-Host "     DLL serveur : $((Get-Item -LiteralPath $dll).LastWriteTime)" -ForegroundColor DarkGray
}

Publish-Target $Target
