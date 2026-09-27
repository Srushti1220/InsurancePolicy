namespace InsuranceAPI.Models.Enums
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Agent = "Agent";
        public const string Staff = "Staff";
        public const string Customer = "Customer";

        public const string AdminOrAgent = "Admin,Agent,Staff";
        public const string AdminOnly = "Admin";
        public const string CustomerOnly = "Customer";
        public const string AllAuthenticated = "Admin,Agent,Staff,Customer";
    }
}
