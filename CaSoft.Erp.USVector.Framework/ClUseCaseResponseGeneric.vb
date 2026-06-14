Imports System.Text

' Variante générique de IUseCaseResponse / ClUseCaseResponseBase, attendue par le code
' porté de MobApp.Application (ICrewService, ClCrewService, ...). Le framework legacy
' la fournissait ; la V2 copiée n'avait conservé que la version non générique.

Public Interface IUseCaseResponse(Of T)

    Sub SetResult(data As T)
    Sub AddError(strErrorText As String)
    Sub AddError(ex As Exception)
    ReadOnly Property HasError() As Boolean
    ReadOnly Property IsSuccess As Boolean
    ReadOnly Property HasResult As Boolean
    ReadOnly Property Data As T
    ReadOnly Property Result As T
    ReadOnly Property ErrorText As String

End Interface

Public Class ClUseCaseResponse(Of T)
    Implements IUseCaseResponse(Of T)

    Protected _data As T
    Protected _hasResult As Boolean
    Protected _errorList As New List(Of String)

    Public ReadOnly Property IsSuccess As Boolean Implements IUseCaseResponse(Of T).IsSuccess
        Get
            Return _errorList.Count = 0
        End Get
    End Property

    Public ReadOnly Property HasResult As Boolean Implements IUseCaseResponse(Of T).HasResult
        Get
            Return _hasResult
        End Get
    End Property

    Public ReadOnly Property HasError As Boolean Implements IUseCaseResponse(Of T).HasError
        Get
            Return _errorList.Count > 0
        End Get
    End Property

    Public ReadOnly Property ErrorText As String Implements IUseCaseResponse(Of T).ErrorText
        Get
            Dim sb As New StringBuilder
            If _errorList.Count > 0 Then
                sb.AppendLine(String.Format("{0} erreur(s)", _errorList.Count.ToString))
            End If
            For Each strError In _errorList
                sb.AppendLine(strError)
            Next

            If sb.Length = 0 Then
                sb.Append("pas d'erreurs")
            End If

            Return sb.ToString
        End Get
    End Property

    Public ReadOnly Property Data As T Implements IUseCaseResponse(Of T).Data
        Get
            Return _data
        End Get
    End Property

    ' Alias legacy de Data (le code porté utilise les deux noms).
    Public ReadOnly Property Result As T Implements IUseCaseResponse(Of T).Result
        Get
            Return _data
        End Get
    End Property

    Public Sub SetResult(data As T) Implements IUseCaseResponse(Of T).SetResult
        _data = data
        _hasResult = True
    End Sub

    Public Sub AddError(strErrorText As String) Implements IUseCaseResponse(Of T).AddError
        _errorList.Add(strErrorText)
    End Sub

    Public Sub AddError(ex As Exception) Implements IUseCaseResponse(Of T).AddError
        _errorList.Add(ex.Message)
    End Sub

End Class
