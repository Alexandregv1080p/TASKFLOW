using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.IntegrationTests.Persistence;

namespace TaskFlow.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public class AuthTests(DatabaseFixture db)
{
    private readonly HttpClient _anonymous = db.Api.CreateClient();

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    [Theory]
    [InlineData("/api/projects")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/users")]
    [InlineData("/api/auth/me")]
    public async Task Endpoints_protegidos_exigem_token(string url)
    {
        await ApiTests.AssertErrorAsync(await _anonymous.GetAsync(url), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_invalido_retorna_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/projects");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "nao.e.um.jwt");

        await ApiTests.AssertErrorAsync(await _anonymous.SendAsync(request), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cadastro_login_e_me()
    {
        var email = NewEmail();

        var registered = await _anonymous.PostAsJsonAsync("/api/auth/register",
            new { name = "Bruna Tavares", email = email.ToUpperInvariant(), password = "senha-segura-123" });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        var login = await _anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "senha-segura-123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var auth = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(auth.GetProperty("expiresAt").GetDateTime() > DateTime.UtcNow);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        var me = await (await _anonymous.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Bruna Tavares", me.GetProperty("name").GetString());
        Assert.Equal(email, me.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Senha_errada_e_email_inexistente_retornam_o_mesmo_401()
    {
        var email = NewEmail();
        await _anonymous.PostAsJsonAsync("/api/auth/register", new { name = "Caio", email, password = "senha-segura-123" });

        var wrongPassword = await _anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "outra-senha-000" });
        var unknownEmail = await _anonymous.PostAsJsonAsync("/api/auth/login", new { email = NewEmail(), password = "senha-segura-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync() is var a ? a[..a.IndexOf("timestamp")] : null,
            await unknownEmail.Content.ReadAsStringAsync() is var b ? b[..b.IndexOf("timestamp")] : null);
    }

    [Fact]
    public async Task Email_duplicado_retorna_409_e_senha_curta_400()
    {
        var email = NewEmail();
        await _anonymous.PostAsJsonAsync("/api/auth/register", new { name = "Davi", email, password = "senha-segura-123" });

        var duplicate = await _anonymous.PostAsJsonAsync("/api/auth/register", new { name = "Davi 2", email, password = "senha-segura-123" });
        var shortPassword = await _anonymous.PostAsJsonAsync("/api/auth/register", new { name = "Eva", email = NewEmail(), password = "123" });

        await ApiTests.AssertErrorAsync(duplicate, HttpStatusCode.Conflict);
        await ApiTests.AssertErrorAsync(shortPassword, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Senha_nao_e_armazenada_em_texto_puro()
    {
        var email = NewEmail();
        await _anonymous.PostAsJsonAsync("/api/auth/register", new { name = "Fábio", email, password = "senha-segura-123" });

        await using var context = db.CreateContext();
        var user = context.Users.Single(u => u.Email == email);

        Assert.DoesNotContain("senha-segura-123", user.PasswordHash);
    }

    [Fact]
    public async Task Usuario_nao_ve_projeto_de_outro_pela_api()
    {
        var owner = await db.CreateAuthenticatedClientAsync("Dona");
        var intruder = await db.CreateAuthenticatedClientAsync("Intruso");

        var project = await (await owner.PostAsJsonAsync("/api/projects", new { name = "Privado" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = project.GetProperty("id").GetString();

        await ApiTests.AssertErrorAsync(await intruder.GetAsync($"/api/projects/{id}"), HttpStatusCode.NotFound);
        await ApiTests.AssertErrorAsync(await intruder.DeleteAsync($"/api/projects/{id}"), HttpStatusCode.NotFound);
        Assert.Equal(0, (await intruder.GetFromJsonAsync<JsonElement>("/api/projects")).GetArrayLength());
    }
}
