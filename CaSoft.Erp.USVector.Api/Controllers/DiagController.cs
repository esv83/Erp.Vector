using CaSoft.Erp.USVector.Infrastructure.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CaSoft.Erp.USVector.Api.Controllers;

/// <summary>
/// Diagnostic développeur (DEV-ONLY) de la chaîne d'identité mobile :
/// <c>sub Keycloak → PER_ID → crews candidats → membre ? → actif ?</c>.
/// <list type="bullet">
///   <item><c>GET /api/diag</c> (ou <c>/diag</c>) : page HTML visuelle (saisir un sub, voir la chaîne).</item>
///   <item><c>GET /api/diag/crew-chain?sub=&amp;at=</c> : le JSON brut de la chaîne.</item>
/// </list>
/// Activé uniquement en environnement Development, ou si <c>Diagnostics:Enabled = true</c>
/// (ex. serveur de dev IIS via variable <c>Diagnostics__Enabled</c>). Sinon 404.
/// </summary>
[ApiController]
[Route("api/diag")]
public class DiagController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _cfg;

    // Le service ERP est résolu à la demande (dans l'action), pas au ctor : la page HTML doit se servir
    // même si le client Orders.Api n'est pas construisible (ex. OrdersApi:BaseUrl absent).
    public DiagController(IWebHostEnvironment env, IConfiguration cfg)
    {
        _env = env;
        _cfg = cfg;
    }

    private bool Enabled => _env.IsDevelopment() || _cfg.GetValue("Diagnostics:Enabled", false);

    [HttpGet("crew-chain")]
    public async Task<IActionResult> CrewChain([FromQuery] Guid sub, [FromQuery] DateTime? at, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (sub == Guid.Empty)
            return BadRequest("Paramètre 'sub' (Guid Keycloak) requis.");

        CrewChainDiagnostic diag;
        try
        {
            diag = HttpContext.RequestServices.GetRequiredService<CrewChainDiagnostic>();
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                $"Client Orders.Api non initialisé (vérifie OrdersApi:BaseUrl) : {ex.Message}");
        }

        var result = await diag.DiagnoseAsync(sub, at ?? DateTime.Now, ct);
        return Ok(result);
    }

    // Résout un username Keycloak en utilisateur(s) (dont l'id = le sub) via l'Admin API (service account).
    [HttpGet("resolve-user")]
    public async Task<IActionResult> ResolveUser([FromQuery] string username, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest("Paramètre 'username' requis.");

        KeycloakAdminClient admin;
        try
        {
            admin = HttpContext.RequestServices.GetRequiredService<KeycloakAdminClient>();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Client Admin Keycloak indisponible : {ex.Message}");
        }

        if (!admin.IsConfigured)
            return StatusCode(501,
                "Recherche par username non configurée : renseigne Keycloak:AdminClientId / AdminClientSecret " +
                "(service account avec rôles realm-management view-users/query-users).");

        try
        {
            var users = await admin.FindUsersByUsernameAsync(username.Trim(), ct);
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(502, $"Erreur Admin API Keycloak : {ex.Message}");
        }
    }

    [HttpGet]
    [HttpGet("/diag")]
    public IActionResult Page()
    {
        if (!Enabled) return NotFound();
        // Injecte la base d'API réelle (gère le déploiement en sous-application IIS, ex. /vector) :
        // les fetch de la page sont relatifs à cette base, pas à la racine du site.
        var apiBase = (Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty) + "/api/diag";
        return new ContentResult
        {
            ContentType = "text/html; charset=utf-8",
            Content = Html.Replace("__API_BASE__", apiBase)
        };
    }

    private const string Html =
