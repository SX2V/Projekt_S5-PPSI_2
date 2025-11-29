namespace SportConnect.API.Dtos
{
    public class TrainingRequestStatsDto
    {
        public int DailyRequests { get; set; }
        public int WeeklyRequests { get; set; }
        public int MonthlyRequests { get; set; }
        public int DailyResponses { get; set; }
        public int WeeklyResponses { get; set; }
        public int MonthlyResponses { get; set; }
    }
}
