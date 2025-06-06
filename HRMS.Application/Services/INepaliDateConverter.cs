namespace HRMS.Application.Services
{
    public interface INepaliDateConverter
    {
        (int Year, int Month, int Day) ConvertToBS(DateTime adDate);
        DateTime ConvertToAD(int bsYear, int bsMonth, int bsDay);
        bool IsValidBSDate(int year, int month, int day);
    }
}
