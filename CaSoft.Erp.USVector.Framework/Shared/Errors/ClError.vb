''' <summary>
''' Implémentation concrète de <see cref="IError"/> pour le Result pattern.
''' Porte un message, une couche d'origine (diagnostic) et éventuellement l'exception source.
''' Le drapeau <see cref="IsNotFound"/> distingue « ressource introuvable » (→ 404) d'une erreur
''' métier (→ 400) côté mapping HTTP. Instanciation via les fabriques (Domain/Application/Infrastructure/NotFound).
''' </summary>
Public Class ClError
    Implements IError

    Private ReadOnly _errorText As String
    Private ReadOnly _layer As ErrorLayer
    Private ReadOnly _exception As Exception
    Private ReadOnly _isNotFound As Boolean

    Private Sub New(errorText As String, layer As ErrorLayer,
                    Optional ex As Exception = Nothing, Optional isNotFound As Boolean = False)
        _errorText = errorText
        _layer = layer
        _exception = ex
        _isNotFound = isNotFound
    End Sub

    Public ReadOnly Property ErrorText As String Implements IError.ErrorText
        Get
            Return _errorText
        End Get
    End Property

    ''' <summary>Couche d'origine (libellé), à visée diagnostic — n'influence pas le code HTTP.</summary>
    Public ReadOnly Property Layer As String Implements IError.Layer
        Get
            Return _layer.GetLabel()
        End Get
    End Property

    Public ReadOnly Property Exception As Exception Implements IError.Exception
        Get
            Return _exception
        End Get
    End Property

    Public ReadOnly Property HasException As Boolean Implements IError.HasException
        Get
            Return _exception IsNot Nothing
        End Get
    End Property

    ''' <summary>Vrai si l'erreur représente une ressource introuvable (→ 404 côté HTTP).</summary>
    Public ReadOnly Property IsNotFound As Boolean
        Get
            Return _isNotFound
        End Get
    End Property

    ' ── Fabriques ────────────────────────────────────────────────────────────
    Public Shared Function Domain(message As String, Optional ex As Exception = Nothing) As ClError
        Return New ClError(message, ErrorLayer.Domain, ex)
    End Function

    Public Shared Function Application(message As String, Optional ex As Exception = Nothing) As ClError
        Return New ClError(message, ErrorLayer.Application, ex)
    End Function

    Public Shared Function Infrastructure(message As String, Optional ex As Exception = Nothing) As ClError
        Return New ClError(message, ErrorLayer.Infrastructure, ex)
    End Function

    ''' <summary>Erreur « introuvable » → mappée 404 (parité avec Data nul du presenter legacy).</summary>
    Public Shared Function NotFound(message As String) As ClError
        Return New ClError(message, ErrorLayer.Application, Nothing, isNotFound:=True)
    End Function

End Class
