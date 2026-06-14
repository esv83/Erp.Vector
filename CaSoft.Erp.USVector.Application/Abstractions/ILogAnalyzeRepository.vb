Public Interface ILogAnalyzeRepository


    Function GetAnalyze(intLogId As Integer) As ClLogAnalyze
    Sub SaveAnalyze(analyze As ClLogAnalyze)


End Interface
