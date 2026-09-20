using CaSoft.Erp.USVector.Api.Controllers;
using CaSoft.Erp.USVector.Api.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CaSoft.Erp.USVector.Tests;

/// <summary>
/// G8 — « Quel code tourne, et sur quelle base ? ». Ces tests protègent les deux promesses de la
/// route : elle dit l'**état de l'arbre** au build, et elle ne laisse **jamais** filtrer un secret.
/// </summary>
public class VersionRouteTests
{
    private const string ChaineDeProd =
        "Server=192.168.1.109,1440;Database=BD_ERP_MOBILE_APP;User Id=ErpAccount;Password=tr3s-secret;TrustServerCertificate=True";

    [Fact]
    public void La_base_resolue_nomme_le_serveur_et_la_base()
    {
        var (server, database) = VersionController.ResolveDatabase(ChaineDeProd);

        server.Should().Be("192.168.1.109,1440");
        database.Should().Be("BD_ERP_MOBILE_APP");
    }

    /// <summary>
    /// Le 25/08, un correctif est parti sur la mauvaise base sans une erreur : c'est pour cela que la
    /// base se nomme. Mais **rien d'autre** de la chaîne ne doit sortir — surtout pas le mot de passe.
    /// </summary>
    [Fact]
    public void Aucun_identifiant_ne_sort_de_la_chaine_de_connexion()
    {
        var (server, database) = VersionController.ResolveDatabase(ChaineDeProd);

        string.Join('|', server, database).Should()
            .NotContain("tr3s-secret").And.NotContain("ErpAccount").And.NotContain("Password");
    }

    [Theory]
    [InlineData("Data Source=srv;Initial Catalog=BD;User Id=u;Password=p", "srv", "BD")]
    [InlineData("Server=srv;Database=BD", "srv", "BD")]
    public void Les_deux_ecritures_de_la_chaine_sont_lues(string chaine, string serveur, string baseAttendue)
    {
        VersionController.ResolveDatabase(chaine).Should().Be((serveur, baseAttendue));
    }

    /// <summary>
    /// Une chaîne absente ou illisible rend « je ne sais pas ». Lever ici rendrait muette la seule
    /// route qu'on interroge justement quand quelque chose cloche.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ceci n'est pas une chaîne de connexion")]
    public void Une_chaine_absente_ou_illisible_ne_leve_pas(string? chaine)
    {
        VersionController.ResolveDatabase(chaine).Should().Be((null, null));
    }

    /// <summary>
    /// Le `.pdb` donne le `HEAD` du build ; il ne dit pas si des fichiers non commités ont été
    /// compilés avec. `Tree` répond à cette question, et vaut l'une des trois valeurs attendues.
    /// </summary>
    [Fact]
    public void L_etat_de_l_arbre_au_build_est_l_une_des_trois_valeurs()
        => BuildInfo.Tree.Should().BeOneOf("clean", "modified", "unknown");

    /// <summary>
    /// Le commit est soit un vrai SHA, soit vide (build sans git) — jamais un fragment d'autre chose.
    /// Orders sert un `shortCommit` qui vaut « build.2 » pour avoir coupé un suffixe horodaté.
    /// </summary>
    [Fact]
    public void Le_commit_est_un_sha_complet_ou_rien()
    {
        BuildInfo.Commit.Should().Match(c => c.Length == 0 || c.Length == 40);
        BuildInfo.ShortCommit.Should().Be(BuildInfo.Commit.Length >= 7 ? BuildInfo.Commit[..7] : BuildInfo.Commit);
    }
}
