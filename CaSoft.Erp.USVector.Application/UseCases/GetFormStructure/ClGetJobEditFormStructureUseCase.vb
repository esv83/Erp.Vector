
Public Class ClGetJobEditFormStructureUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _repository As IJobRepository
    Private _query As Guid
    ' Private _cache As IJobCache

    Public Sub New(gId As Guid, repository As IJobRepository)

        _query = gId
        ' _cache = cache
        _repository = repository

    End Sub
    Public Overrides sub execute(presenter As IResponseHandler) Implements IUseCase.Execute

        If CanExecute() Then
            Try
                'Dim job = _cache.GetJob(_query)
                Dim job = _repository.GetJob(_query)

                Dim fieldList As New List(Of ClMobileAppFieldModel)

                For Each attribut In job.ContractType.Attributs

                    Dim field As New ClMobileAppFieldModel With {
                                   .Name = attribut.Key,
                                   .Label = attribut.Value.Label,
                                 .Index = attribut.Value.Index,
                                 .Type = attribut.Value.FieldType,
                                 .Required = attribut.Value.Required,
                                 .PlaceHolder = attribut.Value.PlaceHolder,
                                 .InstantUpdate = attribut.Value.InstantUpdate,
                                 .IsMulti = attribut.Value.IsMulti,
                    .Value = attribut.Value.Value
                    }

                    ' Type = 'list' : on fournit au dev web les valeurs qui remplissent la liste.
                    If attribut.Value.IsList Then
                        field.Options = attribut.Value.Options
                    End If

                    fieldList.Add(field)

                Next

                Response.SetResult(fieldList)

            Catch ex As Exception

                Response.AddError(ex.Message)
            Finally
                presenter.Handle(Response)

            End Try

        End If

    End Sub

    Public Overrides Sub Before()

    End Sub
End Class
