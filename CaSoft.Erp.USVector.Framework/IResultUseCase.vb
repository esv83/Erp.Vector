''' <summary>
''' Contrat cible d'un use case (Result pattern, cf. CLAUDE.md) : verbe utilisateur + méthode
''' <c>Handle</c> retournant un <see cref="ClResult(Of T)"/> typé (au lieu du legacy
''' <c>Execute(presenter)</c> à effet de bord et <c>Data As Object</c> non typé).
''' Un use case Result se branche dans la plomberie existante via <see cref="ClResultUseCaseAdapter(Of T)"/>.
''' </summary>
Public Interface IResultUseCase(Of T)
    Function Handle() As ClResult(Of T)
End Interface
