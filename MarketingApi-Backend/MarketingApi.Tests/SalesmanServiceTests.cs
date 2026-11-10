using MarketingApi.Common;
using MarketingApi.DTOs.Common;
using MarketingApi.DTOs.Salesman;
using MarketingApi.Enums;
using MarketingApi.Models.Entities;
using MarketingApi.Repository.Interfaces;
using MarketingApi.Service.Implementations;
using MarketingApi.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MarketingApi.Tests;

public class SalesmanServiceTests
{
    private readonly Mock<ISalesmanRepository> _repoMock = new();
    private readonly SalesmanService _service;

    public SalesmanServiceTests()
    {
        _service = new SalesmanService(
            _repoMock.Object,
            new CreateSalesmanDtoValidator(),
            new UpdateSalesmanDtoValidator(),
            NullLogger<SalesmanService>.Instance);
    }

    [Fact]
    public async Task CreateSalesman_DuplicateCode_Returns409Conflict_R1()
    {
        _repoMock.Setup(r => r.CodeExistsAsync("SM001", null)).ReturnsAsync(true);

        var dto = new CreateSalesmanDto
        {
            Salesmancode = "sm001",
            Salesmanname = "Arjun Kumar",
            Mobileno = "9876543210",
            Email = "Arjun@Example.com",
            Joiningdate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        };

        var result = await _service.CreateSalesmanAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("salesmancode", result.Field);
    }

    [Fact]
    public async Task CreateSalesman_AutoLowercasesEmail_AndAllowsUnicodeName_M13()
    {
        _repoMock.Setup(r => r.CodeExistsAsync("SM002", null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.MobileExistsAsync("9876543211", null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.EmailExistsAsync("ramalingam@example.com", null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Salesman>()))
            .ReturnsAsync((Salesman s) => { s.Id = 10; return s; });

        var dto = new CreateSalesmanDto
        {
            Salesmancode = "sm002",
            Salesmanname = "ராமலிங்கம் José",
            Mobileno = "9876543211",
            Email = "Ramalingam@Example.COM",
            Joiningdate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
        };

        var result = await _service.CreateSalesmanAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("SM002", result.Data!.Salesmancode);
        Assert.Equal("ramalingam@example.com", result.Data.Email);
    }

    [Fact]
    public async Task CreateSalesman_FutureJoiningDate_Returns400BadRequest()
    {
        var dto = new CreateSalesmanDto
        {
            Salesmancode = "SM003",
            Salesmanname = "Ravi",
            Mobileno = "9876543212",
            Email = "ravi@example.com",
            Joiningdate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3))
        };

        var result = await _service.CreateSalesmanAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("joiningdate", result.Field);
    }

    [Fact]
    public async Task GetPagedSalesmen_ClampsPageSizeTo100_H8()
    {
        SalesmanQueryParameters? captured = null;
        _repoMock.Setup(r => r.GetPagedSalesmenAsync(It.IsAny<SalesmanQueryParameters>()))
            .Callback<SalesmanQueryParameters>(p => captured = p)
            .ReturnsAsync(new PagedResultDto<SalesmanListDto>());

        await _service.GetPagedSalesmenAsync(new SalesmanQueryParameters { PageNumber = -5, PageSize = 100000 });

        Assert.NotNull(captured);
        Assert.Equal(1, captured!.PageNumber);
        Assert.Equal(100, captured.PageSize);
    }

    [Fact]
    public async Task ApproveDeactivation_UsesCurrentAssignmentNotStaleRequestRole_H4_H5()
    {
        var request = new SalesmanDeactivationRequest
        {
            Id = 5,
            SalesmanId = 20,
            SalesmanCode = "SM020",
            SalesmanName = "Vikram",
            TeamId = 1,
            TeamCode = "MT01",
            RoleInTeam = nameof(TeamRoleType.Leader),
            Status = nameof(DeactivationRequestStatus.Pending)
        };

        var salesman = new Salesman
        {
            Id = 20,
            SalesmanCode = "SM020",
            SalesmanName = "Vikram",
            Status = (short)RecordStatus.Active
        };

        var currentAssignment = new ActiveTeamAssignmentDto
        {
            TeamId = 2,
            TeamCode = "MT02",
            TeamName = "South",
            RoleType = TeamRoleType.Member
        };

        _repoMock.Setup(r => r.GetDeactivationRequestByIdAsync(5)).ReturnsAsync(request);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(salesman);
        _repoMock.Setup(r => r.GetActiveTeamAssignmentAsync(20)).ReturnsAsync(currentAssignment);
        _repoMock.Setup(r => r.ApproveDeactivationAtomicallyAsync(request, salesman, currentAssignment, "mkt_user"))
            .ReturnsAsync(() =>
            {
                request.RoleInTeam = currentAssignment.RoleInTeam;
                request.TeamId = currentAssignment.TeamId;
                request.Status = nameof(DeactivationRequestStatus.Approved);
                return request;
            });

        var result = await _service.ApproveDeactivationRequestAsync(5, "mkt_user");

        Assert.True(result.Success);
        Assert.Equal("Member", result.Data!.RoleInTeam);
        Assert.Equal(2, result.Data.TeamId);
        _repoMock.Verify(r => r.ApproveDeactivationAtomicallyAsync(request, salesman, currentAssignment, "mkt_user"), Times.Once);
    }

    [Fact]
    public async Task RejectDeactivation_SavesNotes_H1()
    {
        var request = new SalesmanDeactivationRequest
        {
            Id = 7,
            SalesmanId = 30,
            Status = nameof(DeactivationRequestStatus.Pending)
        };
        _repoMock.Setup(r => r.GetDeactivationRequestByIdAsync(7)).ReturnsAsync(request);

        var result = await _service.RejectDeactivationRequestAsync(7, "mkt_master", "  Needed for Q4 drive  ");

        Assert.True(result.Success);
        Assert.Equal("Rejected", result.Data!.Status);
        Assert.Equal("Needed for Q4 drive", result.Data.Notes);
        _repoMock.Verify(r => r.UpdateDeactivationRequestAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeactivateSalesman_AlreadyInactive_Returns400_M16()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Salesman { Id = 1, SalesmanCode = "SM1", Status = (short)RecordStatus.Inactive });

        var result = await _service.DeactivateSalesmanAsync(1, "sup");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ActivateSalesman_CancelsPendingRequests_M16()
    {
        var salesman = new Salesman { Id = 1, SalesmanCode = "SM1", Status = (short)RecordStatus.Inactive };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(salesman);

        var result = await _service.ActivateSalesmanAsync(1, "sup");

        Assert.True(result.Success);
        _repoMock.Verify(r => r.ActivateSalesmanAndCancelPendingRequestsAsync(salesman, "sup"), Times.Once);
    }

    [Fact]
    public void EscapeLikePattern_EscapesWildcards_M8()
    {
        Assert.Equal(@"100\%\_test\\", SearchHelper.EscapeLikePattern(@"100%_test\"));
    }
}
