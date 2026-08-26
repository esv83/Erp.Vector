' Mise à jour des attributs d'édition d'une mission — Result pattern.
Public Class ClUpdateJobEditUseCase
    Implements IResultUseCase(Of List(Of ClAttributValueModel))

    Private ReadOnly _command As ClUpdateJobEditCommand
    Private ReadOnly _cache As IJobCache
    Private ReadOnly _repository As IJobRepository

    Public Sub New(command As ClUpdateJobEditCommand, cache As IJobCache, repository As IJobRepository)
        _command = command
        _cache = cache
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of List(Of ClAttributValueModel)) Implements IResultUseCase(Of List(Of ClAttributValueModel)).Handle

        Try
            Dim job = _cache.GetJob(_command.JobID)

            ' Validation : rejeter les attributs hors du contrat courant plutôt que de
            ' les ignorer silencieusement (UpdateAttribute est un no-op sur clé absente).
            Dim unknown = _command.NewAttributsValues _
                .Where(Function(a) Not job.ContractType.Attributs.ContainsKey(a.AttributName)) _
                .Select(Function(a) a.AttributName) _
                .ToList()

            If unknown.Count > 0 Then
                Return ClResult(Of List(Of ClAttributValueModel)).Fail(
                    ClError.Application("Attribut(s) non applicable(s) au contrat de la mission : " & String.Join(", ", unknown)))
            End If

            For Each attribut In _command.NewAttributsValues
                job.UpdateAttribute(attribut.AttributName, attribut.AttributValue)
            Next

            _repository.Save(job)

            Return ClResult(Of List(Of ClAttributValueModel)).Ok(_command.NewAttributsValues)

        Catch ex As Exception
            Return ClResult(Of List(Of ClAttributValueModel)).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
