using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccessLayer.Migrations
{
    public partial class init1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Auctions",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    CarId = table.Column<string>(nullable: true),
                    DealerId = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ApprovedDate = table.Column<DateTime>(nullable: false),
                    ApprovedBy = table.Column<string>(nullable: true),
                    AuctionDays = table.Column<int>(nullable: false),
                    Reserve = table.Column<decimal>(nullable: false),
                    Increment = table.Column<decimal>(nullable: false),
                    StartAmount = table.Column<decimal>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auctions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bids",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    CarId = table.Column<string>(nullable: true),
                    BiddingAmount = table.Column<decimal>(nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    UpdatedOn = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(nullable: true),
                    AuctionDays = table.Column<int>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bids", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarReviews",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    Rating = table.Column<int>(nullable: false),
                    CarId = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    Version = table.Column<string>(nullable: true),
                    Owner = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    ThumbnailUrl = table.Column<string>(nullable: true),
                    SmallUrl = table.Column<string>(nullable: true),
                    Listing = table.Column<string>(nullable: true),
                    ListingGrid = table.Column<string>(nullable: true),
                    MediumUrl = table.Column<string>(nullable: true),
                    Deleted = table.Column<bool>(nullable: false),
                    IsProcessed = table.Column<bool>(nullable: false),
                    Order = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: false),
                    LastName = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: false),
                    Phone = table.Column<string>(nullable: false),
                    OfferAmount = table.Column<decimal>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    CarId = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    Version = table.Column<string>(nullable: true),
                    UserType = table.Column<string>(nullable: false),
                    DealerId = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: false),
                    Password = table.Column<string>(nullable: false),
                    FirstName = table.Column<string>(nullable: false),
                    LastName = table.Column<string>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    Idp = table.Column<string>(nullable: true),
                    CompanyName = table.Column<string>(nullable: true),
                    Phone = table.Column<string>(nullable: true),
                    HasMoreCars = table.Column<bool>(nullable: false),
                    ProfileUrl = table.Column<string>(nullable: true),
                    HasMorePurchases = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Country = table.Column<string>(nullable: true),
                    Street = table.Column<string>(nullable: true),
                    City = table.Column<string>(nullable: true),
                    State = table.Column<string>(nullable: true),
                    Zip = table.Column<string>(nullable: true),
                    Lat = table.Column<string>(nullable: true),
                    Lon = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Address_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    Version = table.Column<string>(nullable: true),
                    Owner = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    Mileage = table.Column<int>(nullable: false),
                    SalePrice = table.Column<double>(nullable: false),
                    HasImages = table.Column<bool>(nullable: false),
                    Status = table.Column<string>(nullable: true),
                    Make = table.Column<string>(nullable: true),
                    Model = table.Column<string>(nullable: true),
                    Year = table.Column<int>(nullable: false),
                    Color = table.Column<string>(nullable: true),
                    Thumbnail = table.Column<string>(nullable: true),
                    PagePartId = table.Column<string>(nullable: true),
                    Deleted = table.Column<bool>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Properties_NCSAMake = table.Column<string>(nullable: true),
                    Properties_NCSAMapExcApprovedBy = table.Column<string>(nullable: true),
                    Properties_NCSAMapExcApprovedOn = table.Column<string>(nullable: true),
                    Properties_NCSAMappingException = table.Column<string>(nullable: true),
                    Properties_NCSAModel = table.Column<string>(nullable: true),
                    Properties_NCSANote = table.Column<string>(nullable: true),
                    Properties_NCSABodyType = table.Column<string>(nullable: true),
                    Properties_Note = table.Column<string>(nullable: true),
                    Properties_OtherEngineInfo = table.Column<string>(nullable: true),
                    Properties_OtherMotorcycleInfo = table.Column<string>(nullable: true),
                    Properties_OtherRestraintSystemInfo = table.Column<string>(nullable: true),
                    Properties_OtherTrailerInfo = table.Column<string>(nullable: true),
                    Properties_ParkAssist = table.Column<string>(nullable: true),
                    Properties_PedestrianAutomaticEmergencyBraking = table.Column<string>(nullable: true),
                    Properties_OtherBusInfo = table.Column<string>(nullable: true),
                    Properties_MotorcycleSuspensionType = table.Column<string>(nullable: true),
                    Properties_MotorcycleChassisType = table.Column<string>(nullable: true),
                    Properties_ModelID = table.Column<string>(nullable: true),
                    Properties_FuelInjectionType = table.Column<string>(nullable: true),
                    Properties_FuelTypePrimary = table.Column<string>(nullable: true),
                    Properties_FuelTypeSecondary = table.Column<string>(nullable: true),
                    Properties_GCWR = table.Column<string>(nullable: true),
                    Properties_GCWR_to = table.Column<string>(nullable: true),
                    Properties_GVWR = table.Column<string>(nullable: true),
                    Properties_GVWR_to = table.Column<string>(nullable: true),
                    Properties_KeylessIgnition = table.Column<string>(nullable: true),
                    Properties_LaneCenteringAssistance = table.Column<string>(nullable: true),
                    Properties_LaneDepartureWarning = table.Column<string>(nullable: true),
                    Properties_LaneKeepSystem = table.Column<string>(nullable: true),
                    Properties_LowerBeamHeadlampLightSource = table.Column<string>(nullable: true),
                    Properties_MakeID = table.Column<string>(nullable: true),
                    Properties_Manufacturer = table.Column<string>(nullable: true),
                    Properties_ManufacturerId = table.Column<string>(nullable: true),
                    Properties_PlantCity = table.Column<string>(nullable: true),
                    Properties_PlantCompanyName = table.Column<string>(nullable: true),
                    Properties_PlantCountry = table.Column<string>(nullable: true),
                    Properties_PlantState = table.Column<string>(nullable: true),
                    Properties_TrailerBodyType = table.Column<string>(nullable: true),
                    Properties_TrailerLength = table.Column<string>(nullable: true),
                    Properties_TrailerType = table.Column<string>(nullable: true),
                    Properties_TransmissionSpeeds = table.Column<string>(nullable: true),
                    Properties_TransmissionStyle = table.Column<string>(nullable: true),
                    Properties_Trim2 = table.Column<string>(nullable: true),
                    Properties_Turbo = table.Column<string>(nullable: true),
                    Properties_VIN = table.Column<string>(nullable: true),
                    Properties_ValveTrainDesign = table.Column<string>(nullable: true),
                    Properties_VehicleType = table.Column<string>(nullable: true),
                    Properties_WheelBaseLong = table.Column<string>(nullable: true),
                    Properties_WheelBaseShort = table.Column<string>(nullable: true),
                    Properties_WheelBaseType = table.Column<string>(nullable: true),
                    Properties_WheelSizeFront = table.Column<string>(nullable: true),
                    Properties_WheelSizeRear = table.Column<string>(nullable: true),
                    Properties_TractionControl = table.Column<string>(nullable: true),
                    Properties_ForwardCollisionWarning = table.Column<string>(nullable: true),
                    Properties_TrackWidth = table.Column<string>(nullable: true),
                    Properties_TPMS = table.Column<string>(nullable: true),
                    Properties_PossibleValues = table.Column<string>(nullable: true),
                    Properties_Pretensioner = table.Column<string>(nullable: true),
                    Properties_RearAutomaticEmergencyBraking = table.Column<string>(nullable: true),
                    Properties_RearCrossTrafficAlert = table.Column<string>(nullable: true),
                    Properties_RearVisibilitySystem = table.Column<string>(nullable: true),
                    Properties_SAEAutomationLevel = table.Column<string>(nullable: true),
                    Properties_SAEAutomationLevel_to = table.Column<string>(nullable: true),
                    Properties_SeatBeltsAll = table.Column<string>(nullable: true),
                    Properties_SeatRows = table.Column<string>(nullable: true),
                    Properties_Seats = table.Column<string>(nullable: true),
                    Properties_SemiautomaticHeadlampBeamSwitching = table.Column<string>(nullable: true),
                    Properties_Series = table.Column<string>(nullable: true),
                    Properties_Series2 = table.Column<string>(nullable: true),
                    Properties_SteeringLocation = table.Column<string>(nullable: true),
                    Properties_SuggestedVIN = table.Column<string>(nullable: true),
                    Properties_TopSpeedMPH = table.Column<string>(nullable: true),
                    Properties_ErrorText = table.Column<string>(nullable: true),
                    Properties_ErrorCode = table.Column<string>(nullable: true),
                    Properties_EntertainmentSystem = table.Column<string>(nullable: true),
                    Properties_Axles = table.Column<string>(nullable: true),
                    Properties_BasePrice = table.Column<string>(nullable: true),
                    Properties_BatteryA = table.Column<string>(nullable: true),
                    Properties_BatteryA_to = table.Column<string>(nullable: true),
                    Properties_BatteryCells = table.Column<string>(nullable: true),
                    Properties_BatteryInfo = table.Column<string>(nullable: true),
                    Properties_BatteryKWh = table.Column<string>(nullable: true),
                    Properties_BatteryKWh_to = table.Column<string>(nullable: true),
                    Properties_BatteryModules = table.Column<string>(nullable: true),
                    Properties_BatteryPacks = table.Column<string>(nullable: true),
                    Properties_BatteryType = table.Column<string>(nullable: true),
                    Properties_BatteryV = table.Column<string>(nullable: true),
                    Properties_BatteryV_to = table.Column<string>(nullable: true),
                    Properties_BedLengthIN = table.Column<string>(nullable: true),
                    Properties_BedType = table.Column<string>(nullable: true),
                    Properties_AxleConfiguration = table.Column<string>(nullable: true),
                    Properties_BlindSpotIntervention = table.Column<string>(nullable: true),
                    Properties_AutomaticPedestrianAlertingSound = table.Column<string>(nullable: true),
                    Properties_AirBagLocSide = table.Column<string>(nullable: true),
                    Properties_ModelYear = table.Column<string>(nullable: true),
                    Properties_Make = table.Column<string>(nullable: true),
                    Properties_Model = table.Column<string>(nullable: true),
                    Properties_Trim = table.Column<string>(nullable: true),
                    Properties_ABS = table.Column<string>(nullable: true),
                    Properties_ActiveSafetySysNote = table.Column<string>(nullable: true),
                    Properties_AdaptiveCruiseControl = table.Column<string>(nullable: true),
                    Properties_AdaptiveDrivingBeam = table.Column<string>(nullable: true),
                    Properties_AdaptiveHeadlights = table.Column<string>(nullable: true),
                    Properties_AdditionalErrorText = table.Column<string>(nullable: true),
                    Properties_AirBagLocCurtain = table.Column<string>(nullable: true),
                    Properties_AirBagLocFront = table.Column<string>(nullable: true),
                    Properties_AirBagLocKnee = table.Column<string>(nullable: true),
                    Properties_AirBagLocSeatCushion = table.Column<string>(nullable: true),
                    Properties_AutoReverseSystem = table.Column<string>(nullable: true),
                    Properties_Wheels = table.Column<string>(nullable: true),
                    Properties_BlindSpotMon = table.Column<string>(nullable: true),
                    Properties_BodyClass = table.Column<string>(nullable: true),
                    Properties_DriveType = table.Column<string>(nullable: true),
                    Properties_DriverAssist = table.Column<string>(nullable: true),
                    Properties_DynamicBrakeSupport = table.Column<string>(nullable: true),
                    Properties_EDR = table.Column<string>(nullable: true),
                    Properties_ESC = table.Column<string>(nullable: true),
                    Properties_EVDriveUnit = table.Column<string>(nullable: true),
                    Properties_ElectrificationLevel = table.Column<string>(nullable: true),
                    Properties_EngineConfiguration = table.Column<string>(nullable: true),
                    Properties_EngineCycles = table.Column<string>(nullable: true),
                    Properties_EngineCylinders = table.Column<string>(nullable: true),
                    Properties_EngineHP = table.Column<string>(nullable: true),
                    Properties_EngineHP_to = table.Column<string>(nullable: true),
                    Properties_EngineKW = table.Column<string>(nullable: true),
                    Properties_EngineManufacturer = table.Column<string>(nullable: true),
                    Properties_EngineModel = table.Column<string>(nullable: true),
                    Properties_Doors = table.Column<string>(nullable: true),
                    Properties_BodyCabType = table.Column<string>(nullable: true),
                    Properties_DisplacementL = table.Column<string>(nullable: true),
                    Properties_DisplacementCC = table.Column<string>(nullable: true),
                    Properties_BrakeSystemDesc = table.Column<string>(nullable: true),
                    Properties_BrakeSystemType = table.Column<string>(nullable: true),
                    Properties_BusFloorConfigType = table.Column<string>(nullable: true),
                    Properties_BusLength = table.Column<string>(nullable: true),
                    Properties_BusType = table.Column<string>(nullable: true),
                    Properties_CAN_AACN = table.Column<string>(nullable: true),
                    Properties_CIB = table.Column<string>(nullable: true),
                    Properties_CashForClunkers = table.Column<string>(nullable: true),
                    Properties_ChargerLevel = table.Column<string>(nullable: true),
                    Properties_ChargerPowerKW = table.Column<string>(nullable: true),
                    Properties_CoolingType = table.Column<string>(nullable: true),
                    Properties_CurbWeightLB = table.Column<string>(nullable: true),
                    Properties_CustomMotorcycleType = table.Column<string>(nullable: true),
                    Properties_DaytimeRunningLight = table.Column<string>(nullable: true),
                    Properties_DestinationMarket = table.Column<string>(nullable: true),
                    Properties_DisplacementCI = table.Column<string>(nullable: true),
                    Properties_Windows = table.Column<string>(nullable: true),
                    Vin = table.Column<string>(nullable: false),
                    VinDecoded = table.Column<bool>(nullable: false),
                    IsAutomatic = table.Column<bool>(nullable: false),
                    IsNew = table.Column<bool>(nullable: false),
                    IsPetrol = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cars_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Videos_Cars_Id",
                        column: x => x.Id,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_UserId",
                table: "Cars",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "Auctions");

            migrationBuilder.DropTable(
                name: "Bids");

            migrationBuilder.DropTable(
                name: "CarReviews");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "Videos");

            migrationBuilder.DropTable(
                name: "Cars");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
