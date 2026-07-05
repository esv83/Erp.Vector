' Désignation du conducteur d'un équipage — Result pattern.
Public Class ClSetDriverUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _repository As ICrewRepository
    Private ReadOnly _command As ClSetDriverCommand

    Public Sub New(Command As ClSetDriverCommand, Repository As ICrewRepository)
        _command = Command
        _repository = Repository
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            Dim Crew As ClCrew = _repository.GetCrew(_command.CrewId)
            Dim employee = Crew.EmployeeList.SingleOrDefault(Function(f) f.Id = _command.DriverId)
            If employee Is Nothing Then
                Return ClResult(Of Boolean).Fail(
                    ClError.Application($"le salarié {_command.DriverId.ToString} ne fait pas parti de l'equipage {_command.CrewId} "))
            End If

            Dim lastDriver = New ClLastDriver(employee, DateTime.Now)
            Crew.SetLastDriver(lastDriver)
            _repository.Update(Crew)
            Return ClResult(Of Boolean).Ok(True)

        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
