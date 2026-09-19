<#
.SYNOPSIS
  Publie Vector.Api sur le serveur de DEV ou de PROD.

.DESCRIPTION
  La cible = un profil de publication (.pubxml) qui copie la build Release
  sur le partage UNC du serveur :
    dev  -> \\192.168.1.112\dev_api\Vector.Api
    prod -> \\192.168.1.112\prod_api\Vector.Api

  Le web.config (ASPNETCORE_ENVIRONMENT + secrets) est gere MANUELLEMENT sur le
  serveur (csproj : IsTransformWebConfigDisabled=true) -- rien a tamponner ici.

  ATTENTION : appsettings.json EST ecrase par la publication (il fait partie de la
  sortie) et porte les valeurs de dev. Toute valeur specifique a la prod doit vivre
  dans appsettings.Production.json, qui est shippe et surcharge la base.

  La publication vers PROD demande une confirmation explicite (-Force pour la sauter,
  p.ex. en CI). La publication pose app_offline.htm : l'API est coupee le temps de la copie.

  GARDE DE DEPOT (PROD) : la publication compile l'ARBRE DE TRAVAIL, pas HEAD. On ne publie donc
  en PROD que depuis `main`, arbre propre (fichiers non suivis compris : le SDK les compile), et
  HEAD egal a origin/main -- ni en retard, ni en avance (un commit non pousse ne se reproduit pas).
  Cinq publications de ce qui n'etait pas prevu entre le 25/08 et le 15/09, dont cinq heures sans
  la capture mutuelle ni le correctif de cloture. -Force ne saute PAS cette garde.
  -CheckOnly : execute la garde et s'arrete, sans rien publier.

  APRES PUBLICATION : le .pdb de la cible doit annoncer le commit publie (sourcelink), et
  appsettings*.json + nlog.config doivent etre identiques au depot.

  Prerequis (1re fois) : une session ouverte sur le partage cible
    net use \\192.168.1.112\dev_api  /user:192.168.1.112\DeployApi *
    net use \\192.168.1.112\prod_api /user:192.168.1.112\DeployApi *

.EXAMPLE
  .\deploy.ps1            # publie sur le serveur de dev
  .\deploy.ps1 dev
  .\deploy.ps1 prod       # demande confirmation
  .\deploy.ps1 prod -Force
  .\deploy.ps1 prod -CheckOnly   # "puis-je publier ?" -- garde seule, rien n'est touche
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('dev', 'prod')]
    [string]$Target = 'dev',

    # Saute la confirmation interactive de la cible PROD (usage non-interactif / CI).
    # Ne saute PAS la garde de depot.
    [switch]$Force,

    # Execute la garde de depot et s'arrete, sans rien publier.
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'CaSoft.Erp.USVector.Api\CaSoft.Erp.USVector.Api.csproj'

# Cible -> profil pubxml
$profileOf = @{
    dev  = 'IIS-DevServer'
    prod = 'IIS-ProdServer'
}

# Cibles sensibles : confirmation explicite avant de couper l'app et d'ecraser la sortie.
$protected = @('prod')

function Confirm-Target($key, $url) {
    if ($key -notin $protected -or $Force) { return }

    Write-Host ""
    Write-Host "  /!\  Publication en $($key.ToUpper()) : $url" -ForegroundColor Yellow
    Write-Host "       L'API sera coupee (app_offline.htm) le temps de la copie." -ForegroundColor Yellow
    $answer = Read-Host "       Taper $key pour confirmer"
    if ($answer -ne $key) { throw "Publication $key annulee (confirmation non saisie)." }
}

function Invoke-Git {
    $out = & git -C $PSScriptRoot @args
    if ($LASTEXITCODE) { throw "git $($args -join ' ') a echoue (code $LASTEXITCODE)." }
    return $out
}

# Garde de depot (G8). Rend le SHA de HEAD, que la verification finale retrouve dans le .pdb publie.
# En DEV, simple information : on y essaie justement des branches et des arbres en cours.
function Assert-PublishableTree($key) {
    $head = ([string](Invoke-Git rev-parse HEAD)).Trim()
    $short = $head.Substring(0, 7)
    $branch = ([string](Invoke-Git rev-parse --abbrev-ref HEAD)).Trim()
    # Fichiers non suivis compris : le SDK compile tout *.cs / *.vb du dossier, suivi ou non.
    $dirty = @(Invoke-Git status --porcelain | Where-Object { $_ })

    if ($key -notin $protected) {
        $state = if ($dirty.Count) { ", arbre modifie ($($dirty.Count) fichier(s))" } else { '' }
        Write-Host "   Depot : $branch @ $short$state" -ForegroundColor DarkGray
        return $head
    }

    if ($branch -ne 'main') {
        throw "Garde de depot : publication $key refusee depuis la branche '$branch'. " +
              "La production ne se publie que depuis main (incident du 15/09 : une branche sans " +
              "les correctifs de main a tourne cinq heures)."
    }
    if ($dirty.Count) {
        throw "Garde de depot : publication $key refusee, l'arbre de travail est modifie -- la " +
              "publication compilerait ce qui n'est dans aucun commit :`n  " + ($dirty -join "`n  ")
    }

    Invoke-Git fetch --quiet origin main | Out-Null
    $remote = ([string](Invoke-Git rev-parse origin/main)).Trim()
    if ($head -ne $remote) {
        $ahead = ([string](Invoke-Git rev-list --count 'origin/main..HEAD')).Trim()
        $behind = ([string](Invoke-Git rev-list --count 'HEAD..origin/main')).Trim()
        throw "Garde de depot : publication $key refusee, main ($short) n'est pas origin/main " +
              "($($remote.Substring(0, 7))) -- $ahead commit(s) non pousse(s), $behind en retard. " +
              "Pousser (ou tirer) avant de publier : ce qui tourne doit se reproduire depuis git."
    }

    Write-Host "   Depot : main @ $short, propre, egal a origin/main" -ForegroundColor DarkGray
    return $head
}

