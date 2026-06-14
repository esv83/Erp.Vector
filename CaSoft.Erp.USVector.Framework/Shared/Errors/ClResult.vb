Imports System.Text

Public Class ClResult
    Implements IResult

    Private _innerError As IError
    Protected Sub New(Optional [error] As IError = Nothing)
        _innerError = [error]

        If [error] IsNot Nothing Then _innerError = [error]

    End Sub
    Public ReadOnly Property IsFail As Boolean Implements IResult.IsFail
        Get
            Return _innerError IsNot Nothing
        End Get
    End Property
    Public ReadOnly Property IsSucces As Boolean Implements IResult.IsSucces
        Get
            Return Not IsFail
        End Get
    End Property

    Public ReadOnly Property InnerError As IError Implements IResult.InnerError
        Get
            Return _innerError
        End Get

    End Property

    Public Shared Function Ok() As ClResult
        Return New ClResult()
    End Function
    Public Shared Function Fail(pError As IError) As ClResult

        Return New ClResult(pError)

    End Function


End Class

Public Class ClResult(Of T)
    Inherits ClResult

    Protected Sub New([error] As IError, Optional suffixe As String = "")
        MyBase.New([error])
    End Sub
    Private Sub New(value As T)
        MyBase.New()

        _Value = value
    End Sub

    Public ReadOnly Property Value As T
    Public Overloads Shared Function Ok(Value As T) As ClResult(Of T)

        Return New ClResult(Of T)(Value)

    End Function
    Public Overloads Shared Function Fail(pError As IError) As ClResult(Of T)

        Return New ClResult(Of T)(pError)
    End Function


End Class
