''' <summary>
''' P3 — Ce que la lecture automatique <b>propose</b> pour une carte mutuelle. Quatre champs et une
''' confiance ; rien de plus, et surtout rien d'écrit d'autorité.
''' </summary>
''' <remarks>
''' <para>
''' <b>Proposé, jamais posé</b> (`M5`). Ces valeurs vivent dans des colonnes à part, que l'opérateur
''' voit à côté de la saisie officielle. C'est sa validation qui recopie — le même geste que la saisie
''' manuelle d'aujourd'hui. Un code AMC lu sur une photo et écrit d'autorité en facturation, ce serait
''' l'inverse de ce que ce module promet : le terrain n'écrase jamais la donnée officielle (D2).
''' </para>
''' <para>
''' <b>La confiance voyage avec les champs</b>, et elle est journalisée. Un opérateur qui lit
''' « confiance 0,4 » ne valide pas comme à 0,95 ; sans elle, les deux se ressemblent.
''' </para>
''' </remarks>
Public Class ClMutuelleCardOcrProposal

    Public Property MutuelleName As String
    Public Property AmcCode As String
    Public Property Concentrateur As String
    Public Property Teletransmission As String

    ''' <summary>Confiance rendue par le modèle, de 0 à 1. Nothing s'il n'en a pas donné.</summary>
    Public Property Confidence As Decimal?

    ''' <summary>Vrai si au moins un des quatre champs a été lu : sinon, il n'y a rien à proposer.</summary>
    Public ReadOnly Property HasValue As Boolean
        Get
            Return Not (String.IsNullOrWhiteSpace(MutuelleName) AndAlso
                        String.IsNullOrWhiteSpace(AmcCode) AndAlso
                        String.IsNullOrWhiteSpace(Concentrateur) AndAlso
                        String.IsNullOrWhiteSpace(Teletransmission))
        End Get
    End Property

End Class
