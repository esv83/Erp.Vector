


Namespace Helper
    Public Interface IEntityMapper

        Function GetDto(entity As ClEntityBase) As Object
        Function GetEntity(Dto As Object) As ClEntityBase

    End Interface

End Namespace
