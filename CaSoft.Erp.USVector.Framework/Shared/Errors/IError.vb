Public Interface IError
    ReadOnly Property ErrorText As String
    ReadOnly Property Layer As String
    ReadOnly Property Exception As Exception
    ReadOnly Property HasException As Boolean
End Interface
