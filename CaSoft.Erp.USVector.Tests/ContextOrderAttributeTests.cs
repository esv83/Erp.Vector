using System.Net;
using System.Text;
using System.Text.Json;
using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Infrastructure.ErpApi;
using CaSoft.Erp.USVector.Infrastructure.Repositories.Erp;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// OC-5 — Le formulaire d'attributs et les valeurs saisies, servis par Order.
/// <para>
/// Ce que ces tests protègent : le <b>contrat mobile ne change pas</b> (mêmes champs, même parsing),
/// les deux ajouts qui portent le verrou <b>par champ</b> arrivent bien jusqu'au front, et une
/// écriture refusée par Order remonte en <b>issue métier</b> — jamais en exception, jamais en
/// écriture partielle.
/// </para>
/// </summary>
public class ContextOrderAttributeTests
{
    private const string BaseUrl = "https://api.urgencesante.net/order/";
    private static readonly Guid Mission = Guid.Parse("9f3ca1b2-0000-0000-0000-000000000001");

    /// <summary>
    /// Formulaire tel qu'Order le sert : volontairement désordonné, avec une DDN déjà connue de la
    /// fiche bénéficiaire (donc verrouillée) et une liste à options.
    /// </summary>
    private const string FormPayload = """
    [
      {
        "name": "NIR", "label": "N° sécurité sociale", "index": 20, "type": "text",
        "required": true, "instantUpdate": false, "placeHolder": "13 chiffres + clé",
        "isMulti": false, "options": null, "value": "",
        "isReadOnly": false, "readOnlyReason": null
      },
      {
        "name": "DDN", "label": "Date de naissance", "index": 10, "type": "date",
        "required": false, "instantUpdate": false, "placeHolder": null,
        "isMulti": false, "options": null, "value": "1954-03-02",
        "isReadOnly": true, "readOnlyReason": "Déjà renseignée sur la fiche du bénéficiaire"
      },
      {
        "name": "PMT", "label": "Prescription", "index": 30, "type": "list",
        "required": false, "instantUpdate": true, "placeHolder": null,
        "isMulti": false,
        "options": [ { "key": 0, "label": "Non" }, { "key": 1, "label": "Oui" } ],
        "value": "1",
        "isReadOnly": true, "readOnlyReason": "Document déjà récupéré à l'aller"
      }
    ]
    """;

    // ── Lecture ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// L'UI mobile ne change que d'URL : mêmes noms de propriétés, même ordre d'affichage piloté par
    /// l'index. Rien à réapprendre côté front.
    /// </summary>
    [Fact]
    public async Task Le_formulaire_arrive_trie_et_au_format_du_contrat_mobile()
    {
        var fields = await Build(new StubHandler(FormPayload)).GetFormStructureAsync(Mission, default);

        fields.Should().NotBeNull();
        fields!.Select(f => f.Name).Should().Equal(new[] { "DDN", "NIR", "PMT" }, "l'ordre vient de l'index");

        var nir = fields!.Single(f => f.Name == "NIR");
        nir.Label.Should().Be("N° sécurité sociale");
        nir.Type.Should().Be("text");
        nir.Required.Should().BeTrue();
        nir.PlaceHolder.Should().Be("13 chiffres + clé");
    }

    /// <summary>
    /// Le cœur d'OC-5 côté affichage : le verrou <b>par champ</b>. Une DDN déjà connue s'affiche mais
    /// ne se saisit plus — et le motif est là pour que l'ambulancier comprenne pourquoi, au lieu de
    /// buter sur un champ grisé sans explication.
    /// </summary>
    [Fact]
    public async Task Le_verrou_par_champ_et_son_motif_remontent_au_front()
    {
        var fields = await Build(new StubHandler(FormPayload)).GetFormStructureAsync(Mission, default);

        var ddn = fields!.Single(f => f.Name == "DDN");
        ddn.IsReadOnly.Should().BeTrue();
        ddn.ReadOnlyReason.Should().Be("Déjà renseignée sur la fiche du bénéficiaire");
        ddn.Value.Should().Be("1954-03-02", "un champ verrouillé s'affiche, il ne disparaît pas");

        var nir = fields!.Single(f => f.Name == "NIR");
        nir.IsReadOnly.Should().BeFalse();
        nir.ReadOnlyReason.Should().BeNull();
    }

    /// <summary>
    /// Les options ne sont servies que pour une liste — convention du modèle mobile. En servir un
    /// objet vide ailleurs ferait apparaître une clé que le front n'a jamais attendue.
    /// </summary>
    [Fact]
    public async Task Les_options_ne_sont_servies_que_pour_une_liste()
    {
        var fields = await Build(new StubHandler(FormPayload)).GetFormStructureAsync(Mission, default);

        fields!.Single(f => f.Name == "PMT").Options.Should().NotBeNull();
        fields!.Single(f => f.Name == "NIR").Options.Should().BeNull();
    }

    /// <summary>
    /// ⚠️ Les deux catalogues n'ont pas la même forme d'options : Order sert un <b>tableau</b>
    /// ordonné <c>[{key,label}]</c>, le contrat mobile attend un <b>objet</b>
    /// <c>{"0":"Non","1":"Oui"}</c> — la forme que le front parse depuis toujours.
    /// <para>
    /// Sans cette conversion, le miroir typé en dictionnaire faisait échouer la désérialisation : le
    /// <b>formulaire entier</b> tombait dès qu'une mission portait un seul attribut de type liste,
    /// pas seulement ce champ. Aucun contexte du catalogue de production n'en portait au moment de
    /// la bascule, ce qui rendait le défaut invisible — jusqu'au premier qu'on configurerait.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Les_options_sont_rendues_au_format_objet_attendu_par_le_front()
    {
        var fields = await Build(new StubHandler(FormPayload)).GetFormStructureAsync(Mission, default);

        var options = fields!.Single(f => f.Name == "PMT").Options as Dictionary<int, string>;

        options.Should().NotBeNull("un tableau [{key,label}] doit ressortir en objet clé → libellé");
        options!.Should().HaveCount(2);
        options[0].Should().Be("Non");
        options[1].Should().Be("Oui");
        options.Keys.Should().Equal(new[] { 0, 1 }, "l'ordre voulu par Order est conservé");
    }

