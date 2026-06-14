Public MustInherit Class ClQueryBase

    Protected _IsValid As Boolean
    Protected _ErrorText As String
    Public MustOverride Sub Valid(dte As Object)

    Public ReadOnly Property IsValid As Boolean
        Get
            Return _IsValid
        End Get
    End Property
    Public ReadOnly Property ErrorText As String
        Get
            Return _ErrorText
        End Get
    End Property

End Class
