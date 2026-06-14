Public Class ClSetDriverUseCase
    Inherits ClUseCaseBase

    Private _repository As ICrewRepository
    Private _command As ClSetDriverCommand
    Public Sub New(Command As ClSetDriverCommand, Repository As ICrewRepository)
        _command = Command
        _repository = Repository

    End Sub

    Public Overrides sub execute(presenter as IResponseHandler)

        Try
            If CanExecute() Then

                Dim Crew As ClCrew = _repository.GetCrew(_command.CrewId)
                Dim employee = Crew.EmployeeList.SingleOrDefault(Function(f) f.Id = _command.DriverId)
                If employee IsNot Nothing Then
                    Dim lastDriver = New ClLastDriver(employee, DateTime.Now)
                    Crew.SetLastDriver(lastDriver)

                    _repository.Update(Crew)

                    Response.SetResult(True)
                Else
                    Response.AddError($"le salarié {_command.DriverId.ToString} ne fait pas parti de l'equipage {_command.CrewId} ")


                End If


            End If
        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try


    End Sub

    Public Overrides Sub Before()

    End Sub
End Class
