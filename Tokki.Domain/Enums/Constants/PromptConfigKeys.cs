namespace Tokki.Domain.Constants
{
    public static class PromptConfigKeys
    {
        ///Default: 4
        public const string MaxTasksPerDay = "AI_ROADMAP_MAX_TASKS_PER_DAY";

        ///Default: 6
        public const string StudyDaysPerWeek = "AI_ROADMAP_STUDY_DAYS_PER_WEEK";

        ///Default: 7
        public const string WeeklyExamDay = "AI_ROADMAP_WEEKLY_EXAM_DAY";

        ///Default: 50
        public const string ScoreThresholdLow = "AI_ROADMAP_SCORE_THRESHOLD_LOW";

        ///Default: 80
        public const string ScoreThresholdHigh = "AI_ROADMAP_SCORE_THRESHOLD_HIGH";

        ///Default: 10
        public const string QuestionsPerPartMcq = "EXAM_QUESTIONS_PER_PART_MCQ";

        ///Default: 2
        public const string QuestionsPerPartWriting = "EXAM_QUESTIONS_PER_PART_WRITING";

        ///Default: 2
        public const string MinutesPerMcqQuestion = "EXAM_MINUTES_PER_MCQ_QUESTION";

        ///Default: 20
        public const string MinutesPerWritingQuestion = "EXAM_MINUTES_PER_WRITING_QUESTION";
    }
}