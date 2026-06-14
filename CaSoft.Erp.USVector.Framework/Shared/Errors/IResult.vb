Public Interface IResult
    ReadOnly Property IsSucces As Boolean
    ReadOnly Property IsFail As Boolean
    ReadOnly Property InnerError As IError

End Interface
