''' <summary>
''' OC-5 — Formulation <b>terrain</b> des verrous d'attributs, en remplacement de celle d'Order.
''' </summary>
''' <remarks>
''' <para>
''' <b>Pourquoi réécrire un message qu'Order formule déjà bien.</b> Order s'adresse d'abord à la
''' régulation, à qui il est utile de savoir que la fiche vient du référentiel et qu'elle s'y
''' corrige. L'ambulancier, lui, n'a accès ni à ce référentiel ni à personne qui l'administre :
''' lui nommer un système qu'il ne peut pas ouvrir ne lui dit pas quoi faire, et se lit comme une
''' panne. Ce module ne traduit donc pas le motif, il change de destinataire.
''' </para>
''' <para>
''' <b>Deux mécanismes, et c'est voulu.</b> Sur le formulaire, la substitution se décide sur le
''' <b>nom de l'attribut</b> — <c>DDN</c> et <c>NIR</c> sont les deux seuls adossés à la fiche
''' bénéficiaire, et leurs noms sont stables des deux côtés (scripts Order <c>057</c> et
''' <c>058</c>). Sur un refus d'écriture, le champ fautif n'est pas nommé dans la réponse : on ne
''' peut alors que reconnaître le motif, et le <b>reste passe intact</b> — « case déjà cochée » est
''' un message parfaitement lisible sur un brancard, et le perdre appauvrirait l'écran.
''' </para>
''' <para>
''' ⚠️ Le marqueur ci-dessous est un <b>couplage assumé</b> à la formulation d'Order. S'il cesse de
''' correspondre, le pire qui arrive est que le motif d'origine ressorte tel quel côté mobile —
''' d'où le test qui le fige.
''' </para>
''' </remarks>
Public Module ModTerrainLockWording

    ''' <summary>Ce que lit l'ambulancier à la place du motif d'Order.</summary>
    Public Const FicheLocked As String =
        "Fiche verrouillée par la facturation : cette information ne peut plus être modifiée depuis le terrain."

    ''' <summary>Attributs adossés à la fiche bénéficiaire, dont le verrou est toujours reformulé.</summary>
    Private ReadOnly FicheBackedAttributes As String() = {"DDN", "NIR"}

    ''' <summary>Fragment du motif d'Order qui nomme le référentiel, et ne doit pas atteindre le terrain.</summary>
    Private Const ReferentialMarker As String = "AidesNSoft"

    ''' <summary>
    ''' Motif à servir pour un champ du formulaire. Inchangé tant que le champ n'est pas verrouillé,
    ''' ou qu'il ne s'adosse pas à la fiche bénéficiaire.
    ''' </summary>
    Public Function ForField(attributeName As String, isReadOnly As Boolean, reason As String) As String
        If Not isReadOnly Then Return reason
        If Not IsFicheBacked(attributeName) Then Return Sanitize(reason)

        Return FicheLocked
    End Function

    ''' <summary>
    ''' Motif à servir pour un refus d'écriture « champ verrouillé ». Celui d'Order passe tel quel
    ''' sauf s'il nomme le référentiel ; absent, l'appelant reçoit Nothing et pose son propre libellé.
    ''' </summary>
    Public Function ForRefusal(reason As String) As String
        Return Sanitize(reason)
    End Function

    Private Function Sanitize(reason As String) As String
        If String.IsNullOrWhiteSpace(reason) Then Return reason
        If reason.IndexOf(ReferentialMarker, StringComparison.OrdinalIgnoreCase) < 0 Then Return reason

        Return FicheLocked
    End Function

    Private Function IsFicheBacked(attributeName As String) As Boolean
        If String.IsNullOrWhiteSpace(attributeName) Then Return False

        Return FicheBackedAttributes.Any(
            Function(n) String.Equals(n, attributeName.Trim(), StringComparison.OrdinalIgnoreCase))
    End Function

End Module
