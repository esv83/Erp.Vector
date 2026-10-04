Imports System.Threading

''' <summary>
''' Contrat d'un use case asynchrone : <c>HandleAsync</c> retournant un <see cref="ClResult(Of T)"/>.
''' <para>
''' <b>Seul le refus métier devient un <c>Fail</c></b> (règle du 04/10, la même qu'Orders le 16/09) :
''' une panne — Orders à terre, base injoignable — <b>remonte</b>, et c'est le gestionnaire de l'API qui
''' la rend en 503. Attraper toute exception pour la rendre en 400 faisait accuser l'ambulancier d'une
''' panne : ~230 réponses ainsi pendant les coupures des 22/09 et 02/10.
''' </para>
''' </summary>
Public Interface IResultUseCaseAsync(Of T)
    Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of T))
End Interface
