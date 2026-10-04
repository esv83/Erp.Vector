Imports System.Threading

' Désignation du conducteur d'un équipage — Result pattern.
Public Class ClSetDriverUseCase
    Implements IResultUseCaseAsync(Of Boolean)

    ''' <summary>Libellé d'un refus d'Orders sans motif lisible.</summary>
    Public Const RefusalFallback As String = "Changement de conducteur refusé par la régulation."

    Private ReadOnly _repository As ICrewRepository
    Private ReadOnly _command As ClSetDriverCommand

    Public Sub New(Command As ClSetDriverCommand, Repository As ICrewRepository)
        _command = Command
        _repository = Repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of Boolean)) Implements IResultUseCaseAsync(Of Boolean).HandleAsync

        Dim Crew As ClCrew = Await _repository.GetCrewAsync(_command.CrewId, ct)
        If Crew Is Nothing Then
            Return ClResult(Of Boolean).Fail(ClError.Application($"Équipage {_command.CrewId} introuvable côté ERP."))
        End If

        Dim employee = Crew.EmployeeList.SingleOrDefault(Function(f) f.Id = _command.DriverId)
        If employee Is Nothing Then
            Return ClResult(Of Boolean).Fail(
                ClError.Application($"le salarié {_command.DriverId.ToString} ne fait pas parti de l'equipage {_command.CrewId} "))
        End If

        Dim lastDriver = New ClLastDriver(employee, DateTime.Now)
        Crew.SetLastDriver(lastDriver)
        ' Un refus d'Orders part tel quel : son motif est la seule phrase qui dise à l'ambulancier
        ' pourquoi. Même code HTTP qu'avant (400, erreur applicative), seul le texte change (D14).
        Dim written = Await _repository.UpdateAsync(Crew, ct)
        If Not written.IsApplied Then
            Return ClResult(Of Boolean).Fail(
                ClError.Application(If(written.HasReason, written.Reason, RefusalFallback)))
        End If

        Return ClResult(Of Boolean).Ok(True)

    End Function

End Class
