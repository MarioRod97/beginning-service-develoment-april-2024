﻿using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Mvc;

namespace IssueTrackerApi.Controllers.Issues;

public class Api(IDocumentSession session) : ControllerBase
{
    // GET /issues
    [HttpGet("/issues")]
    public async Task<ActionResult> GetTheIssuesAsync()
    {
        var issues = await session.Query<Issue>().ToListAsync();

        return Ok(issues);
    }

    [HttpGet("/issues/{id:guid}")]
    public async Task<ActionResult> GetIssueByIdAsync(Guid id)
    {
        var issue = await session.Query<Issue>().SingleOrDefaultAsync(i => i.Id == id);

        if (issue is null)
        {
            return NotFound();
        }
        else
        {
            return Ok(issue);
        }
    }

    [HttpPost("/issues")]
    public async Task<ActionResult> AddIssueAsync([FromBody] CreateIssueRequestModel request, [FromServices] IValidator<CreateIssueRequestModel> validator)
    {
        var results = await validator.ValidateAsync(request);

        if (results.IsValid)
        {
            var response = new Issue
            {
                CreatedAt = DateTime.UtcNow,
                Description = request.Description,
                Software = request.Software,
                Id = Guid.NewGuid(),
                Status = IssueStatus.Created
            };

            session.Store(response);
            await session.SaveChangesAsync();

            return Ok(response);
        }
        else
        {
            return BadRequest(results.ToDictionary()); // 400
        }
    }
}

public record CreateIssueRequestModel
{
    public string Software { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public record Issue
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Software { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public IssueStatus Status { get; set; }
}

public enum IssueStatus { Created }

public class CreateIssueRequestModelValidator : AbstractValidator<CreateIssueRequestModel>
{
    private readonly IReadOnlyList<string> _supportedSoftware = ["excel", "powerpoint", "word"];

    public CreateIssueRequestModelValidator()
    {
        RuleFor(i => i.Description)
            .NotEmpty()
            .MaximumLength(1024);

        RuleFor(i => i.Software)
            .NotEmpty()
            .Must(i =>
            {
                var sw = i.ToLowerInvariant().Trim();
                return _supportedSoftware.Any(s => s == sw);
            }).WithMessage("Unsupported Software. Good Luck.");
    }
}