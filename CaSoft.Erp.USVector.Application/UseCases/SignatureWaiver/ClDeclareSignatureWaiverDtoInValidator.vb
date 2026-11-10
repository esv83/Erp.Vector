Imports FluentValidation

''' <summary>
''' Forme de la déclaration d'un motif. Ce qui dépend de la liste en base — le motif existe-t-il, est-il
''' actif, exige-t-il un commentaire — se juge dans le cas d'usage. Longueurs : celles de MOB_012.
''' </summary>
Public Class ClDeclareSignatureWaiverDtoInValidator
    Inherits AbstractValidator(Of ClDeclareSignatureWaiverDtoIn)

    Public Const ReasonCodeMax As Integer = 30
    Public Const CommentMax As Integer = 500

    Public Sub New()
        RuleFor(Function(d) d.ReasonCode).NotEmpty().
            WithMessage("Choisissez un motif. Rien n'a été enregistré.")
        RuleFor(Function(d) d.ReasonCode).MaximumLength(ReasonCodeMax).
            WithMessage($"Code de motif trop long ({ReasonCodeMax} caractères au plus).")
        RuleFor(Function(d) d.Comment).MaximumLength(CommentMax).
            WithMessage($"Commentaire trop long ({CommentMax} caractères au plus).")
    End Sub

End Class
