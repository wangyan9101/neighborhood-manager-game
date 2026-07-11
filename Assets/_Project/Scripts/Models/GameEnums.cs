namespace NeighborhoodManager.Models
{
    public enum GamePhase { None, Initializing, Playing, Paused, DaySettlement, Victory, Failed }
    public enum FacilityType { None, Elevator, ParkingLot, ExpressLocker, Camera, ChildrenArea, ChargingPile, General }
    public enum GameEventType { Complaint, Fault, Security, Environment }
    public enum EventUrgency { Normal, Urgent }
    public enum EventState { Pending, Handling, Completed, Failed }
    public enum WorkerType { Repairman, Security, CustomerService }
    public enum WorkerState { Idle, Working }
    public enum ReportGrade { S, A, B, C, D }
}
