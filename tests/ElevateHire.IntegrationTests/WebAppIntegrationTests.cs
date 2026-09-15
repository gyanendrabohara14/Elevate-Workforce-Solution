using System.Net;

namespace ElevateWorkforce.IntegrationTests;

public class WebAppIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _noRedirectClient;

    public WebAppIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _noRedirectClient = factory.CreateNoRedirectClient();
    }

    [Fact]
    public async Task Homepage_ReturnsOk_AndRendersBrand()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("ElevateWorkforce", html);
    }

    [Fact]
    public async Task JobsList_ReturnsOk_AndShowsOpportunities()
    {
        var response = await _client.GetAsync("/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("opportunit", html);
        Assert.Contains("Find Jobs", html);
    }

    [Fact]
    public async Task JobDetails_ReturnsOk_ForListing()
    {
        var jobsHtml = await (await _client.GetAsync("/jobs")).Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(jobsHtml, @"[Hh]ref=\""(/[Jj]obs/[Dd]etails/(\d+))\""");
        Assert.True(match.Success, "No job link found on the jobs page.");

        var response = await _client.GetAsync(match.Groups[1].Value);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Job summary", html);
    }

    [Fact]
    public async Task LoginPage_ReturnsOk()
    {
        var response = await _client.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Welcome back", html);
        Assert.Contains("data-role=\"Employer\"", html);
        Assert.DoesNotContain("js-demo-login", html);
    }

    [Fact]
    public async Task GetStartedPage_ReturnsOk_AndOffersRolePaths()
    {
        var response = await _client.GetAsync("/account/getstarted");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Welcome to ElevateWorkforce", html);
        Assert.Contains("looking for a job", html);
        Assert.Contains("I'm hiring", html);
        Assert.Contains("Log in as an administrator", html);
    }

    [Fact]
    public async Task AnonymousUser_IsRedirectedToLogin_ForAdminArea()
    {
        var response = await _noRedirectClient.GetAsync("/admin/dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task AnonymousUser_IsRedirectedToLogin_ForEmployerArea()
    {
        var response = await _noRedirectClient.GetAsync("/employer/dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.ToString());
    }
}