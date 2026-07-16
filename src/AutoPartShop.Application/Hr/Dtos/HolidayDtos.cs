namespace AutoPartShop.Application.Hr.Dtos
{
    public class HolidayResponse
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SaveHolidayRequest
    {
        public DateTime Date { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
