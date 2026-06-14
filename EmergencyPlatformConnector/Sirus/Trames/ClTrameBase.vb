Imports NLog

Public MustInherit Class ClTrameBase

    Friend _trameData(1) As Byte

    Public Sub New(intTrameLength As Integer)
        ReDim _trameData(intTrameLength)

    End Sub

    Public ReadOnly Property Id As Integer
        Get
            Return GetIdFromTrame()
        End Get
    End Property
    Friend Function GetIdFromTrame() As Integer
        Dim result As Integer
        'Todo Factoriser avec La trame Acknowledge

        Dim IdByteArray(3) As Byte
        For i = 0 To 3
            IdByteArray(i) = _trameData(i)
        Next

        result = BitConverter.ToInt32(IdByteArray, 0)


        Return result

    End Function

    Public Function ContentAsText() As String
        Return Text.ASCIIEncoding.ASCII.GetString(_trameData)
    End Function
End Class