"""
<!doctype html>
<html lang="fr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Diag chaîne identité — USVector</title>
<style>
  :root { color-scheme: light dark; }
  body { font-family: system-ui, Segoe UI, Roboto, sans-serif; margin: 0; background: #0f1115; color: #e6e6e6; }
  header { padding: 18px 24px; background: #161a22; border-bottom: 1px solid #262b36; }
  h1 { font-size: 18px; margin: 0; }
  .sub { color: #8b93a1; font-size: 13px; margin-top: 4px; }
  main { max-width: 980px; margin: 0 auto; padding: 24px; }
  .form { display: flex; gap: 10px; flex-wrap: wrap; align-items: end; background: #161a22; padding: 16px; border-radius: 10px; border: 1px solid #262b36; }
  .form label { display: block; font-size: 12px; color: #8b93a1; margin-bottom: 4px; }
  input { background: #0f1115; color: #e6e6e6; border: 1px solid #333a47; border-radius: 7px; padding: 9px 11px; font-size: 14px; }
  input#sub { width: 340px; font-family: ui-monospace, monospace; }
  button { background: #2f6feb; color: #fff; border: 0; border-radius: 7px; padding: 10px 18px; font-size: 14px; cursor: pointer; }
  button:hover { background: #4079f0; }
  button.alt { background: #333a47; }
  button.alt:hover { background: #3f4757; }
  .urow { display: flex; align-items: center; gap: 10px; margin: 6px 0; flex-wrap: wrap; }
  .muted { color: #8b93a1; }
  .err { color: #ff6b6b; background: #2a1414; border: 1px solid #52201f; padding: 12px; border-radius: 8px; }
  .step { margin-top: 22px; }
  .card { background: #161a22; border: 1px solid #262b36; border-radius: 10px; padding: 16px; margin-bottom: 14px; }
  .card.ok { border-left: 4px solid #2ec26a; }
  .card.ko { border-left: 4px solid #ff6b6b; }
  .card h3 { margin: 0 0 8px; font-size: 15px; }
  .kv { font-family: ui-monospace, monospace; font-size: 13px; color: #cbd2dd; }
  .pills { display: flex; gap: 8px; flex-wrap: wrap; margin: 10px 0; }
  .pill { font-size: 12px; padding: 4px 10px; border-radius: 20px; border: 1px solid; }
  .pill.on { color: #2ec26a; border-color: #245c3c; background: #10261a; }
  .pill.off { color: #ff6b6b; border-color: #52201f; background: #241313; }
  .verdict { font-weight: 600; padding: 8px 12px; border-radius: 7px; margin-top: 8px; display: inline-block; }
  .verdict.ok { color: #2ec26a; background: #10261a; }
  .verdict.ko { color: #ff6b6b; background: #241313; }
  .members { margin: 8px 0 0; padding-left: 18px; font-size: 13px; }
  .members li.target { color: #ffd166; font-weight: 600; }
  .summary { font-size: 15px; margin: 8px 0 18px; }
  .badge { font-family: ui-monospace, monospace; }
</style>
</head>
<body>
<header>
  <h1>Diagnostic chaîne d'identité — USVector</h1>
  <div class="sub">sub Keycloak → PER_ID → crews candidats → membre ? → actif ? (règle réelle IsSelectableAt)</div>
</header>
<main>
  <div class="form">
    <div>
      <label for="username">Chercher par username Keycloak</label>
      <input id="username" placeholder="ex. jdupont" onkeydown="if(event.key==='Enter')resolveUser()">
    </div>
    <button class="alt" onclick="resolveUser()">Résoudre le sub</button>
  </div>
  <div id="users" class="step"></div>
  <div class="form" style="margin-top:14px">
    <div>
      <label for="sub">sub Keycloak (Guid)</label>
      <input id="sub" placeholder="ex. 11111111-1111-1111-1111-111111111111">
    </div>
    <div>
      <label for="at">Instant testé (optionnel)</label>
      <input id="at" type="datetime-local">
    </div>
    <button onclick="run()">Diagnostiquer</button>
  </div>
  <div id="out" class="step"><p class="muted">Cherche un username, ou colle un sub, puis lance le diagnostic.</p></div>
</main>
<script>
var API = '__API_BASE__'; // base d'API injectée côté serveur (gère le sous-chemin IIS, ex. /vector/api/diag)
function esc(s){ return (s==null?'':String(s)).replace(/[&<>]/g, c=>({'&':'&amp;','<':'&lt;','>':'&gt;'}[c])); }
function fmt(dt){ return dt ? new Date(dt).toLocaleString('fr-FR') : '—'; }
function pill(label, ok){ return '<span class="pill '+(ok?'on':'off')+'">'+(ok?'✓ ':'✗ ')+esc(label)+'</span>'; }

async function resolveUser(){
  var u = document.getElementById('username').value.trim();
  var box = document.getElementById('users');
  if(!u){ box.innerHTML = '<p class="err">Renseigne un username.</p>'; return; }
  box.innerHTML = '<p class="muted">Recherche Keycloak…</p>';
  try{
    var res = await fetch(API + '/resolve-user?username=' + encodeURIComponent(u));
    if(res.status === 501){ box.innerHTML = '<p class="err">Recherche par username non configurée côté serveur (Keycloak:AdminClientId / AdminClientSecret). Tu peux coller un sub à la main.</p>'; return; }
    if(!res.ok){ box.innerHTML = '<p class="err">HTTP ' + res.status + ' — ' + esc(await res.text()) + '</p>'; return; }
    var users = await res.json();
    if(!users.length){ box.innerHTML = '<p class="muted">Aucun utilisateur « ' + esc(u) + ' ».</p>'; return; }
    if(users.length === 1){ box.innerHTML = ''; pickUser(users[0].Id, users[0].Username); return; }
    box.innerHTML = '<div class="card"><h3>' + users.length + ' correspondances — choisis :</h3>' +
      users.map(function(x, i){ return '<div class="urow"><button class="alt" data-i="' + i + '">' + esc(x.Username || '(sans nom)') + '</button> <span class="kv">' + esc(x.Id) + (x.Enabled ? '' : ' · désactivé') + (x.Email ? ' · ' + esc(x.Email) : '') + '</span></div>'; }).join('') + '</div>';
    box.querySelectorAll('button[data-i]').forEach(function(b){ b.addEventListener('click', function(){ var x = users[+b.getAttribute('data-i')]; pickUser(x.Id, x.Username); }); });
  }catch(e){ box.innerHTML = '<p class="err">' + esc(e) + '</p>'; }
}

function pickUser(id, username){
  document.getElementById('sub').value = id;
  document.getElementById('users').innerHTML = '<p class="muted">sub de « ' + esc(username) + ' » : <span class="kv">' + esc(id) + '</span></p>';
  run();
}

async function run(){
  var sub = document.getElementById('sub').value.trim();
  var at = document.getElementById('at').value;
  var out = document.getElementById('out');
  if(!sub){ out.innerHTML = '<p class="err">Renseigne un sub (Guid Keycloak).</p>'; return; }
  var url = API + '/crew-chain?sub=' + encodeURIComponent(sub);
  if(at) url += '&at=' + encodeURIComponent(at);
  out.innerHTML = '<p class="muted">Résolution…</p>';
  try{
    var res = await fetch(url);
    if(!res.ok){ out.innerHTML = '<p class="err">HTTP ' + res.status + ' — ' + esc(await res.text()) + '</p>'; return; }
    render(await res.json());
  }catch(e){ out.innerHTML = '<p class="err">' + esc(e) + '</p>'; }
}

function render(d){
  var html = '';
  if(d.Error){ html += '<p class="err">Erreur d\'appel ERP : ' + esc(d.Error) + '</p>'; }

  // Maillon 1 : sub → PER_ID
  var okPer = d.Step1Ok;
  html += '<div class="card ' + (okPer?'ok':'ko') + '">';
  html += '<h3>1 — sub Keycloak → PER_ID</h3>';
  html += '<div class="kv">sub : ' + esc(d.Sub) + '</div>';
  html += '<div class="kv">instant testé : ' + fmt(d.At) + '</div>';
  html += okPer
    ? '<div class="verdict ok">PER_ID = ' + esc(d.PersonnelId) + '</div>'
    : '<div class="verdict ko">Compte non rattaché à un personnel (PER_KEYCLOAK_MAP)</div>';
  html += '</div>';

  if(!okPer){ document.getElementById('out').innerHTML = html; return; }

  // Maillon 2 : crews candidats
  var sel = (d.SelectableCrewIds||[]).length;
  html += '<div class="summary">Crews candidats : <b>' + d.CandidateCount + '</b> · sélectionnables (membre + actif) : <b style="color:' + (sel?'#2ec26a':'#ff6b6b') + '">' + sel + '</b></div>';

  (d.Crews||[]).forEach(function(c){
    var good = c.IsMember && c.Selectable;
    html += '<div class="card ' + (good?'ok':'ko') + '">';
    html += '<h3 class="badge">' + esc(c.VehicleImmat || '(sans véhicule)') + ' · <span class="muted">' + esc(c.CrewId) + '</span></h3>';
    if(!c.Found){ html += '<div class="verdict ko">Introuvable côté ERP</div></div>'; return; }
    html += '<div class="kv">service : ' + fmt(c.ServiceStart) + ' → ' + (c.ServiceEnd?fmt(c.ServiceEnd):'… (ouvert)') + '</div>';
    html += '<div class="pills">' + pill('membre', c.IsMember) + pill('démarré', c.Started) + pill('non clôturé', c.NotClosed) + pill('non obsolète (≤18h)', c.NotObsolete) + '</div>';
    html += '<div class="verdict ' + (good?'ok':'ko') + '">' + esc(c.Verdict) + '</div>';
    var mem = (c.Members||[]).map(function(m){ return '<li class="' + (m.IsTarget?'target':'') + '">' + esc(m.Name) + ' <span class="muted">(' + esc(m.PersonnelId) + ')</span>' + (m.IsTarget?' ← ce personnel':'') + '</li>'; }).join('');
    html += '<ul class="members">' + (mem||'<li class="muted">aucun membre</li>') + '</ul>';
    html += '</div>';
  });

  if(!(d.Crews||[]).length){ html += '<p class="muted">Aucun crew candidat renvoyé par Orders.Api pour ce personnel à cette date.</p>'; }
  document.getElementById('out').innerHTML = html;
}
</script>
</body>
</html>
""";
}
