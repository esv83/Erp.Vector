Public Class ClUpdateJobEditUseCase
    Inherits ClUseCaseBase

    Private _command As ClUpdateJobEditCommand
    Private _cache As IJobCache
    Private _repository As IJobRepository

    Public Sub New(command As ClUpdateJobEditCommand, cache As IJobCache, repository As IJobRepository)

        _command = command
        _cache = cache
        _repository = repository

    End Sub
    Public Overrides Sub execute(presenter As IResponseHandler)
        If CanExecute() Then

            Try
                Dim job = _cache.GetJob(_command.JobID)

                For Each attribut In _command.NewAttributsValues
                    job.UpdateAttribute(attribut.AttributName, attribut.AttributValue)
                Next

                _repository.Save(job)

                Response.SetResult(_command.NewAttributsValues)
                '  Response.SetResult(True)

            Catch ex As Exception
                Response.AddError(ex.Message)
            Finally
                presenter.Handle(Response)
            End Try

        End If



        '        [
        '  {
        '    "attributName": "CONTRACT",
        '    "attributValue": "5"
        '  },
        '  {
        '    "attributName": "PHONES",
        '    "attributValue": "[\"0102030405\",\"0203040506\"]"
        '  },
        '  {
        '    "attributName": "MAILS",
        '    "attributValue": "[\"mail@mail.com\"]"
        '  },
        '  {
        '    "attributName": "COMMENTS",
        '    "attributValue": "Un commentaire"
        '  }
        ']

    End Sub

    Public Overrides Sub Before()
    End Sub


#Region "Adapter"
    Private Class ClContactDtoAdapter
        Inherits ClUpdateContactDto

        Private _command As ClUpdateJobEditCommand

        Public Sub New(gContactId As Guid, command As ClUpdateJobEditCommand)
            Me._command = command

            With Me
                .ContactID = gContactId
                '.NIR = _command.NIR
                '.DDN = _command.DDN
                '.ContTel1 = GetItemFromListByIndex(_command.Phones, 1)
                '.ContTel2 = GetItemFromListByIndex(_command.Phones, 2)
                '.ContTel3 = GetItemFromListByIndex(_command.Phones, 3)
                '.ContMail = GetItemFromListByIndex(_command.Emails, 1)
            End With

        End Sub

        Private Function GetItemFromListByIndex(list As List(Of String), intIndex As Integer) As String
            Dim result As Object = String.Empty


            If (list.Count >= intIndex) Then
                result = list(intIndex - 1)

            End If


            Return result

        End Function

    End Class
    Private Class ClCommandeDtoAdapter
        Inherits ClUpdateCommandeDto

        Private command As ClUpdateJobEditCommand

        Public Sub New(command As ClUpdateJobEditCommand)
            Me.command = command

            With Me
                ' .Comments = command.Comments
                .IsPmtPresent = IsPmtPresent
                .Reference = Reference
            End With

        End Sub

    End Class
    Private Class ClCommandeAttributDtoAdapter
        Inherits List(Of ClContractAttribut)
        Public Sub New(command As ClUpdateJobEditCommand)
            ' Me.AddRange(command.Values)
        End Sub

    End Class

#End Region


End Class
