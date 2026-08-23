namespace FinancialMcp.Api.Common
{
    public static class GeneralExtension
    {
        public static bool IsDevOrLocal(this IHostEnvironment environment)
        {
            return environment.IsDevelopment() 
                || environment.IsEnvironment("devlocal");
        }
    }
}
