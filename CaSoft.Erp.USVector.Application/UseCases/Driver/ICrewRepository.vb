Imports System.Threading
Imports CaSoft.Erp.USVector.Application.Dto

Namespace Port

    ''' <summary>
    ''' Équipage et missions du terrain, lus chez Orders. Asynchrone depuis le 04/10 ; les membres
    ''' jamais implémentés (conducteur par véhicule, instructions, équipages d'une date) sont partis
    ''' avec les cas d'usage qui ne les appelaient pas.
    ''' </summary>
    Public Interface ICrewRepository

        ''' <summary>L'équipage, ou <c>Nothing</c> s'il est inconnu d'Orders.</summary>
        Function GetCrewAsync(gCrewID As Guid, ct As CancellationToken) As Task(Of ClCrew)

        ''' <summary>
        ''' MOB-4a — Union des missions de plusieurs crews (un personnel peut être membre de
        ''' plusieurs crews actifs le même jour). Dédupliquée.
        ''' </summary>
        Function FetchJobListAsync(gCrewIds As IReadOnlyCollection(Of Guid), ct As CancellationToken) As Task(Of List(Of ClJobListItemModel))

        ''' <summary>Instructions de la régulation : pas d'équivalent chez Orders, liste vide.</summary>
        Function FetchInstructionList(gCrewId As Guid) As List(Of ClInstructionListItemModel)

        ''' <summary>
        ''' Enregistre le dernier conducteur désigné de l'équipage. Un refus de l'ERP est rendu, avec
        ''' son motif, et ne lève pas ; seule une panne réelle lève.
        ''' </summary>
        Function UpdateAsync(crew As ClCrew, ct As CancellationToken) As Task(Of ClCrewDriverWriteResult)

    End Interface

End Namespace
