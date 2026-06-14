Public Class ClGJobValuePresenter

    Public Function Adapt(Job As ClJob) As List(Of ClAttributValueDto)
        Dim JobEditValueDto As New List(Of ClAttributValueDto)

        For Each attribut In Job.ContractType.Attributs
            JobEditValueDto.Add(New ClAttributValueDto(attribut.Key, attribut.Value.Type, attribut.Value.Value))

        Next

        Return JobEditValueDto

    End Function

End Class
