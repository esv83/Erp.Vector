Public Interface IComplexMapping(Of C, D)
    Function GetComplexObject(Dto As D) As C
    Sub LoadDto(ComplexObject As C, Dto As D)

End Interface

