
' Structure du formulaire d'édition (attributs du contrat de la mission) — Result pattern.
Public Class ClGetJobEditFormStructureUseCase
    Implements IResultUseCase(Of List(Of ClMobileAppFieldModel))

    Private ReadOnly _repository As IJobRepository
    Private ReadOnly _query As Guid

    Public Sub New(gId As Guid, repository As IJobRepository)
        _query = gId
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of List(Of ClMobileAppFieldModel)) Implements IResultUseCase(Of List(Of ClMobileAppFieldModel)).Handle

        Try
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

            Return ClResult(Of List(Of ClMobileAppFieldModel)).Ok(fieldList)

        Catch ex As Exception
            Return ClResult(Of List(Of ClMobileAppFieldModel)).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
