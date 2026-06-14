Imports System.Text

Public Class ClUseCaseResponseBase
    Implements IUseCaseResponse
    Implements IResponseHandler

    Protected _data As Object
    Protected _isSuccess As Boolean
    Protected _errorList As List(Of String)

    Public Sub New()
        _errorList = New List(Of String)
    End Sub

    Public ReadOnly Property IsSuccess As Boolean Implements IUseCaseResponse.IsSuccess
        Get
            Return _errorList.Count = 0
        End Get
    End Property
    Public ReadOnly Property HasResult As Boolean Implements IUseCaseResponse.HasResult
        Get
            Return _data IsNot Nothing
        End Get
    End Property

    Public ReadOnly Property HasError As Boolean Implements IUseCaseResponse.HasError
        Get
            Return _errorList.Count > 0
        End Get
    End Property

    Public ReadOnly Property ErrorText As String Implements IUseCaseResponse.ErrorText
        Get
            Dim sb As New StringBuilder
            If _errorList.Count > 0 Then
                sb.AppendLine(String.Format("{0} erreur(s)", _errorList.Count.ToString))
            End If
            For Each item In _errorList
                sb.AppendLine(item)
            Next

            If sb.Length = 0 Then
                sb.Append("pas d'erreurs")
            End If

            Return sb.ToString

        End Get
    End Property

    Public ReadOnly Property Data As Object Implements IUseCaseResponse.Data
        Get
            Return _data
        End Get

    End Property



    Public Sub SetResult(data As Object) Implements IUseCaseResponse.SetResult
        _data = data
    End Sub

    Public Sub AddError(errorText As String) Implements IUseCaseResponse.AddError
        _errorList.Add(errorText)
    End Sub
    Public Sub AddError(ex As Exception) Implements IUseCaseResponse.AddError
        _errorList.Add(ex.Message)
    End Sub

    Public Sub Handle(response As ClUseCaseResponseBase) Implements IResponseHandler.Handle
        ' sert pour rendre compatible une response en handler
    End Sub
End Class
