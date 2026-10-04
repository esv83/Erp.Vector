Imports FluentValidation

''' <summary>
''' Saisie des quatre champs d'une carte mutuelle (04/10). Le <c>PATCH</c> les <b>remplace</b> tous —
''' c'est le contrat du front, inchangé — mais deux saisies ne doivent plus passer :
''' <list type="bullet">
'''   <item><b>un corps entièrement vide</b>, qui était pourtant marqué <c>validated</c> : il effaçait la
'''   carte en se donnant l'air de la confirmer ;</item>
'''   <item><b>un champ plus long que sa colonne</b>, qui faisait échouer l'écriture en base — un 500
'''   au lieu d'un refus lisible.</item>
''' </list>
''' Les longueurs sont celles de <c>MOB_MUTUELLE_CARD</c> (<c>MOB_003</c>).
''' </summary>
Public Class ClMutuelleFieldsDtoInValidator
    Inherits AbstractValidator(Of ClMutuelleFieldsDtoIn)

    Public Const MutuelleNameMax As Integer = 200
    Public Const AmcCodeMax As Integer = 50
    Public Const ConcentrateurMax As Integer = 100
    Public Const TeletransmissionMax As Integer = 50

    Public Sub New()
        RuleFor(Function(f) f).
            Must(Function(f) Renseigne(f.MutuelleName) OrElse Renseigne(f.AmcCode) OrElse
                             Renseigne(f.Concentrateur) OrElse Renseigne(f.Teletransmission)).
            WithMessage("Saisie vide : renseignez au moins un des quatre champs. Rien n'a été enregistré.")

        RuleFor(Function(f) f.MutuelleName).MaximumLength(MutuelleNameMax).
            WithMessage($"Nom de la mutuelle trop long ({MutuelleNameMax} caractères au plus).")
        RuleFor(Function(f) f.AmcCode).MaximumLength(AmcCodeMax).
            WithMessage($"Code AMC trop long ({AmcCodeMax} caractères au plus).")
        RuleFor(Function(f) f.Concentrateur).MaximumLength(ConcentrateurMax).
            WithMessage($"Concentrateur trop long ({ConcentrateurMax} caractères au plus).")
        RuleFor(Function(f) f.Teletransmission).MaximumLength(TeletransmissionMax).
            WithMessage($"Télétransmission trop longue ({TeletransmissionMax} caractères au plus).")
    End Sub

    Private Shared Function Renseigne(valeur As String) As Boolean
        Return Not String.IsNullOrWhiteSpace(valeur)
    End Function

End Class
