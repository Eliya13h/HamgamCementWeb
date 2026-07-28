using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models
{
    public class GeneralSettings 
    {
        public int Id { set; get; }
        public string PersianCompanyName { set; get; } = "فابریکه تولید و بسته بندی سمنت همگام نیمروز";
        public string EnglishCompanyName { set; get; } = "Hamgam Nimrooz Cement Manufacturing & Packing Co";
        // مسیر وب لوگوی ZM — فایل ثابت داخل public فرانت
        public string? ZmLogoPath { set; get; } = "/zm_logo.jpg";
        // مسیر وب لوگوی سازمان — فایل آپلودشده در wwwroot/uploads برای استفاده در گزارش‌ها
        public string? CompanyLogoPath { set; get; } = string.Empty;
        public string? CompanyAddress { set; get; } = string.Empty;
        public string? CompanyPhoneNumber1 { set; get; } = string.Empty;
        public string? CompanyPhoneNumber2 { set; get; } = string.Empty;
        public string? CompanyPhoneNumber3 { set; get; } = string.Empty;
        public string? CompanyEmail { set; get; } = string.Empty;
        public string? CompanySite { set; get; } = string.Empty;
        public string CalendarType { set; get; } = "Solar";

        // درصد مالیات پیش‌فرض فاکتور (پیش‌نویس)
        [Column(TypeName = "decimal(18,4)")]
        public decimal DefaultTaxPercent { set; get; }
    }
}
