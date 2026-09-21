''' <summary>
''' P3 — Ce qu'il advient d'une carte après un échec de lecture : combien de tentatives, et si la file
''' l'abandonne.
''' </summary>
''' <remarks>
''' Le décompte est tenu par le dépôt, là où la ligne est. L'appelant qui le relirait ensuite verrait
''' une valeur déjà périmée si une autre instance lisait la même carte.
''' </remarks>
Public Class ClOcrFailureOutcome

    ''' <summary>Tentatives après celle-ci.</summary>
    Public Property Attempts As Integer

    ''' <summary>Vrai quand la carte passe en <c>error</c> : elle ne sera plus relue.</summary>
    Public Property GaveUp As Boolean

End Class
