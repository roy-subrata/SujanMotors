namespace AutoPartShop.Application.Hr.Dtos
{
    public class ShiftResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int GraceMinutes { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class SaveShiftRequest
    {
        public string Name { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int GraceMinutes { get; set; } = 10;
        public string Notes { get; set; } = string.Empty;
    }
}
