using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EventEase.Validation
{
    /// <summary>
    /// Custom validation attributes for event-related data
    /// </summary>
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime date)
            {
                return date >= DateTime.Today;
            }
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"El campo {name} debe ser una fecha futura.";
        }
    }

    public class EventNameAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is string eventName)
            {
                // Check for prohibited words
                var prohibitedWords = new[] { "test", "prueba", "fake", "demo" };
                var lowerName = eventName.ToLower();
                
                if (prohibitedWords.Any(word => lowerName.Contains(word)))
                {
                    ErrorMessage = "El nombre del evento no puede contener palabras de prueba.";
                    return false;
                }

                // Check for minimum meaningful length
                if (eventName.Trim().Length < 3)
                {
                    ErrorMessage = "El nombre del evento debe tener al menos 3 caracteres.";
                    return false;
                }

                return true;
            }
            return false;
        }
    }

    public class LocationValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is string location)
            {
                // Basic location format validation
                var locationPattern = @"^[a-zA-ZÀ-ÿ\s\d,.-]+$";
                if (!Regex.IsMatch(location, locationPattern))
                {
                    ErrorMessage = "La ubicación contiene caracteres no válidos.";
                    return false;
                }

                // Check for minimum meaningful content
                if (location.Trim().Length < 5)
                {
                    ErrorMessage = "La ubicación debe ser más específica (mínimo 5 caracteres).";
                    return false;
                }

                return true;
            }
            return false;
        }
    }

    public class CapacityRangeAttribute : ValidationAttribute
    {
        private readonly int _minCapacity;
        private readonly int _maxCapacity;

        public CapacityRangeAttribute(int minCapacity = 1, int maxCapacity = 10000)
        {
            _minCapacity = minCapacity;
            _maxCapacity = maxCapacity;
        }

        public override bool IsValid(object? value)
        {
            if (value is int capacity)
            {
                return capacity >= _minCapacity && capacity <= _maxCapacity;
            }
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"La capacidad debe estar entre {_minCapacity} y {_maxCapacity} asistentes.";
        }
    }
}

namespace EventEase.Services
{
    /// <summary>
    /// Service for complex validation logic
    /// </summary>
    public interface IEventValidationService
    {
        Task<ValidationResult> ValidateEventAsync(object eventModel);
        bool IsLocationAvailable(string location, DateTime date);
        bool IsValidEventTime(DateTime date);
    }

    public class EventValidationService : IEventValidationService
    {
        public async Task<ValidationResult> ValidateEventAsync(object eventModel)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(eventModel);
            
            // Perform standard validation
            Validator.TryValidateObject(eventModel, validationContext, validationResults, true);
            
            // Add custom business logic validation
            if (eventModel is EventEase.Components.EventForm.EventModel model)
            {
                // Check if location is available on the selected date
                if (!IsLocationAvailable(model.EventLocation, model.EventDate))
                {
                    validationResults.Add(new ValidationResult(
                        "La ubicación no está disponible en la fecha seleccionada.",
                        new[] { nameof(model.EventLocation), nameof(model.EventDate) }
                    ));
                }

                // Validate event time (not too early or too late)
                if (!IsValidEventTime(model.EventDate))
                {
                    validationResults.Add(new ValidationResult(
                        "Los eventos deben programarse en horarios apropiados.",
                        new[] { nameof(model.EventDate) }
                    ));
                }
            }

            return validationResults.Count == 0 
                ? ValidationResult.Success!
                : new ValidationResult(string.Join("; ", validationResults.Select(r => r.ErrorMessage)));
        }

        public bool IsLocationAvailable(string location, DateTime date)
        {
            // Simulate checking location availability
            // In a real application, this would check against a database
            return !string.IsNullOrEmpty(location) && date > DateTime.Today;
        }

        public bool IsValidEventTime(DateTime date)
        {
            // Events should not be scheduled too far in advance or on weekends for certain types
            var dayOfWeek = date.DayOfWeek;
            return dayOfWeek != DayOfWeek.Sunday || date <= DateTime.Today.AddMonths(12);
        }
    }
}
