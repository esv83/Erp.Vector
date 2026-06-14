Public Class ClGetJobEditPresenter


    Public Function Adapt(job As ClJob) As List(Of ClAttributValueDto)
        Dim JobEditValue As New List(Of ClAttributValueDto) From {
            New ClAttributValueDto("NIR", "String", job.Beneficiary.NIR),
            New ClAttributValueDto("DDN", "Date", job.Beneficiary.DDN),
            New ClAttributValueDto("Mails", "String", job.Beneficiary.Emails),
            New ClAttributValueDto("Phones", "String", job.Beneficiary.Phones),
            New ClAttributValueDto("Comments", "String", job.Comments),
            New ClAttributValueDto("SelectedContractType", "Integer", job.ContractType.Id)
        }

        Return JobEditValue

    End Function

End Class