    [Fact]
    public async Task Mission_inconnue_d_Order_ne_rend_pas_un_formulaire_vide()
    {
        var fields = await Build(new StubHandler(status: HttpStatusCode.NotFound))
            .GetFormStructureAsync(Mission, default);

        fields.Should().BeNull("un formulaire vide se lirait comme « rien à saisir », ce qui est faux");
    }

    // ── Écriture ────────────────────────────────────────────────────────────────

    /// <summary>
    /// On renvoie le formulaire entier sans trier les champs verrouillés : Order ignore une valeur
    /// reposée à l'identique. Trier ici obligerait à dupliquer sa règle de verrou, et à la voir
    /// dériver le jour où elle change.
    /// </summary>
    [Fact]
    public async Task L_ecriture_envoie_les_couples_nom_valeur_tels_quels()
    {
        var handler = new StubHandler(FormPayload);

        var outcome = await Build(handler).SaveValuesAsync(Mission, new List<ClAttributValueModel>
        {
            new() { AttributName = "NIR", AttributValue = "1540375116001" },
            new() { AttributName = "DDN", AttributValue = "1954-03-02" }
        }, "amb-42", default);

        outcome.Should().Be(EnContextOrderValuesOutcome.Applied);
        handler.PatchUri!.ToString().Should().Be($"{BaseUrl}missions/{Mission}/contextOrder/values");

        using var sent = JsonDocument.Parse(handler.PatchBody!);
        var values = sent.RootElement.GetProperty("values");
        values.GetArrayLength().Should().Be(2);
        values[0].GetProperty("name").GetString().Should().Be("NIR");
        values[0].GetProperty("value").GetString().Should().Be("1540375116001");
        sent.RootElement.GetProperty("setBy").GetString().Should().Be("amb-42");
    }

    /// <summary>
    /// Un lot vide ne vaut pas un aller-retour réseau — et surtout pas un refus : ne rien saisir
    /// n'est pas une erreur.
    /// </summary>
    [Fact]
    public async Task Un_lot_vide_n_appelle_pas_l_ERP()
    {
        var handler = new StubHandler(FormPayload);

        var outcome = await Build(handler).SaveValuesAsync(Mission, new List<ClAttributValueModel>(), null, default);

        outcome.Should().Be(EnContextOrderValuesOutcome.Applied);
        handler.PatchUri.Should().BeNull();
    }

    /// <summary>
    /// Les refus d'Order sont des cas métier, pas des pannes : ils remontent typés pour que le
    /// contrôleur les traduise en 409 / 400 / 404, et rien n'est enregistré à moitié.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Conflict, EnContextOrderValuesOutcome.FieldLocked)]
    [InlineData(HttpStatusCode.BadRequest, EnContextOrderValuesOutcome.Invalid)]
    [InlineData(HttpStatusCode.NotFound, EnContextOrderValuesOutcome.MissionNotFound)]
    public async Task Les_refus_de_l_ERP_remontent_en_issue_metier(
        HttpStatusCode status, EnContextOrderValuesOutcome expected)
    {
        var handler = new StubHandler(FormPayload, patchStatus: status);

        var outcome = await Build(handler).SaveValuesAsync(Mission, new List<ClAttributValueModel>
        {
            new() { AttributName = "DDN", AttributValue = "2099-01-01" }
        }, null, default);

        outcome.Should().Be(expected);
    }

    /// <summary>
    /// Une panne réelle (5xx) reste une exception : elle ne doit pas se déguiser en refus métier,
    /// sinon l'ambulancier lirait « saisie refusée » là où il n'y a qu'un serveur en vrac.
    /// </summary>
    [Fact]
    public async Task Une_panne_reelle_leve_au_lieu_de_se_deguiser_en_refus()
    {
        var handler = new StubHandler(FormPayload, patchStatus: HttpStatusCode.InternalServerError);

        var act = () => Build(handler).SaveValuesAsync(Mission, new List<ClAttributValueModel>
        {
            new() { AttributName = "NIR", AttributValue = "1540375116001" }
        }, null, default);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── Harnais ─────────────────────────────────────────────────────────────────

    private static ContextOrderAttributeService Build(StubHandler handler)
        => new(new HttpErpReadApiClient(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
                                        NullLogger<HttpErpReadApiClient>.Instance),
               new HttpErpWriteApiClient(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
                                         NullLogger<HttpErpWriteApiClient>.Instance));

    /// <summary>Répond au GET du formulaire et enregistre le PATCH — ou son absence.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _status;
        private readonly HttpStatusCode _patchStatus;

        public StubHandler(string body = "",
                           HttpStatusCode status = HttpStatusCode.OK,
                           HttpStatusCode patchStatus = HttpStatusCode.NoContent)
        {
            _body = body;
            _status = status;
            _patchStatus = patchStatus;
        }

        public Uri? PatchUri { get; private set; }
        public string? PatchBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Patch)
            {
                PatchUri = request.RequestUri;
                PatchBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
                return new HttpResponseMessage(_patchStatus) { Content = new StringContent("", Encoding.UTF8, "application/json") };
            }

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
        }
    }
}
