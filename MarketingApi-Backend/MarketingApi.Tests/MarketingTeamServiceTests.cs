using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.MarketingTeam;
using MarketingApi.Enums;
using MarketingApi.Models.Entities;
using MarketingApi.Repository.Interfaces;
using MarketingApi.Service.Implementations;
using MarketingApi.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MarketingApi.Tests;

public class MarketingTeamServiceTests
{
    private readonly Mock<IMarketingTeamRepository> _teamRepoMock = new();
    private readonly Mock<ISalesmanRepository> _salesmanRepoMock = new();
    private readonly MarketingTeamService _service;

    public MarketingTeamServiceTests()
    {
        _service = new MarketingTeamService(
            _teamRepoMock.Object,
            _salesmanRepoMock.Object,
            new CreateMarketingTeamDtoValidator(),
            new UpdateMarketingTeamDtoValidator(),
            NullLogger<MarketingTeamService>.Instance);
    }

    [Fact]
    public async Task CreateTeam_DuplicateCode_Returns409_R3()
    {
        _teamRepoMock.Setup(r => r.CodeExistsAsync("MT01", null)).ReturnsAsync(true);

        var dto = new CreateMarketingTeamDto
        {
            Teamcode = "mt01",
            Teamname = "North Team",
            Teamleaderid = 1,
            Description = "North region",
            Members = new List<MarketingTeamDetailInputDto>
            {
                new() { Salesmanid = 1, Role = (short)SalesmanRole.TeamLeader, Targetamount = 1000, Status = 1 }
            }
        };

        var result = await _service.CreateTeamAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_NoMembers_Returns400_R4()
    {
        _teamRepoMock.Setup(r => r.CodeExistsAsync("MT02", null)).ReturnsAsync(false);

        var dto = new CreateMarketingTeamDto
        {
            Teamcode = "MT02",
            Teamname = "East Team",
            Teamleaderid = 1,
            Description = "East region",
            Members = new List<MarketingTeamDetailInputDto>()
        };

        var result = await _service.CreateTeamAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_NegativeTarget_Returns400_R9()
    {
        _teamRepoMock.Setup(r => r.CodeExistsAsync("MT02", null)).ReturnsAsync(false);

        var dto = new CreateMarketingTeamDto
        {
            Teamcode = "MT02",
            Teamname = "East Team",
            Teamleaderid = 1,
            Description = "East region",
            Members = new List<MarketingTeamDetailInputDto>
            {
                new() { Salesmanid = 1, Role = (short)SalesmanRole.TeamLeader, Targetamount = -10, Status = 1 }
            }
        };

        var result = await _service.CreateTeamAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_DuplicateSalesman_Returns400_R5()
    {
        _teamRepoMock.Setup(r => r.CodeExistsAsync("MT02", null)).ReturnsAsync(false);
        _salesmanRepoMock.Setup(r => r.GetSalesmenByIdsAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new List<Salesman> { new() { Id = 1, SalesmanName = "Arun", Status = 1 } });

        var dto = new CreateMarketingTeamDto
        {
            Teamcode = "MT02",
            Teamname = "East Team",
            Teamleaderid = 1,
            Description = "East region",
            Members = new List<MarketingTeamDetailInputDto>
            {
                new() { Salesmanid = 1, Role = (short)SalesmanRole.TeamLeader, Targetamount = 100, Status = 1 },
                new() { Salesmanid = 1, Role = (short)SalesmanRole.SalesExecutive, Targetamount = 50, Status = 1 }
            }
        };

        var result = await _service.CreateTeamAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_LeaderMismatch_Returns400_R6()
    {
        _teamRepoMock.Setup(r => r.CodeExistsAsync("MT02", null)).ReturnsAsync(false);
        _salesmanRepoMock.Setup(r => r.GetSalesmenByIdsAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new List<Salesman>
            {
                new() { Id = 1, SalesmanName = "Arun", Status = 1 },
                new() { Id = 2, SalesmanName = "Bala", Status = 1 }
            });

        var dto = new CreateMarketingTeamDto
        {
            Teamcode = "MT02",
            Teamname = "East Team",
            Teamleaderid = 1,
            Description = "East region",
            Members = new List<MarketingTeamDetailInputDto>
            {
                new() { Salesmanid = 2, Role = (short)SalesmanRole.TeamLeader, Targetamount = 100, Status = 1 }
            }
        };

        var result = await _service.CreateTeamAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_InactiveSalesman_Returns400_R7()
    {
        _teamRepoMock.Setup(r => r.CodeExistsAsync("MT02", null)).ReturnsAsync(false);
        _salesmanRepoMock.Setup(r => r.GetSalesmenByIdsAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new List<Salesman>
            {
                new() { Id = 1, SalesmanName = "Arun", Status = (short)RecordStatus.Inactive }
            });

        var dto = new CreateMarketingTeamDto
        {
            Teamcode = "MT02",
            Teamname = "East Team",
            Teamleaderid = 1,
            Description = "East region",
            Members = new List<MarketingTeamDetailInputDto>
            {
                new() { Salesmanid = 1, Role = (short)SalesmanRole.TeamLeader, Targetamount = 100, Status = 1 }
            }
        };

        var result = await _service.CreateTeamAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_SalesmanAlreadyInActiveTeam_Returns400_R8_M6()
    {
        _teamRepoMock.Setup(r => r.CodeExistsAsync("MT02", null)).ReturnsAsync(false);
        _salesmanRepoMock.Setup(r => r.GetSalesmenByIdsAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new List<Salesman>
            {
                new() { Id = 1, SalesmanName = "Arun", Status = 1 }
            });
        _teamRepoMock.Setup(r => r.GetActiveTeamCodesForSalesmenAsync(It.IsAny<IEnumerable<long>>(), null))
            .ReturnsAsync(new Dictionary<long, string> { { 1, "MT01" } });

        var dto = new CreateMarketingTeamDto
        {
            Teamcode = "MT02",
            Teamname = "East Team",
            Teamleaderid = 1,
            Description = "East region",
            Members = new List<MarketingTeamDetailInputDto>
            {
                new() { Salesmanid = 1, Role = (short)SalesmanRole.TeamLeader, Targetamount = 100, Status = 1 }
            }
        };

        var result = await _service.CreateTeamAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("MT01", result.ErrorMessage);
    }

    [Fact]
    public async Task ActivateTeam_LeaderInactive_Returns400_H3()
    {
        _teamRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10))
            .ReturnsAsync(new MarketingTeam
            {
                Id = 10,
                TeamCode = "MT10",
                TeamLeaderId = 5,
                Status = (short)RecordStatus.Inactive
            });
        _salesmanRepoMock.Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(new Salesman { Id = 5, SalesmanName = "Leader", Status = (short)RecordStatus.Inactive });

        var result = await _service.ActivateTeamAsync(10);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ActivateTeam_LeaderInAnotherActiveTeam_Returns400_H3()
    {
        _teamRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10))
            .ReturnsAsync(new MarketingTeam
            {
                Id = 10,
                TeamCode = "MT10",
                TeamLeaderId = 5,
                Status = (short)RecordStatus.Inactive
            });
        _salesmanRepoMock.Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(new Salesman { Id = 5, SalesmanName = "Leader", Status = (short)RecordStatus.Active });
        _teamRepoMock.Setup(r => r.GetActiveTeamCodeForSalesmanAsync(5, 10))
            .ReturnsAsync("MT01");

        var result = await _service.ActivateTeamAsync(10);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateTeam_RemoveAndReaddSameSalesmanSucceeds_H13()
    {
        var existingTeam = new MarketingTeam
        {
            Id = 1,
            TeamCode = "MT01",
            TeamName = "North",
            TeamLeaderId = 1,
            Description = "North region",
            Status = (short)RecordStatus.Active,
            MarketingTeamDetails = new List<MarketingTeamDetail>
            {
                new() { Id = 100, MarketingTeamId = 1, SalesmanId = 1, Role = (short)SalesmanRole.TeamLeader, TargetAmount = 500, Status = 1 },
                new() { Id = 101, MarketingTeamId = 1, SalesmanId = 2, Role = (short)SalesmanRole.SalesExecutive, TargetAmount = 200, Status = 1 }
            }
        };

        _teamRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(existingTeam);
        _salesmanRepoMock.Setup(r => r.GetSalesmenByIdsAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new List<Salesman>
            {
                new() { Id = 1, SalesmanName = "Leader", Status = 1 },
                new() { Id = 2, SalesmanName = "Member", Status = 1 }
            });
        _teamRepoMock.Setup(r => r.GetActiveTeamCodesForSalesmenAsync(It.IsAny<IEnumerable<long>>(), 1))
            .ReturnsAsync(new Dictionary<long, string>());

        var dto = new UpdateMarketingTeamDto
        {
            Teamname = "North",
            Teamleaderid = 1,
            Description = "North region",
            Members = new List<MarketingTeamDetailInputDto>
            {
                new() { Id = 100, Salesmanid = 1, Role = (short)SalesmanRole.TeamLeader, Targetamount = 500, Status = 1 },
                // Row 101 removed, salesman 2 re-added as new row (Id = null)
                new() { Id = null, Salesmanid = 2, Role = (short)SalesmanRole.SeniorSalesExecutive, Targetamount = 300, Status = 1 }
            }
        };

        var result = await _service.UpdateTeamAsync(1, dto);

        Assert.True(result.Success);
        _teamRepoMock.Verify(r => r.UpdateTeamWithMembersAsync(1, dto), Times.Once);
    }

    [Fact]
    public async Task UpdateTeam_ChangeSalesmanOnExistingRow_Returns400_H13()
    {
        var existingTeam = new MarketingTeam
        {
            Id = 1,
            TeamCode = "MT01",
            TeamName = "North",
            TeamLeaderId = 1,
            Description = "North region",
            Status = (short)RecordStatus.Active,
            MarketingTeamDetails = new List<MarketingTeamDetail>
            {
                new() { Id = 100, MarketingTeamId = 1, SalesmanId = 1, Role = (short)SalesmanRole.TeamLeader, TargetAmount = 500, Status = 1 },
                new() { Id = 101, MarketingTeamId = 1, SalesmanId = 2, Role = (short)SalesmanRole.SalesExecutive, TargetAmount = 200, Status = 1 }
            }
        };

        _teamRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(existingTeam);

        var dto = new UpdateMarketingTeamDto
        {
            Teamname = "North",
            Teamleaderid = 1,
            Description = "North region",
            Members = new List<MarketingTeamDetailInputDto>
            {
                new() { Id = 100, Salesmanid = 1, Role = (short)SalesmanRole.TeamLeader, Targetamount = 500, Status = 1 },
                new() { Id = 101, Salesmanid = 3, Role = (short)SalesmanRole.SalesExecutive, Targetamount = 200, Status = 1 }
            }
        };

        var result = await _service.UpdateTeamAsync(1, dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GetPagedTeams_ClampsPageSizeTo100_H8()
    {
        MarketingTeamQueryParameters? captured = null;
        _teamRepoMock.Setup(r => r.GetPagedTeamsAsync(It.IsAny<MarketingTeamQueryParameters>()))
            .Callback<MarketingTeamQueryParameters>(p => captured = p)
            .ReturnsAsync(new PagedResultDto<MarketingTeamListDto>());

        await _service.GetPagedTeamsAsync(new MarketingTeamQueryParameters { PageNumber = 0, PageSize = 5000 });

        Assert.NotNull(captured);
        Assert.Equal(1, captured!.PageNumber);
        Assert.Equal(100, captured.PageSize);
    }
}
