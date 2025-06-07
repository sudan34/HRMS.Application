using AutoMapper;
using HRMS.Application.DTOs;
using HRMS.Application.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HRMS.Application.Profiles
{
    public class AttendanceProfile : Profile
    {
        public AttendanceProfile()
        {
            CreateMap<AttendanceReportDto, AttendanceReportViewModel>()
                .ForMember(dest => dest.AttendancePercentage,
                    opt => opt.MapFrom(src => CalculateAttendancePercentage(src)));
        }
        private static decimal CalculateAttendancePercentage(AttendanceReportDto dto)
        {
            if (dto.TotalPresent > 0)
            {
                return (dto.TotalPresent * 100m) / (dto.TotalPresent + dto.TotalLate + dto.TotalAbsent);
            }
            return 0;
        }
    }
}
