namespace MarketingApi.Enums
{
    /// <summary>
    /// Role of a salesman within a marketing team.
    /// 1 = Team Leader, 2 = Senior Sales Executive, 3 = Sales Executive, 4 = Trainee
    /// </summary>
    public enum SalesmanRole : short
    {
        TeamLeader = 1,
        SeniorSalesExecutive = 2,
        SalesExecutive = 3,
        Trainee = 4
    }
}