function Publish-Target($key) {
    $profileName = $profileOf[$key]
    Write-Host ""
    Write-Host "-> Publication $($key.ToUpper()) (profil $profileName)" -ForegroundColor Cyan

    # Garde de depot AVANT tout effet : ni pre-vol, ni confirmation, ni app_offline.htm.
    $head = Assert-PublishableTree $key
    if ($CheckOnly) {
        Write-Host "[OK] Garde de depot $($key.ToUpper()) satisfaite -- rien n'a ete publie (-CheckOnly)." -ForegroundColor Green
        return
    }

    # Cible de copie reelle = publishUrl du profil (UNC). Source unique : le .pubxml.
    $pubxml = Join-Path $PSScriptRoot "CaSoft.Erp.USVector.Api\Properties\PublishProfiles\$profileName.pubxml"
    [xml]$x = Get-Content -LiteralPath $pubxml
    $url = ([string]$x.SelectSingleNode('//*[local-name()="publishUrl"]').InnerText).Trim()
    if (-not $url) { throw "publishUrl introuvable dans $pubxml." }

    # Pre-vol : sans session ouverte sur le partage, MSBuild echoue tard et le message est obscur
    # (et sur une cible protegee on aurait deja pose app_offline.htm). On tranche avant.
    if (-not (Test-Path -LiteralPath $url)) {
        # Racine du partage = \\serveur\partage (Split-Path -Qualifier ne gere pas l'UNC).
        $share = if ($url -match '^(\\\\[^\\]+\\[^\\]+)') { $Matches[1] } else { $url }
        throw "Cible $url inaccessible. Ouvrir la session : net use $share /user:192.168.1.112\DeployApi *"
    }

    Confirm-Target $key $url

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
    # Verification du COMMIT : le .pdb publie porte, par sourcelink, le commit compile. C'est lui
    # qu'on lit pour dire ce qui tourne ; il doit donc annoncer HEAD.
    $pdbUnc = Join-Path $url 'CaSoft.Erp.USVector.Api.pdb'
    if (-not (Test-Path -LiteralPath $pdbUnc)) { throw "Verif KO : CaSoft.Erp.USVector.Api.pdb absent de $url." }
    $pdbText = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($pdbUnc))
    if ($pdbText -notmatch "esv83/Erp\.Vector/$head") {
        throw "Verif KO : le .pdb de $url n'annonce pas le commit publie ($($head.Substring(0, 7)))."
    }

    # Verification de la CONFIG. Un binaire a jour sur un appsettings.json perime fait ECHOUER LE
    # DEMARRAGE (garde-fou KC-1 de Program.cs) ; un nlog.config perime fait perdre des journaux sans
    # un signal -- constate le 19/09 : la cible de la sonde de surface (27/08) n'etait jamais arrivee
    # en production. La copie de publication peut sauter un fichier (horodatages) : on compare tout.
    $apiDir = Join-Path $PSScriptRoot 'CaSoft.Erp.USVector.Api'
    $configs = @(Get-ChildItem -LiteralPath $apiDir -Filter 'appsettings*.json' | ForEach-Object Name) + 'nlog.config'
    foreach ($name in $configs) {
        $cfgUnc = Join-Path $url $name
        if (-not (Test-Path -LiteralPath $cfgUnc)) { throw "Verif KO : $name absent de $url." }
        if ((Get-FileHash -LiteralPath (Join-Path $apiDir $name)).Hash -ne (Get-FileHash -LiteralPath $cfgUnc).Hash) {
            throw "Verif KO : $name de $url differe du depot. La publication ne l'a pas copie : " +
                  "copier le fichier a la main (appsettings perime = l'API refuse de demarrer)."
        }
    }

    Write-Host "[OK] $($key.ToUpper()) publie et verifie -> $url" -ForegroundColor Green
    Write-Host "     Commit verifie   : $($head.Substring(0, 7)) annonce par le .pdb publie" -ForegroundColor DarkGray
    Write-Host "     Assembly verifie : $($localNewest.Name) @ $($localNewest.LastWriteTime)" -ForegroundColor DarkGray
    Write-Host "     Config verifiee  : $($configs -join ', ') identiques au depot" -ForegroundColor DarkGray
}

Publish-Target $Target
