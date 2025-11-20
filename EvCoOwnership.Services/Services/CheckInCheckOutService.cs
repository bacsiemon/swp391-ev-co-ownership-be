using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.CheckInCheckOutDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EvCoOwnership.Services.Services
{
    public class CheckInCheckOutService : ICheckInCheckOutService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CheckInCheckOutService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region QR Code Check-In/Out (CoOwner Self-Service)

        public async Task<BaseResponse<CheckInResponse>> QRScanCheckInAsync(
            int userId,
            QRScanCheckInRequest request)
        {
            try
            {
                // Parse and validate QR code
                var qrData = ParseQRCodeData(request.QRCodeData);
                if (qrData == null)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 400,
                        Message = "INVALID_QR_CODE"
                    };
                }

                // Get booking with full details
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .Include(b => b.CheckIns)
                    .FirstOrDefaultAsync(b => b.Id == qrData.BookingId);

                if (booking == null)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Verify ownership
                if (booking.CoOwner?.UserId != userId)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_BOOKING_OWNER"
                    };
                }

                // Check booking status
                if (booking.StatusEnum != EBookingStatus.Confirmed)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 400,
                        Message = "BOOKING_NOT_CONFIRMED"
                    };
                }

                // Check if already checked in
                if (booking.CheckIns.Any())
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 400,
                        Message = "ALREADY_CHECKED_IN",
                        Data = new CheckInResponse
                        {
                            Status = CheckInStatus.AlreadyCheckedIn,
                            StatusMessage = "This booking has already been checked in"
                        }
                    };
                }

                // Validate timing (can check-in up to 30 minutes before booking start)
                var now = DateTime.UtcNow;
                var timeDiff = (booking.StartTime - now).TotalMinutes;
                if (timeDiff > 30)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 400,
                        Message = "TOO_EARLY_FOR_CHECKIN",
                        Errors = $"Check-in available from {booking.StartTime.AddMinutes(-30):g}"
                    };
                }

                // Validate QR code expiry (24 hours from generation)
                if ((now - qrData.GeneratedAt).TotalHours > 24)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 400,
                        Message = "QR_CODE_EXPIRED"
                    };
                }

                // Get vehicle station
                var station = await _unitOfWork.VehicleStationRepository.GetByIdAsync(qrData.VehicleStationId);
                if (station == null)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_STATION_NOT_FOUND"
                    };
                }

                // Create vehicle condition record if not exists
                VehicleCondition? condition = null;
                if (request.ConditionReport != null)
                {
                    condition = new VehicleCondition
                    {
                        VehicleId = booking.VehicleId,
                        ConditionTypeEnum = request.ConditionReport.ConditionType,
                        Description = request.ConditionReport.Notes,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.VehicleConditionRepository.AddAsync(condition);
                    await _unitOfWork.SaveChangesAsync();
                }                // Create check-in record
                var checkIn = new CheckIn
                {
                    BookingId = booking.Id,
                    StaffId = null, // Self-service, no staff
                    VehicleStationId = station.Id,
                    VehicleConditionId = condition?.Id,
                    CheckTime = now,
                    CreatedAt = now
                };

                await _unitOfWork.CheckInRepository.AddAsync(checkIn);

                // Update booking status to Active
                booking.StatusEnum = EBookingStatus.Active;
                booking.UpdatedAt = now;
                await _unitOfWork.BookingRepository.UpdateAsync(booking);

                await _unitOfWork.SaveChangesAsync();

                var response = new CheckInResponse
                {
                    CheckInId = checkIn.Id,
                    BookingId = booking.Id,
                    BookingPurpose = booking.Purpose ?? "",
                    VehicleId = booking.Vehicle?.Id ?? 0,
                    VehicleName = booking.Vehicle?.Name ?? "",
                    LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    CoOwnerId = booking.CoOwner?.UserId ?? 0,
                    CoOwnerName = $"{booking.CoOwner?.User?.FirstName} {booking.CoOwner?.User?.LastName}".Trim(),
                    VehicleStationId = station.Id,
                    VehicleStationName = station.Name ?? "",
                    VehicleStationAddress = station.Address ?? "",
                    CheckInTime = checkIn.CheckTime,
                    VehicleCondition = request.ConditionReport?.ConditionType ?? EVehicleConditionType.Good,
                    ConditionReport = request.ConditionReport,
                    WasQRScanned = true,
                    Notes = request.Notes,
                    Status = CheckInStatus.Success,
                    StatusMessage = "Vehicle picked up successfully via QR scan"
                };

                return new BaseResponse<CheckInResponse>
                {
                    StatusCode = 200,
                    Message = "CHECK_IN_SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<CheckInResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<CheckOutResponse>> QRScanCheckOutAsync(
            int userId,
            QRScanCheckOutRequest request)
        {
            try
            {
                // Parse and validate QR code
                var qrData = ParseQRCodeData(request.QRCodeData);
                if (qrData == null)
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 400,
                        Message = "INVALID_QR_CODE"
                    };
                }

                // Get booking with full details
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .Include(b => b.CheckIns)
                    .Include(b => b.CheckOuts)
                    .FirstOrDefaultAsync(b => b.Id == qrData.BookingId);

                if (booking == null)
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Verify ownership
                if (booking.CoOwner?.UserId != userId)
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_BOOKING_OWNER"
                    };
                }

                // Must be checked in first
                if (!booking.CheckIns.Any())
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 400,
                        Message = "NOT_CHECKED_IN"
                    };
                }

                // Check if already checked out
                if (booking.CheckOuts.Any())
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 400,
                        Message = "ALREADY_CHECKED_OUT",
                        Data = new CheckOutResponse
                        {
                            Status = CheckOutStatus.AlreadyCheckedOut,
                            StatusMessage = "This booking has already been checked out"
                        }
                    };
                }

                var now = DateTime.UtcNow;
                var checkIn = booking.CheckIns.First();

                // Get vehicle station
                var station = await _unitOfWork.VehicleStationRepository.GetByIdAsync(qrData.VehicleStationId);
                if (station == null)
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_STATION_NOT_FOUND"
                    };
                }

                // Create vehicle condition record
                var condition = new VehicleCondition
                {
                    VehicleId = booking.VehicleId,
                    ConditionTypeEnum = request.ConditionReport.ConditionType,
                    Description = request.ConditionReport.Notes,
                    CreatedAt = now
                };
                await _unitOfWork.VehicleConditionRepository.AddAsync(condition);
                await _unitOfWork.SaveChangesAsync();

                // Create check-out record
                var checkOut = new CheckOut
                {
                    BookingId = booking.Id,
                    StaffId = null, // Self-service
                    VehicleStationId = station.Id,
                    VehicleConditionId = condition.Id,
                    CheckTime = now,
                    CreatedAt = now
                };

                await _unitOfWork.CheckOutRepository.AddAsync(checkOut);

                // Calculate usage and fees
                var usage = CalculateUsage(checkIn, checkOut, booking, request);
                var hasNewDamages = request.ConditionReport.HasDamages;
                var damageCharges = hasNewDamages ? CalculateDamageCharges(request.ConditionReport.Damages) : 0m;

                // Update booking
                booking.StatusEnum = EBookingStatus.Completed;
                booking.TotalCost = usage.TotalCost + damageCharges;
                booking.UpdatedAt = now;
                await _unitOfWork.BookingRepository.UpdateAsync(booking);

                // Create usage record
                await CreateUsageRecordAsync(
                    booking,
                    checkIn,
                    checkOut,
                    usage,
                    damageCharges,
                    request.OdometerReading,
                    request.BatteryLevel,
                    null, // No staff for QR scan
                    true); // Was QR scanned

                await _unitOfWork.SaveChangesAsync();

                var status = hasNewDamages
                    ? CheckOutStatus.SuccessWithDamages
                    : usage.LateFee > 0
                        ? CheckOutStatus.SuccessWithLateFee
                        : CheckOutStatus.Success;

                var response = new CheckOutResponse
                {
                    CheckOutId = checkOut.Id,
                    BookingId = booking.Id,
                    BookingPurpose = booking.Purpose ?? "",
                    VehicleId = booking.Vehicle?.Id ?? 0,
                    VehicleName = booking.Vehicle?.Name ?? "",
                    LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    CoOwnerId = booking.CoOwner?.UserId ?? 0,
                    CoOwnerName = $"{booking.CoOwner?.User?.FirstName} {booking.CoOwner?.User?.LastName}".Trim(),
                    VehicleStationId = station.Id,
                    VehicleStationName = station.Name ?? "",
                    VehicleStationAddress = station.Address ?? "",
                    CheckOutTime = checkOut.CheckTime,
                    VehicleCondition = request.ConditionReport.ConditionType,
                    ConditionReport = request.ConditionReport,
                    WasQRScanned = true,
                    OdometerReading = request.OdometerReading,
                    BatteryLevel = request.BatteryLevel,
                    DamagesFound = request.ConditionReport.Damages,
                    HasNewDamages = hasNewDamages,
                    DamageCharges = damageCharges,
                    Notes = request.Notes,
                    Status = status,
                    StatusMessage = GenerateCheckOutMessage(status, usage, hasNewDamages),
                    UsageSummary = usage
                };

                return new BaseResponse<CheckOutResponse>
                {
                    StatusCode = 200,
                    Message = "CHECK_OUT_SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<CheckOutResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<VehicleQRCodeData>> GenerateBookingQRCodeAsync(
            int bookingId,
            int userId)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return new BaseResponse<VehicleQRCodeData>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Can be co-owner or staff
                var isCoOwner = booking.CoOwner?.UserId == userId;
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                var isStaff = user?.RoleEnum == EUserRole.Staff || user?.RoleEnum == EUserRole.Admin;

                if (!isCoOwner && !isStaff)
                {
                    return new BaseResponse<VehicleQRCodeData>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                // Only generate QR for confirmed bookings
                if (booking.StatusEnum != EBookingStatus.Confirmed)
                {
                    return new BaseResponse<VehicleQRCodeData>
                    {
                        StatusCode = 400,
                        Message = "BOOKING_NOT_CONFIRMED"
                    };
                }

                var qrData = new VehicleQRCodeData
                {
                    BookingId = booking.Id,
                    VehicleId = booking.Vehicle?.Id ?? 0,
                    VehicleLicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    CoOwnerId = booking.CoOwner?.UserId ?? 0,
                    CoOwnerName = $"{booking.CoOwner?.User?.FirstName} {booking.CoOwner?.User?.LastName}".Trim(),
                    BookingStartTime = booking.StartTime,
                    BookingEndTime = booking.EndTime,
                    VehicleStationId = 0, // To be provided at check-in
                    VehicleStationName = "",
                    GeneratedAt = DateTime.UtcNow,
                    QRCodeHash = GenerateQRCodeHash(booking.Id, booking.StartTime)
                };

                return new BaseResponse<VehicleQRCodeData>
                {
                    StatusCode = 200,
                    Message = "QR_CODE_GENERATED",
                    Data = qrData
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<VehicleQRCodeData>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                };
            }
        }

        #endregion

        #region Manual Check-In/Out (Staff Verification)

        public async Task<BaseResponse<CheckInResponse>> ManualCheckInAsync(
            int staffId,
            ManualCheckInRequest request)
        {
            try
            {
                // Verify staff role
                var staff = await _unitOfWork.UserRepository.GetByIdAsync(staffId);
                if (staff?.RoleEnum != EUserRole.Staff && staff?.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_STAFF_ONLY"
                    };
                }

                // Get booking
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .Include(b => b.CheckIns)
                    .FirstOrDefaultAsync(b => b.Id == request.BookingId);

                if (booking == null)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Check booking status
                if (booking.StatusEnum != EBookingStatus.Confirmed)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 400,
                        Message = "BOOKING_NOT_CONFIRMED"
                    };
                }

                // Check if already checked in
                if (booking.CheckIns.Any())
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 400,
                        Message = "ALREADY_CHECKED_IN"
                    };
                }

                // Get vehicle station
                var station = await _unitOfWork.VehicleStationRepository.GetByIdAsync(request.VehicleStationId);
                if (station == null)
                {
                    return new BaseResponse<CheckInResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_STATION_NOT_FOUND"
                    };
                }

                // Create vehicle condition record
                var condition = new VehicleCondition
                {
                    VehicleId = booking.VehicleId,
                    ConditionTypeEnum = request.ConditionReport.ConditionType,
                    Description = request.ConditionReport.Notes,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.VehicleConditionRepository.AddAsync(condition);
                await _unitOfWork.SaveChangesAsync();

                var checkInTime = request.OverrideCheckInTime ?? DateTime.UtcNow;

                // Create check-in record
                var checkIn = new CheckIn
                {
                    BookingId = booking.Id,
                    StaffId = staffId,
                    VehicleStationId = station.Id,
                    VehicleConditionId = condition.Id,
                    CheckTime = checkInTime,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.CheckInRepository.AddAsync(checkIn);

                // Update booking status
                booking.StatusEnum = EBookingStatus.Active;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.BookingRepository.UpdateAsync(booking);

                await _unitOfWork.SaveChangesAsync();

                var response = new CheckInResponse
                {
                    CheckInId = checkIn.Id,
                    BookingId = booking.Id,
                    BookingPurpose = booking.Purpose ?? "",
                    VehicleId = booking.Vehicle?.Id ?? 0,
                    VehicleName = booking.Vehicle?.Name ?? "",
                    LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    CoOwnerId = booking.CoOwner?.UserId ?? 0,
                    CoOwnerName = $"{booking.CoOwner?.User?.FirstName} {booking.CoOwner?.User?.LastName}".Trim(),
                    VehicleStationId = station.Id,
                    VehicleStationName = station.Name ?? "",
                    VehicleStationAddress = station.Address ?? "",
                    CheckInTime = checkIn.CheckTime,
                    StaffId = staffId,
                    StaffName = $"{staff.FirstName} {staff.LastName}".Trim(),
                    VehicleCondition = request.ConditionReport.ConditionType,
                    ConditionReport = request.ConditionReport,
                    WasQRScanned = false,
                    Notes = request.StaffNotes,
                    Status = CheckInStatus.Success,
                    StatusMessage = "Vehicle picked up - manually verified by staff"
                };

                return new BaseResponse<CheckInResponse>
                {
                    StatusCode = 200,
                    Message = "MANUAL_CHECK_IN_SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<CheckInResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<CheckOutResponse>> ManualCheckOutAsync(
            int staffId,
            ManualCheckOutRequest request)
        {
            try
            {
                // Verify staff role
                var staff = await _unitOfWork.UserRepository.GetByIdAsync(staffId);
                if (staff?.RoleEnum != EUserRole.Staff && staff?.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_STAFF_ONLY"
                    };
                }

                // Get booking
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .Include(b => b.CheckIns)
                    .Include(b => b.CheckOuts)
                    .FirstOrDefaultAsync(b => b.Id == request.BookingId);

                if (booking == null)
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Must be checked in first
                if (!booking.CheckIns.Any())
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 400,
                        Message = "NOT_CHECKED_IN"
                    };
                }

                // Check if already checked out
                if (booking.CheckOuts.Any())
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 400,
                        Message = "ALREADY_CHECKED_OUT"
                    };
                }

                var checkIn = booking.CheckIns.First();

                // Get vehicle station
                var station = await _unitOfWork.VehicleStationRepository.GetByIdAsync(request.VehicleStationId);
                if (station == null)
                {
                    return new BaseResponse<CheckOutResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_STATION_NOT_FOUND"
                    };
                }

                // Create vehicle condition record
                var condition = new VehicleCondition
                {
                    VehicleId = booking.VehicleId,
                    ConditionTypeEnum = request.ConditionReport.ConditionType,
                    Description = request.ConditionReport.Notes,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.VehicleConditionRepository.AddAsync(condition);
                await _unitOfWork.SaveChangesAsync();

                var checkOutTime = request.OverrideCheckOutTime ?? DateTime.UtcNow;

                // Create check-out record
                var checkOut = new CheckOut
                {
                    BookingId = booking.Id,
                    StaffId = staffId,
                    VehicleStationId = station.Id,
                    VehicleConditionId = condition.Id,
                    CheckTime = checkOutTime,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.CheckOutRepository.AddAsync(checkOut);

                // Calculate usage and fees
                var usage = CalculateUsage(checkIn, checkOut, booking, null, request.OdometerReading, request.BatteryLevel);
                var hasNewDamages = request.DamagesFound?.Any() ?? false;
                var damageCharges = hasNewDamages ? CalculateDamageCharges(request.DamagesFound) : 0m;

                // Update booking
                booking.StatusEnum = EBookingStatus.Completed;
                booking.TotalCost = usage.TotalCost + damageCharges;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.BookingRepository.UpdateAsync(booking);

                // Create usage record
                await CreateUsageRecordAsync(
                    booking,
                    checkIn,
                    checkOut,
                    usage,
                    damageCharges,
                    request.OdometerReading,
                    request.BatteryLevel,
                    staffId,
                    false); // Manual check-out

                await _unitOfWork.SaveChangesAsync();

                var status = hasNewDamages
                    ? CheckOutStatus.SuccessWithDamages
                    : usage.LateFee > 0
                        ? CheckOutStatus.SuccessWithLateFee
                        : CheckOutStatus.Success;

                var response = new CheckOutResponse
                {
                    CheckOutId = checkOut.Id,
                    BookingId = booking.Id,
                    BookingPurpose = booking.Purpose ?? "",
                    VehicleId = booking.Vehicle?.Id ?? 0,
                    VehicleName = booking.Vehicle?.Name ?? "",
                    LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    CoOwnerId = booking.CoOwner?.UserId ?? 0,
                    CoOwnerName = $"{booking.CoOwner?.User?.FirstName} {booking.CoOwner?.User?.LastName}".Trim(),
                    VehicleStationId = station.Id,
                    VehicleStationName = station.Name ?? "",
                    VehicleStationAddress = station.Address ?? "",
                    CheckOutTime = checkOut.CheckTime,
                    StaffId = staffId,
                    StaffName = $"{staff.FirstName} {staff.LastName}".Trim(),
                    VehicleCondition = request.ConditionReport.ConditionType,
                    ConditionReport = request.ConditionReport,
                    WasQRScanned = false,
                    OdometerReading = request.OdometerReading,
                    BatteryLevel = request.BatteryLevel,
                    DamagesFound = request.DamagesFound,
                    HasNewDamages = hasNewDamages,
                    DamageCharges = damageCharges,
                    Notes = request.StaffNotes,
                    Status = status,
                    StatusMessage = GenerateCheckOutMessage(status, usage, hasNewDamages),
                    UsageSummary = usage
                };

                return new BaseResponse<CheckOutResponse>
                {
                    StatusCode = 200,
                    Message = "MANUAL_CHECK_OUT_SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<CheckOutResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                };
            }
        }

        #endregion

        #region Validation & Utilities

        public async Task<BaseResponse<object>> ValidateCheckInEligibilityAsync(int bookingId, int userId)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                    .Include(b => b.CheckIns)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                if (booking.CoOwner?.UserId != userId)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                var errors = new List<string>();
                var now = DateTime.UtcNow;

                if (booking.StatusEnum != EBookingStatus.Confirmed)
                {
                    errors.Add("Booking is not confirmed");
                }

                if (booking.CheckIns.Any())
                {
                    errors.Add("Already checked in");
                }

                var timeDiff = (booking.StartTime - now).TotalMinutes;
                if (timeDiff > 30)
                {
                    errors.Add($"Too early. Check-in available from {booking.StartTime.AddMinutes(-30):g}");
                }

                return new BaseResponse<object>
                {
                    StatusCode = errors.Any() ? 400 : 200,
                    Message = errors.Any() ? "CHECK_IN_NOT_ELIGIBLE" : "CHECK_IN_ELIGIBLE",
                    Data = new
                    {
                        isEligible = !errors.Any(),
                        errors,
                        bookingStartTime = booking.StartTime,
                        earliestCheckInTime = booking.StartTime.AddMinutes(-30)
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<object>> ValidateCheckOutEligibilityAsync(int bookingId, int userId)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                    .Include(b => b.CheckIns)
                    .Include(b => b.CheckOuts)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                if (booking.CoOwner?.UserId != userId)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                var errors = new List<string>();

                if (!booking.CheckIns.Any())
                {
                    errors.Add("Not checked in yet");
                }

                if (booking.CheckOuts.Any())
                {
                    errors.Add("Already checked out");
                }

                return new BaseResponse<object>
                {
                    StatusCode = errors.Any() ? 400 : 200,
                    Message = errors.Any() ? "CHECK_OUT_NOT_ELIGIBLE" : "CHECK_OUT_ELIGIBLE",
                    Data = new
                    {
                        isEligible = !errors.Any(),
                        errors,
                        bookingEndTime = booking.EndTime,
                        checkInTime = booking.CheckIns.FirstOrDefault()?.CheckTime
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<object>> GetBookingCheckInOutHistoryAsync(int bookingId, int userId)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                    .Include(b => b.CheckIns)
                        .ThenInclude(ci => ci.Staff)
                    .Include(b => b.CheckIns)
                        .ThenInclude(ci => ci.VehicleStation)
                    .Include(b => b.CheckOuts)
                        .ThenInclude(co => co.Staff)
                    .Include(b => b.CheckOuts)
                        .ThenInclude(co => co.VehicleStation)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                if (booking.CoOwner?.UserId != userId)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                var history = new
                {
                    bookingId = booking.Id,
                    checkIns = booking.CheckIns.Select(ci => new
                    {
                        checkInId = ci.Id,
                        checkTime = ci.CheckTime,
                        stationName = ci.VehicleStation?.Name,
                        stationAddress = ci.VehicleStation?.Address,
                        staffName = ci.Staff != null ? $"{ci.Staff.FirstName} {ci.Staff.LastName}".Trim() : "Self-service",
                        wasManual = ci.StaffId.HasValue
                    }).ToList(),
                    checkOuts = booking.CheckOuts.Select(co => new
                    {
                        checkOutId = co.Id,
                        checkTime = co.CheckTime,
                        stationName = co.VehicleStation?.Name,
                        stationAddress = co.VehicleStation?.Address,
                        staffName = co.Staff != null ? $"{co.Staff.FirstName} {co.Staff.LastName}".Trim() : "Self-service",
                        wasManual = co.StaffId.HasValue
                    }).ToList()
                };

                return new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "HISTORY_RETRIEVED",
                    Data = history
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                };
            }
        }

        #endregion

        #region Helper Methods

        private VehicleQRCodeData? ParseQRCodeData(string qrCodeData)
        {
            try
            {
                return JsonSerializer.Deserialize<VehicleQRCodeData>(qrCodeData);
            }
            catch
            {
                return null;
            }
        }

        private string GenerateQRCodeHash(int bookingId, DateTime bookingTime)
        {
            var data = $"{bookingId}:{bookingTime:yyyyMMddHHmmss}:EVCO";
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        private UsageSummary CalculateUsage(
            CheckIn checkIn,
            CheckOut checkOut,
            Booking booking,
            QRScanCheckOutRequest? qrRequest = null,
            int? manualOdometer = null,
            int? manualBattery = null)
        {
            var duration = checkOut.CheckTime - checkIn.CheckTime;
            var totalHours = (int)Math.Ceiling(duration.TotalHours);

            // Base cost from booking
            var baseCost = booking.TotalCost ?? 0;

            // Late fee calculation (15 minutes grace period)
            decimal lateFee = 0;
            var timePastEnd = (checkOut.CheckTime - booking.EndTime).TotalMinutes;
            if (timePastEnd > 15)
            {
                var lateHours = Math.Ceiling((timePastEnd - 15) / 60);
                lateFee = (decimal)lateHours * 50000; // 50,000 VND per late hour
            }

            var totalCost = baseCost + lateFee;

            return new UsageSummary
            {
                CheckInTime = checkIn.CheckTime,
                CheckOutTime = checkOut.CheckTime,
                TotalDuration = duration,
                TotalHours = totalHours,
                DistanceTraveled = manualOdometer ?? qrRequest?.OdometerReading,
                BatteryUsed = manualBattery ?? qrRequest?.BatteryLevel,
                BookingCost = baseCost,
                LateFee = lateFee > 0 ? lateFee : null,
                TotalCost = totalCost
            };
        }

        private decimal CalculateDamageCharges(List<DamageReport>? damages)
        {
            if (damages == null || !damages.Any())
                return 0;

            return damages.Sum(d => d.EstimatedCost ?? 0);
        }

        private string GenerateCheckOutMessage(CheckOutStatus status, UsageSummary usage, bool hasNewDamages)
        {
            return status switch
            {
                CheckOutStatus.Success => "Vehicle returned successfully",
                CheckOutStatus.SuccessWithDamages =>
                    $"Vehicle returned with {(hasNewDamages ? "new damages" : "issues")} - charges may apply",
                CheckOutStatus.SuccessWithLateFee =>
                    $"Vehicle returned late - late fee: {usage.LateFee:N0} VND",
                _ => "Check-out processed"
            };
        }

        /// <summary>
        /// Create a vehicle usage record after successful check-out
        /// </summary>
        private async Task CreateUsageRecordAsync(
            Booking booking,
            CheckIn checkIn,
            CheckOut checkOut,
            UsageSummary usage,
            decimal damageCharges,
            int? odometerEnd,
            int? batteryEnd,
            int? staffId,
            bool wasQRScanned)
        {
            // Get odometer and battery start values from check-in condition
            var checkInCondition = checkIn.VehicleConditionId.HasValue
                ? await _unitOfWork.VehicleConditionRepository.GetByIdAsync(checkIn.VehicleConditionId.Value)
                : null;

            var odometerStart = checkInCondition?.OdometerReading;
            var batteryStart = checkInCondition?.FuelLevel.HasValue == true
                ? (int?)Math.Round(checkInCondition.FuelLevel.Value)
                : null;

            var usageRecord = new VehicleUsageRecord
            {
                BookingId = booking.Id,
                VehicleId = booking.VehicleId,
                CoOwnerId = booking.CoOwnerId,
                CheckInId = checkIn.Id,
                CheckOutId = checkOut.Id,
                StartTime = checkIn.CheckTime,
                EndTime = checkOut.CheckTime,
                DurationHours = (decimal)usage.TotalDuration.TotalHours,
                DistanceKm = usage.DistanceTraveled,
                BatteryUsedPercent = usage.BatteryUsed,
                OdometerStart = odometerStart,
                OdometerEnd = odometerEnd,
                BatteryLevelStart = batteryStart,
                BatteryLevelEnd = batteryEnd,
                BookingCost = usage.BookingCost,
                LateFee = usage.LateFee,
                DamageFee = damageCharges > 0 ? damageCharges : null,
                TotalCost = usage.TotalCost + damageCharges,
                Purpose = booking.Purpose,
                Notes = null,
                WasQrScanned = wasQRScanned,
                AssistingStaffId = staffId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.VehicleUsageRecordRepository.AddAsync(usageRecord);
        }

        #endregion
    }
}

