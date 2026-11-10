Imports System.Threading

Namespace Port

    ''' <summary>
    ''' Motif « le patient ne peut pas signer » (MOB_012) : la liste des motifs et la déclaration par
    ''' mission. Toute écriture arme la projection vers Order dans sa propre transaction (PRJ-1).
    ''' </summary>
    Public Interface ISignatureWaiverRepository

        ''' <summary>Motifs actifs, dans l'ordre d'affichage.</summary>
        Function ListActiveReasonsAsync(ct As CancellationToken) As Task(Of IReadOnlyList(Of ClSignatureWaiverReason))

        ''' <summary>Un motif de la liste, actif ou non ; <c>Nothing</c> s'il n'existe pas.</summary>
        Function GetReasonAsync(code As String, ct As CancellationToken) As Task(Of ClSignatureWaiverReason)

        ''' <summary>Le motif déclaré pour la mission ; <c>Nothing</c> s'il n'y en a pas.</summary>
        Function GetAsync(missionId As Guid, ct As CancellationToken) As Task(Of ClSignatureWaiver)

        ''' <summary>Pose ou remplace le motif de la mission.</summary>
        Function SaveAsync(waiver As ClSignatureWaiver, ct As CancellationToken) As Task

        ''' <summary>Retire le motif de la mission. Sans effet s'il n'y en a pas.</summary>
        Function DeleteAsync(missionId As Guid, ct As CancellationToken) As Task

    End Interface

End Namespace
