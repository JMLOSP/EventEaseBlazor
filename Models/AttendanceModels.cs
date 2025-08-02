using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class AttendanceRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Registered;
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string? Notes { get; set; }
        public bool IsVip { get; set; }
        public string? SeatNumber { get; set; }
        public Dictionary<string, object> CustomFields { get; set; } = new();

        // Calculated properties
        public TimeSpan? AttendanceDuration => CheckOutTime.HasValue && CheckInTime.HasValue 
            ? CheckOutTime.Value - CheckInTime.Value 
            : null;

        public bool IsPresent => Status == AttendanceStatus.Present || Status == AttendanceStatus.CheckedOut;
    }

    public enum AttendanceStatus
    {
        Registered,     // Usuario registrado pero no ha llegado
        Present,        // Usuario presente en el evento
        CheckedOut,     // Usuario se retiró del evento
        NoShow,         // Usuario no asistió
        Cancelled       // Registro cancelado
    }

    public class EventAttendanceInfo
    {
        public string EventId { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventLocation { get; set; } = string.Empty;
        public int MaxCapacity { get; set; }
        public int TotalRegistered { get; set; }
        public int TotalPresent { get; set; }
        public int TotalNoShow { get; set; }
        public int TotalCheckedOut { get; set; }
        public double AttendanceRate => TotalRegistered > 0 ? (double)TotalPresent / TotalRegistered * 100 : 0;
        public double AttendancePercentage => AttendanceRate; // Alias for consistency
        public double NoShowRate => TotalRegistered > 0 ? (double)TotalNoShow / TotalRegistered * 100 : 0;
        public int AvailableSpots => MaxCapacity - TotalRegistered;
        public bool IsFull => TotalRegistered >= MaxCapacity;
    }

    public class AttendanceCheckInModel
    {
        [Required(ErrorMessage = "El ID del evento es requerido")]
        public string EventId { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email del usuario es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string UserEmail { get; set; } = string.Empty;

        public string? Notes { get; set; }
        public bool IsVip { get; set; }
        public string? SeatNumber { get; set; }
    }

    public class AttendanceSearchFilter
    {
        public string? EventId { get; set; }
        public string? UserEmail { get; set; }
        public AttendanceStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsVip { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class AttendanceStatistics
    {
        public int TotalEvents { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalAttendees { get; set; }
        public double OverallAttendanceRate { get; set; }
        public Dictionary<string, int> AttendanceByStatus { get; set; } = new();
        public Dictionary<string, double> AttendanceByEvent { get; set; } = new();
        public Dictionary<DateTime, int> AttendanceByDate { get; set; } = new();
        public List<TopAttendee> TopAttendees { get; set; } = new();
    }

    public class TopAttendee
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int EventsAttended { get; set; }
        public double AttendanceRate { get; set; }
    }
}
