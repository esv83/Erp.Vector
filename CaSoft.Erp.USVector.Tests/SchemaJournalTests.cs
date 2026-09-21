using CaSoft.Erp.USVector.Infrastructure.Persistence.Schema;
using FluentAssertions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// G4 — Le suivi des migrations SQL. Prod et dev ont divergé en sens inverse le 06/08 : 500 opaque et
/// une journée sans données terrain, sans que rien ne dise quel script manquait.
/// </summary>
public class SchemaJournalTests
{
    private static AppliedScript Applique(string id, string origine = "script")
        => new(id, new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero), origine);

    // ── Le catalogue ──────────────────────────────────────────────────────────

    /// <summary>
    /// Le catalogue se lit dans l'assemblage, pas dans une liste écrite à la main : un script ajouté
    /// demain est attendu sans que personne y pense.
    /// </summary>
    [Fact]
    public void Le_catalogue_vient_des_scripts_embarques()
    {
        SchemaCatalogue.Attendus.Should().Contain("MOB_001_Initial")
            .And.Contain("MOB_009_SchemaJournal")
            .And.BeInAscendingOrder(StringComparer.Ordinal);
    }

    /// <summary>
    /// `MOB_008` a supprimé ce que `MOB_002` et `MOB_007` posaient : réclamer un script dont l'effet a
    /// été défait ne ferait qu'un signal faux de plus.
    /// </summary>
    [Fact]
    public void Les_scripts_annules_ne_sont_plus_attendus()
    {
        SchemaCatalogue.Attendus.Should().NotContain("MOB_002_JobAttributes")
            .And.NotContain("MOB_007_FixArt80Display");
        SchemaCatalogue.Annules.Keys.Should().BeEquivalentTo("MOB_002_JobAttributes", "MOB_007_FixArt80Display");
    }

    // ── Le verdict ────────────────────────────────────────────────────────────

    [Fact]
    public void Une_base_qui_porte_tous_les_scripts_est_a_jour()
    {
        var etat = SchemaState.From(SchemaCatalogue.Attendus.Select(id => Applique(id)).ToList());

        etat.UpToDate.Should().BeTrue();
        etat.Missing.Should().BeEmpty();
        etat.LastApplied.Should().Be(SchemaCatalogue.Attendus.Last());
        etat.Statement().Should().Contain("à jour");
    }

    [Fact]
    public void Un_script_absent_est_nomme()
    {
        var sansLeDernier = SchemaCatalogue.Attendus.Take(SchemaCatalogue.Attendus.Count - 1)
            .Select(id => Applique(id)).ToList();

        var etat = SchemaState.From(sansLeDernier);

        etat.UpToDate.Should().BeFalse();
        etat.Missing.Should().ContainSingle().Which.Should().Be(SchemaCatalogue.Attendus.Last());
        etat.Statement().Should().Contain("MANQUANTS").And.Contain(SchemaCatalogue.Attendus.Last());
    }

    /// <summary>
    /// Le 06/08, la divergence allait dans les deux sens : une base peut aussi être **en avance** sur
    /// le dépôt. C'est une anomalie, pas un détail — le binaire déployé ne connaît pas ce script.
    /// </summary>
    [Fact]
    public void Une_base_en_avance_sur_le_depot_est_signalee()
    {
        var etat = SchemaState.From(SchemaCatalogue.Attendus.Select(id => Applique(id))
            .Append(Applique("MOB_042_VenuDuFutur")).ToList());

        etat.UpToDate.Should().BeFalse();
        etat.Unknown.Should().ContainSingle().Which.Should().Be("MOB_042_VenuDuFutur");
        etat.Statement().Should().Contain("INCONNUS");
    }

    /// <summary>
    /// Une base ancienne porte encore la trace de `MOB_002` et `MOB_007` : ils ont vraiment été joués.
    /// Ni manquants, ni inconnus — sans quoi toute base d'avant le 13/09 crierait au loup.
    /// </summary>
    [Fact]
    public void Les_scripts_annules_inscrits_ne_sont_ni_manquants_ni_inconnus()
    {
        var etat = SchemaState.From(SchemaCatalogue.Attendus.Select(id => Applique(id))
            .Append(Applique("MOB_002_JobAttributes", "constat"))
            .Append(Applique("MOB_007_FixArt80Display", "constat")).ToList());

        etat.UpToDate.Should().BeTrue();
        etat.Unknown.Should().BeEmpty();
        etat.Missing.Should().BeEmpty();
    }

    [Fact]
    public void Une_base_sans_journal_dit_quoi_jouer()
    {
        var etat = SchemaState.WithoutJournal();

        etat.UpToDate.Should().BeFalse();
        etat.Statement().Should().Contain("__VectorSchema est ABSENTE").And.Contain("MOB_009");
    }

    /// <summary>
    /// Base injoignable : le schéma n'est ni bon ni mauvais, il est **inconnu**. Et le détail SQL, qui
    /// nomme le compte de connexion, ne sort jamais de la phrase servie.
    /// </summary>
    [Fact]
    public void Une_base_injoignable_ne_divulgue_pas_le_detail_sql()
    {
        var etat = SchemaState.Unreachable("Login failed for user 'ErpAccount'.");

        etat.UpToDate.Should().BeFalse();
        etat.Statement().Should().Contain("injoignable").And.NotContain("ErpAccount");
        etat.Error.Should().Contain("ErpAccount");   // journalisé, jamais servi
    }

    [Fact]
    public async Task Sans_chaine_de_connexion_la_lecture_rend_injoignable_sans_lever()
    {
        var etat = await new SchemaJournal(null).ReadAsync();

        etat.Reachable.Should().BeFalse();
        etat.Error.Should().Contain("aucune chaîne de connexion");
    }
}
