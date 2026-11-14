Public Class ClAttributValueModel

    Public Property AttributName As String
    Public Property AttributValue As Object

    ''' <summary>
    ''' J-154 — Texte libre quand la clé choisie est une option <c>TextRequired</c> (« Autre »).
    ''' Additif : absent, rien ne change pour les clients actuels. Relayé à Order tel quel.
    ''' </summary>
    Public Property OtherText As String

End Class
