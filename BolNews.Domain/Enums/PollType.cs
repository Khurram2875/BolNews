namespace BolNews.Domain.Enums;

/// <summary>How a poll is presented. All types share the same options/votes model.</summary>
public enum PollType
{
    /// <summary>A question with 2-10 answers; the visitor picks one.</summary>
    Choice = 0,
    /// <summary>Exactly two contenders (e.g. two rap singers).</summary>
    Versus = 1,
    /// <summary>Exactly two answers, True / False (labels are editable, e.g. Yes / No).</summary>
    TrueFalse = 2
}
